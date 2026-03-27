namespace Frank.Application.DTOs.Responses;

public record AccountExportDto(
    string FirstName,
    string[] AddictionTypes,
    string PreUrgeDescription,
    string CoreMotivation,
    string HabitDuration,
    DateTime CreatedAt,
    List<MorningExportDto> MorningCheckins,
    List<UrgeExportDto> UrgeEvents,
    List<EveningExportDto> EveningCheckins,
    List<WeeklyExportDto> WeeklySummaries
);

public record MorningExportDto(
    DateOnly Date,
    string? ForecastText,
    bool HighRiskFlag
);

public record UrgeExportDto(
    DateTime Timestamp,
    string AddictionType,
    int UrgeIntensity,
    bool Resisted,
    string? PreUrgeContext,
    string? HaltRoot
);

public record EveningExportDto(
    DateOnly Date,
    string DayResult,
    string? TomorrowRisk
);

public record WeeklyExportDto(
    DateOnly WeekStart,
    int TotalUrges,
    int TotalResisted,
    double ResistRate,
    string? Observation1,
    string? Observation2,
    string? Observation3,
    string? ReflectionText
);