namespace Frank.Application.DTOs.Responses;

public record MiniMirrorResponse(
    string[] Quotes,
    string Observation,
    int UrgeCount
);