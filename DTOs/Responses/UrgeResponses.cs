namespace Frank.Application.DTOs.Responses;

public record InterventionResponse(
    string Id,
    string Text,

    /// <summary>question / physical / mirror / replacement / surfing</summary>
    string Type,

    /// <summary>frame1 / frame2</summary>
    string Frame
);

public record StartUrgeResponse(
    Guid UrgeEventId,
    InterventionResponse Intervention
);

public record ResolveUrgeResponse(
    int DailyCount,

    /// <summary>
    /// Counter message based on daily count.
    /// null when count = 1 (silent first press).
    /// "That's twice today. Still watching." on count = 2.
    /// Loaded from app_settings — never hardcoded.
    /// </summary>
    string? CounterMessage
);