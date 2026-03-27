using System.ComponentModel.DataAnnotations;

namespace Frank.Application.DTOs.Requests;

public record RegisterRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required][MaxLength(50)] string FirstName
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);