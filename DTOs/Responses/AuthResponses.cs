namespace Frank.Application.DTOs.Responses;

public record AuthResponse(
    Guid UserId,
    string Token
);