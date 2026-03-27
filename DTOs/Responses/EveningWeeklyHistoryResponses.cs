
namespace Frank.Application.DTOs.Responses;

// ── Evening ───────────────────────────────────────────────────────────────────

public record EveningStatusResponse(bool Completed);

public record EveningCheckinResponse(
    /// <summary>
    /// Fixed string from app_settings based on day_result.
    /// "Logged. Rest well." / "It happens. You still showed up." / "Hard days are data too."
    /// </summary>
    string ResponseText
);

// ── Weekly ────────────────────────────────────────────────────────────────────

/// <summary>
/// One observation with supporting raw user quotes.
/// Frontend renders quotes FIRST then observation.
/// Pattern undeniable when shown user's own words.
/// </summary>
public record ObservationWithQuotes(
    string Observation,
    string[] Quotes
);

public record WeeklyMirrorResponse(
    ObservationWithQuotes[] Observations,
    QuestionResponse? ReflectionQuestion,
    double ResistRate,
    int TotalUrges,
    string? WorstDay,
    int? WorstHour
);

// ── History ───────────────────────────────────────────────────────────────────

public record WeekSummaryResponse(
    int WeekNumber,
    DateOnly DateFrom,
    DateOnly DateTo,
    int TotalUrges,
    int Resisted,
    double ResistRate
);

public record HistoryWeeksResponse(
    WeekSummaryResponse[] Weeks
);

public record ChatMessageResponse(
    string Role,
    string? Content,
    string MessageType,
    string? TapValue,
    DateTime Timestamp,
    string? SessionType
);

public record DayChatResponse(
    ChatMessageResponse[] Messages
);

// ── Shared ────────────────────────────────────────────────────────────────────

public record SuccessResponse(bool Success = true);

public record ErrorResponse(
    string Message,
    string? Code = null
);