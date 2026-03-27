using Frank.Application.DTOs.Responses;
using Frank.Domain.Entities;

namespace Frank.Application.Interfaces.Services;

// ── Context records ───────────────────────────────────────────────────────────

/// <summary>Everything Claude needs to generate the morning forecast.</summary>
public record MorningForecastContext(
    UserProfile Profile,
    UserSummary? Summary,
    string MoodState,
    string SleepQuality,
    string DayStructure,
    string? EmotionalCarry,   // from M02/M11 answer
    string? TomorrowRisk,     // from last night's E14
    int? YesterdayScore,
    string Question1Text,
    string Answer1,
    string Question2Text,
    string Answer2
);

/// <summary>Everything Claude needs for the weekly mirror observations.</summary>
public record WeeklyMirrorContext(
    UserProfile Profile,
    WeeklySummary Summary,
    List<string> PreUrgeContexts,  // raw quotes from this week's urge sessions
    WeeklySummary? PreviousWeek
);

/// <summary>Everything Claude needs for the weekly reflection question.</summary>
public record ReflectionContext(
    string Observation1,
    string Observation2,
    string Observation3,
    string[] RecentQuestionIds      // avoid repeating within 4 weeks
);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IClaudeService
{
    /// <summary>
    /// Generates morning forecast.
    /// Returns 2 sentences max, 30 words max, plain text.
    /// On any failure returns fallback from IAppSettingsService — never throws.
    /// Never logs context content — logs user_id + event type only.
    /// </summary>
    Task<string> GetMorningForecastAsync(
        MorningForecastContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates weekly mirror observations with supporting raw quotes.
    /// Returns JSON parsed into list of ObservationWithQuotes.
    /// On any failure returns empty list — caller uses fallback string.
    /// </summary>
    Task<List<ObservationWithQuotes>> GetWeeklyObservationsAsync(
        WeeklyMirrorContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates weekly reflection question.
    /// Returns one question max 20 words ending with ?.
    /// Returns null on failure — caller uses W01 as fallback.
    /// </summary>
    Task<string?> GetReflectionQuestionAsync(
        ReflectionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Generates mini mirror observation from early urge quotes.
    /// One sentence. Called once on day 3-7 when urge_count >= 3.
    /// Returns fallback string on failure — never throws.
    /// </summary>
    Task<string> GetMiniMirrorObservationAsync(
        List<string> quotes,
        UserProfile profile,
        CancellationToken ct = default);
}