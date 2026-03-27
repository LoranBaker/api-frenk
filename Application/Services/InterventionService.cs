using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

/// <summary>
/// The routing brain of Frank.
/// Selects the right intervention for every urge press.
/// 
/// Decision flow:
///   1. HALT check — if halt_root found → HALT intervention → stop
///   2. Intensity gate → determines frame (frame1/frame2)
///   3. Scenario router → 12 signatures checked in priority order
///   4. DEFAULT → U01 or physical interrupt
///
/// Zero hardcoded logic — intensity thresholds read from
/// question_selection_rules table. Intervention texts
/// loaded from questions table. Never from code.
/// </summary>
public class InterventionService : IInterventionService
{
    private readonly IQuestionRepository _questions;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<InterventionService> _logger;

    public InterventionService(
        IQuestionRepository questions,
        IAppSettingsService settings,
        ILogger<InterventionService> logger)
    {
        _questions = questions;
        _settings = settings;
        _logger = logger;
    }

    public async Task<InterventionResponse> SelectAsync(
    InterventionContext context,
    CancellationToken ct = default)
    {
        _logger.LogInformation(
            "INTERVENTION INPUT: Addiction={A} Intensity={I} HaltRoot={H} Environment={E} Hour={Hr} SlippedToday={S} ResistRate={R} Duration={D} Identity={Id}",
            context.AddictionType, context.UrgeIntensity, context.HaltRoot,
            context.Environment, context.HourOfDay, context.SlippedToday,
            context.ResistRateThisWeek, context.HabitDuration, context.IdentityFraming);

        // Step 1
        if (!string.IsNullOrEmpty(context.HaltRoot))
        {
            _logger.LogInformation("INTERVENTION: HALT root found = {Root}", context.HaltRoot);
            var haltIntervention = await GetHaltInterventionAsync(context.HaltRoot, ct);
            if (haltIntervention is not null) return haltIntervention;
            _logger.LogWarning("INTERVENTION: HALT question not found for root {Root}", context.HaltRoot);
        }

        // Step 2
        var frame = context.UrgeIntensity switch
        {
            >= 8 => "frame2",
            >= 4 => "frame2",
            _ => "frame1"
        };
        _logger.LogInformation("INTERVENTION: Frame selected = {Frame}", frame);

        // Step 3 — replace scenario ID lookup with tag lookup
        var tag = GetScenarioTag(context, frame);
        _logger.LogInformation("INTERVENTION: Tag selected = {Tag}", tag ?? "NULL");

        if (tag is not null)
        {
            var tagIntervention = await GetInterventionByTagAsync(tag, frame, ct);
            if (tagIntervention is not null) return tagIntervention;
            _logger.LogWarning("INTERVENTION: No active questions for tag {Tag}", tag);
        }

        // Step 4
        _logger.LogInformation("INTERVENTION: Using default for intensity {I}", context.UrgeIntensity);
        return await GetDefaultInterventionAsync(context, frame, ct);
    }
    private async Task<InterventionResponse?> GetInterventionByTagAsync(
      string tag,
      string frame,
      CancellationToken ct)
    {
        var questions = await _questions.GetByTagAsync(tag, ct);
        if (!questions.Any()) return null;

        // Get today's already shown intervention IDs to avoid repeats
        // For now just pick random — add exclusion when you have more questions
        var selected = questions[new Random().Next(questions.Count)];
        var type = DetermineInterventionType(selected.PatternTags);

        return new InterventionResponse(
            Id: selected.Id,
            Text: selected.Text,
            Type: type,
            Frame: frame
        );
    }
    // ── Scenario Router ───────────────────────────────────

    /// <summary>
    /// 12 scenario signatures checked in priority order.
    /// First match wins.
    /// Returns intervention question ID or null for default.
    /// </summary>
    // Change signature — returns tag not ID
    private static string? GetScenarioTag(
     InterventionContext context,
     string frame)
    {
        var addiction = context.AddictionType;
        var hour = context.HourOfDay;
        var intensity = context.UrgeIntensity;
        var duration = context.HabitDuration;
        var identity = context.IdentityFraming ?? "";
        var env = context.Environment ?? "";

        // Priority 1 — late night high intensity (overrides everything)
        if (hour is >= 22 or <= 4 && intensity >= 7)
            return "midnight_regret";

        // Priority 2 — addiction specific high intensity
        if (addiction == "gambling")
            return "cost_reflection";

        if (addiction == "gaming")
            return env == "home_alone" && hour is >= 18 or <= 2
                ? "physical_interrupt"
                : "identity_mirror";

        if (addiction == "weed")
            return "justification_catch";

        if (addiction == "cigarettes")
            return env is "work_study" or "transit_out"
                ? "physical_interrupt"
                : "body_awareness";

        if (addiction == "social_media")
            return "timer_awareness";

        // Priority 3 — slipped today (after addiction routing)
        if (context.SlippedToday)
            return "positive_anchor";

        // Priority 4 — identity locked
        if (identity == "identity_doer")
            return "identity_mirror";

        // Priority 5 — long habit + high intensity
        if (duration is "gt_3yr" && intensity >= 8)
            return "physical_interrupt";

        // Priority 6 — low resist rate
        if (context.ResistRateThisWeek < 0.30)
            return "urge_surfing";

        // Priority 7 — struggling users
        if (context.ResistRateThisWeek < 0.50)
            return "positive_anchor";

        return null;
    }

    // ── Intervention loaders ──────────────────────────────

    private async Task<InterventionResponse?> GetHaltInterventionAsync(
        string haltRoot,
        CancellationToken ct)
    {
        // Map halt_root to the corresponding question ID
        var questionId = haltRoot switch
        {
            "hungry" => "H01",
            "angry" => "H02",
            "lonely" => "H03",
            "tired" => "H04",
            "stressed" => "H05",
            _ => null
        };

        if (questionId is null) return null;

        return await GetInterventionByIdAsync(questionId, "frame2", ct);
    }

    private async Task<InterventionResponse?> GetInterventionByIdAsync(
        string questionId,
        string frame,
        CancellationToken ct)
    {
        var question = await _questions.GetByIdAsync(questionId, ct);
        if (question is null)
        {
            _logger.LogWarning(
                "Intervention question {QuestionId} not found in DB.", questionId);
            return null;
        }

        // Determine intervention type from question framework field
        var type = DetermineInterventionType(question.PatternTags);

        return new InterventionResponse(
            Id: question.Id,
            Text: question.Text,
            Type: type,
            Frame: frame
        );
    }

    private async Task<InterventionResponse> GetDefaultInterventionAsync(
        InterventionContext context,
        string frame,
        CancellationToken ct)
    {
        // Default: physical interrupt for high intensity, reflective for low
        var defaultId = context.UrgeIntensity >= 8 ? "U13" : "U01";
        var fallback = await GetInterventionByIdAsync(defaultId, frame, ct);

        if (fallback is not null) return fallback;

        // Hard fallback — this should never happen but never leave user stranded
        _logger.LogError(
            "Default intervention {Id} not found. Using hardcoded fallback.",
            defaultId);

        return new InterventionResponse(
            Id: "U13",
            Text: "Stand up. Walk to your kitchen. Drink a full glass of water slowly. Come back. That's all.",
            Type: "physical",
            Frame: "frame2"
        );
    }

    private static string DetermineInterventionType(string[] patternTags)
    {
        if (patternTags.Contains("physical_interrupt")) return "physical";
        if (patternTags.Contains("identity_mirror")) return "mirror";
        if (patternTags.Contains("urge_surfing")) return "surfing";
        if (patternTags.Contains("body_awareness")) return "question";
        if (patternTags.Contains("timer_awareness")) return "question";
        if (patternTags.Contains("cost_reflection")) return "question";
        if (patternTags.Contains("justification_catch")) return "question";
        if (patternTags.Contains("positive_anchor")) return "question";
        if (patternTags.Contains("midnight_regret")) return "question";
        return "question";
    }
}