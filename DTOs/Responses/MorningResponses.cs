namespace Frank.Application.DTOs.Responses;

public record MorningCheckinResponse(
    string ForecastText,
    Guid SessionId
);

public record MorningStatusResponse(
    bool Completed,
    string? ForecastText
);