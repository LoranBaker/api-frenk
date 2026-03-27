using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly ITapOptionRepository _tapOptions;
    private readonly ILogger<QuestionService> _logger;

    private const int CooldownDays = 7;

    public QuestionService(
        IQuestionRepository questions,
        ITapOptionRepository tapOptions,
        ILogger<QuestionService> logger)
    {
        _questions = questions;
        _tapOptions = tapOptions;
        _logger = logger;
    }

    // ── Morning pair ──────────────────────────────────────

    /// <summary>
    /// Reads question_selection_rules for session_type=morning.
    /// For each required slot: queries questions by frame, excludes cooldown.
    /// Returns one frame1 (emotional) + one circumstance question.
    /// Never same pair two days in a row.
    /// Zero hardcoded question IDs.
    /// </summary>
    public async Task<QuestionsResponse> GetMorningPairAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var rules = await _questions.GetSelectionRulesAsync("morning", ct);
        var pair = new List<QuestionResponse>();

        foreach (var rule in rules.Where(r => r.Required).OrderBy(r => r.Slot))
        {
            var candidates = await _questions.GetByFrameAsync(
                sessionType: "morning",
                frame: rule.Frame,
                userId: userId,
                cooldownDays: CooldownDays,
                ct: ct);

            if (!candidates.Any())
            {
                // All questions on cooldown — reset and pick by weight
                _logger.LogWarning(
                    "All {Frame} morning questions on cooldown for user {UserId}. Resetting.",
                    rule.Frame, userId);

                candidates = await _questions.GetByFrameAsync(
                    sessionType: "morning",
                    frame: rule.Frame,
                    userId: Guid.Empty, // skip cooldown filter
                    cooldownDays: 0,
                    ct: ct);
            }

            var selected = PickByWeight(candidates);
            if (selected is null) continue;

            await _questions.RecordUsageAsync(userId, selected.Id, "morning", ct);

            pair.Add(await MapToResponseAsync(selected, ct));
        }

        return new QuestionsResponse(pair.ToArray());
    }

    // ── Onboarding ────────────────────────────────────────

    public async Task<QuestionsResponse> GetOnboardingQuestionsAsync(
        CancellationToken ct = default)
    {
        var questions = await _questions.GetBySessionTypeAsync("onboarding", ct);
        var responses = await MapManyToResponseAsync(questions, ct);
        return new QuestionsResponse(responses);
    }

    // ── HALT ──────────────────────────────────────────────

    public async Task<QuestionsResponse> GetHaltQuestionsAsync(
        CancellationToken ct = default)
    {
        var questions = await _questions.GetBySessionTypeAsync("halt", ct);
        var responses = await MapManyToResponseAsync(questions, ct);
        return new QuestionsResponse(responses);
    }

    // ── Urge questions ────────────────────────────────────

    public async Task<QuestionsResponse> GetUrgeQuestionsAsync(
        string addictionType,
        int intensity,
        string? haltRoot,
        CancellationToken ct = default)
    {
        // Determine frame based on intensity
        var frame = intensity switch
        {
            >= 8 => "frame2",
            >= 4 => "frame2",
            _ => "frame1"
        };

        var questions = await _questions.GetByFrameAsync(
            sessionType: "urge",
            frame: frame,
            userId: Guid.Empty, // no cooldown for urge questions
            cooldownDays: 0,
            ct: ct);

        // Filter further by addiction type via pattern_tags if available
        var filtered = questions
            .Where(q => q.PatternTags.Length == 0 ||
                        q.PatternTags.Contains(addictionType) ||
                        q.PatternTags.Contains("all"))
            .ToList();

        var responses = await MapManyToResponseAsync(filtered, ct);
        return new QuestionsResponse(responses);
    }

    // ── Helpers ───────────────────────────────────────────

    private static Question? PickByWeight(List<Question> candidates)
    {
        if (!candidates.Any()) return null;

        var totalWeight = candidates.Sum(q => q.Weight);
        var random = new Random().Next(0, totalWeight);
        var cumulative = 0;

        foreach (var q in candidates)
        {
            cumulative += q.Weight;
            if (random < cumulative) return q;
        }

        return candidates.Last();
    }

    private async Task<QuestionResponse> MapToResponseAsync(
        Question q,
        CancellationToken ct)
    {
        var options = await _tapOptions.GetByQuestionIdAsync(q.Id, ct);

        return new QuestionResponse(
            Id: q.Id,
            Text: q.Text,
            InputType: q.InputType,
            Frame: q.Frame,
            TapOptions: options
                .Where(o => o.Active)
                .OrderBy(o => o.Order)
                .Select(o => new TapOptionResponse(
                    Id: o.Id,
                    Text: o.Text,
                    Value: o.Value,
                    Order: o.Order,
                    PatternTag: o.PatternTag,
                    ColorHint: o.ColorHint))
                .ToArray()
        );
    }

    private async Task<QuestionResponse[]> MapManyToResponseAsync(
        List<Question> questions,
        CancellationToken ct)
    {
        // Batch load all tap options in one query
        var ids = questions.Select(q => q.Id).ToList();
        var options = await _tapOptions.GetByQuestionIdsAsync(ids, ct);
        var optMap = options.GroupBy(o => o.QuestionId)
                             .ToDictionary(g => g.Key, g => g.ToList());

        return questions.Select(q =>
        {
            var qOptions = optMap.TryGetValue(q.Id, out var opts)
                ? opts.Where(o => o.Active)
                      .OrderBy(o => o.Order)
                      .Select(o => new TapOptionResponse(
                          Id: o.Id,
                          Text: o.Text,
                          Value: o.Value,
                          Order: o.Order,
                          PatternTag: o.PatternTag,
                          ColorHint: o.ColorHint))
                      .ToArray()
                : Array.Empty<TapOptionResponse>();

            return new QuestionResponse(
                Id: q.Id,
                Text: q.Text,
                InputType: q.InputType,
                Frame: q.Frame,
                TapOptions: qOptions);
        }).ToArray();


    }

    public async Task<TapOptionsMapResponse> GetAllTapOptionsAsync(CancellationToken ct = default)
    {
        var options = await _tapOptions.GetAllActiveAsync(ct);
        var map = options.Select(o => new TapOptionMapItem(o.Value, o.Text)).ToArray();
        return new TapOptionsMapResponse(map);
    }
}