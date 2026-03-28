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
///   1. HALT check
///        — intensity < 8  → HALT question (user can still reflect)
///        — intensity >= 8 → skip question, store root, fall through to physical
///   2. Frame: >= 8 = frame2, else frame1
///   3. Scenario router → tag
///   4. Intensity override: if >= 8 and tag is reflective → force physical_interrupt
///   5. Load pool → exclude last 3 shown today → random pick
///   6. Default
/// </summary>
public class InterventionService : IInterventionService
{
    private readonly IQuestionRepository _questions;
    private readonly IAppSettingsService _settings;
    private readonly IUrgeRepository _urge;
    private readonly ILogger<InterventionService> _logger;

    // Tags allowed to fire at intensity 8-10.
    // midnight_regret is allowed because its pool now contains physical interrupts.
    // Everything else gets overridden to physical_interrupt.
    private static readonly HashSet<string> HighIntensityAllowedTags = new()
    {
        "physical_interrupt",
        "midnight_regret"
    };

    // Exclude last N interventions per user to prevent same question repeating.
    private const int RepeatExclusionWindow = 6; // last 6 excluded — ~15-day no-repeat coverage

    public InterventionService(
        IQuestionRepository questions,
        IAppSettingsService settings,
        IUrgeRepository urge,
        ILogger<InterventionService> logger)
    {
        _questions = questions;
        _settings = settings;
        _urge = urge;
        _logger = logger;
    }

    public async Task<InterventionResponse> SelectAsync(
        InterventionContext context,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "INTERVENTION INPUT: Addiction={A} Intensity={I} HaltRoot={H} " +
            "Environment={E} Hour={Hr} SlippedToday={S} ResistRate={R} Duration={D} Identity={Id}",
            context.AddictionType, context.UrgeIntensity, context.HaltRoot,
            context.Environment, context.HourOfDay, context.SlippedToday,
            context.ResistRateThisWeek, context.HabitDuration, context.IdentityFraming);

        // ── Step 1: HALT ──────────────────────────────────────────────────────
        // FIX B: At intensity >= 8 the brain is flooded — telling a hungry or
        // tired user to address that need is clinically wrong, they can't act on
        // it meaningfully. Store the halt_root for data (already done by UrgeService),
        // skip the question, fall through to physical interrupt.
        if (!string.IsNullOrEmpty(context.HaltRoot))
        {
            if (context.UrgeIntensity >= 8)
            {
                _logger.LogInformation(
                    "INTERVENTION: HALT root={Root} found but intensity={I} >= 8. " +
                    "Skipping HALT question — routing to physical interrupt.",
                    context.HaltRoot, context.UrgeIntensity);
                // fall through intentionally
            }
            else
            {
                _logger.LogInformation(
                    "INTERVENTION: HALT root={Root}, intensity={I} < 8. Returning HALT question.",
                    context.HaltRoot, context.UrgeIntensity);

                var haltIntervention = await GetHaltInterventionAsync(context.HaltRoot, ct);
                if (haltIntervention is not null) return haltIntervention;

                _logger.LogWarning(
                    "INTERVENTION: HALT question not found for root {Root}. Falling through.",
                    context.HaltRoot);
            }
        }

        // ── Step 2: Frame ─────────────────────────────────────────────────────
        var frame = context.UrgeIntensity switch
        {
            >= 8 => "frame2",
            >= 4 => "frame1",
            _ => "frame1"
        };
        _logger.LogInformation(
            "INTERVENTION: Frame={Frame} (intensity={I})", frame, context.UrgeIntensity);

        // ── Step 3: Scenario tag ──────────────────────────────────────────────
        var lowResistThreshold = _settings.GetDouble("urge_low_resist_threshold", 0.30);
        var midResistThreshold = _settings.GetDouble("urge_mid_resist_threshold", 0.50);

        var tag = GetScenarioTag(context, frame, lowResistThreshold, midResistThreshold);
        _logger.LogInformation("INTERVENTION: Scenario tag={Tag}", tag ?? "NULL");

        // ── Step 4: Intensity override ────────────────────────────────────────
        // FIX A: Spec rule — 8-10 = physical interrupt ONLY. No reflective questions.
        // If scenario picked a reflective tag, override it.
        if (context.UrgeIntensity >= 8 && tag is not null && !HighIntensityAllowedTags.Contains(tag))
        {
            _logger.LogInformation(
                "INTERVENTION: Intensity={I} >= 8, overriding tag {OldTag} → physical_interrupt.",
                context.UrgeIntensity, tag);
            tag = "physical_interrupt";
        }

        // No tag from scenario + high intensity → force physical
        if (context.UrgeIntensity >= 8 && tag is null)
        {
            _logger.LogInformation(
                "INTERVENTION: Intensity={I} >= 8, no scenario tag → physical_interrupt.",
                context.UrgeIntensity);
            tag = "physical_interrupt";
        }

        // ── Step 5: Load pool, exclude recent, pick ───────────────────────────
        if (tag is not null)
        {
            var intervention = await GetInterventionByTagAsync(tag, frame, context.UserId, ct);
            if (intervention is not null) return intervention;

            _logger.LogWarning(
                "INTERVENTION: No active questions for tag {Tag} after exclusions. Falling to default.", tag);
        }

        // ── Step 6: Default ───────────────────────────────────────────────────
        _logger.LogInformation("INTERVENTION: Default path. Intensity={I}", context.UrgeIntensity);
        return await GetDefaultInterventionAsync(context, frame, ct);
    }

    // ── Scenario Router ───────────────────────────────────────────────────────

    private static string? GetScenarioTag(
        InterventionContext context,
        string frame,
        double lowResistThreshold,
        double midResistThreshold)
    {
        var addiction = context.AddictionType;
        var hour = context.HourOfDay;
        var intensity = context.UrgeIntensity;
        var duration = context.HabitDuration;
        var identity = context.IdentityFraming ?? "";
        var env = context.Environment ?? "";

        // Late night high intensity — midnight_regret pool has physical interrupts now
        if ((hour >= 22 || hour <= 4) && intensity >= 7)
            return "midnight_regret";

        // Addiction-specific
        if (addiction == "gambling")
            return "cost_reflection";

        if (addiction == "gaming")
            return (env == "home_alone" && (hour >= 18 || hour <= 2))
                ? "physical_interrupt"
                : "identity_mirror";

        // Weed: high intensity → physical (flooded), else catch the story
        if (addiction == "weed")
            return intensity >= 8 ? "physical_interrupt" : "justification_catch";

        // Cigarettes: can't smoke at work/transit → physical redirect
        if (addiction == "cigarettes")
            return (env is "work_study" or "transit_out")
                ? "physical_interrupt"
                : "body_awareness";

        if (addiction == "social_media")
            return "timer_awareness";

        // Slipped today — redirect to positive, not shame (after addiction routing)
        if (context.SlippedToday)
            return "positive_anchor";

        // Identity locked — show them who they wrote they want to be
        if (identity == "identity_doer")
            return "identity_mirror";

        // Long habit + high intensity → automatic behaviour, break it physically
        if (duration is "gt_3yr" && intensity >= 8)
            return "physical_interrupt";

        // Resist rate gates — tunable from app_settings without deploy
        if (context.ResistRateThisWeek < lowResistThreshold)
            return "urge_surfing";

        if (context.ResistRateThisWeek < midResistThreshold)
            return "positive_anchor";

        return null;
    }

    // ── Intervention loaders ──────────────────────────────────────────────────

    /// <summary>
    /// FIX C: Repeat prevention.
    /// Loads pool for tag, excludes the last N intervention IDs this user
    /// already saw today. Picks randomly from what remains.
    /// If exclusion empties the pool (small pool + many presses) — falls back
    /// to full pool. Better a repeat than a hardcoded fallback.
    /// </summary>
    private async Task<InterventionResponse?> GetInterventionByTagAsync(
        string tag,
        string frame,
        Guid userId,
        CancellationToken ct)
    {
        var pool = await _questions.GetByTagAsync(tag, ct);
        if (!pool.Any()) return null;

        // Get last N intervention IDs shown to this user today
        var recentIds = await _urge.GetRecentInterventionIdsAsync(
            userId, RepeatExclusionWindow, ct);

        var filtered = pool.Where(q => !recentIds.Contains(q.Id)).ToList();

        // Exclusion reset: if all questions were recently shown, use full pool
        var candidates = filtered.Any() ? filtered : pool;

        if (!filtered.Any())
        {
            _logger.LogInformation(
                "INTERVENTION: Exclusion reset for tag={Tag}, user={UserId}. All {Count} questions recently shown.",
                tag, userId, pool.Count);
        }

        var selected = candidates[new Random().Next(candidates.Count)];

        _logger.LogInformation(
            "INTERVENTION: Selected {Id} from tag={Tag}. Pool={PoolSize}, Available={AvailableSize}, Excluded={ExcludedCount}.",
            selected.Id, tag, pool.Count, candidates.Count, recentIds.Count);

        return new InterventionResponse(
            Id: selected.Id,
            Text: selected.Text,
            Type: DetermineInterventionType(selected.PatternTags),
            Frame: frame
        );
    }

    private async Task<InterventionResponse?> GetHaltInterventionAsync(
        string haltRoot,
        CancellationToken ct)
    {
        var pool = await _questions.GetByHaltRootAsync(haltRoot, ct);
        if (!pool.Any()) return null;

        var selected = pool[new Random().Next(pool.Count)];
        return new InterventionResponse(
            Id: selected.Id,
            Text: selected.Text,
            Type: "question",
            Frame: "frame2"
        );
    }

    private async Task<InterventionResponse?> GetInterventionByIdAsync(
        string questionId,
        string frame,
        CancellationToken ct)
    {
        var question = await _questions.GetByIdAsync(questionId, ct);
        if (question is null)
        {
            _logger.LogWarning("Intervention question {QuestionId} not found in DB.", questionId);
            return null;
        }

        return new InterventionResponse(
            Id: question.Id,
            Text: question.Text,
            Type: DetermineInterventionType(question.PatternTags),
            Frame: frame
        );
    }

    private async Task<InterventionResponse> GetDefaultInterventionAsync(
        InterventionContext context,
        string frame,
        CancellationToken ct)
    {
        var defaultId = context.UrgeIntensity >= 8 ? "U13" : "U01";
        var fallback = await GetInterventionByIdAsync(defaultId, frame, ct);
        if (fallback is not null) return fallback;

        _logger.LogError("Default intervention {Id} not found. Using hardcoded fallback.", defaultId);

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
        return "question";
    }
}