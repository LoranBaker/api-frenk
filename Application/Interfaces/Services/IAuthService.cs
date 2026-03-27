using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Microsoft.AspNetCore.Identity.Data;

namespace Frank.Application.Interfaces.Services;

public interface IAuthService
{
    /// <summary>
    /// Creates a new Supabase Auth user and user_profiles row.
    /// Returns JWT token for immediate use.
    /// </summary>
    Task<AuthResponse> RegisterAsync(
        DTOs.Requests.RegisterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Authenticates via Supabase Auth.
    /// Returns JWT token.
    /// </summary>
    Task<AuthResponse> LoginAsync(
        DTOs.Requests.LoginRequest request,
        CancellationToken ct = default);
}