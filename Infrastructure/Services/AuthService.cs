using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Frank.Infrastructure.Services;

/// <summary>
/// Proxies auth to Supabase Auth REST API.
/// Never handles or stores passwords.
/// Creates user_profiles row after successful registration.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private string SupabaseUrl => _config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
    private string SupabaseAnon => _config["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey not configured");

    public AuthService(
        HttpClient http,
        IUserRepository users,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _http = http;
        _users = users;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        // 1. Call Supabase Auth signup
        var payload = new
        {
            email = request.Email,
            password = request.Password,
            data = new { first_name = request.FirstName }
        };

        var response = await PostToSupabaseAsync(
            "/auth/v1/signup", payload, ct);

        var userId = ExtractUserId(response);
        var token = ExtractToken(response);

        // 2. Create user_profiles stub — onboarding fills the rest
        if (!await _users.ExistsAsync(userId, ct))
        {
            await _users.UpsertAsync(new UserProfile
            {
                UserId = userId,
                FirstName = request.FirstName.Trim(),
                CreatedAt = DateTime.UtcNow,

                // Required fields — onboarding will fill properly
                AddictionTypes = Array.Empty<string>(),
                RiskWindows = Array.Empty<string>(),
                EnvironmentRisks = Array.Empty<string>(),
                ReplacementStack = Array.Empty<string>(),
                PreUrgeDescription = "",
                CoreMotivation = "",
                HabitDuration = ""
            }, ct);
        }

        _logger.LogInformation(
            "User registered successfully. UserId={UserId}", userId);

        return new AuthResponse(UserId: userId, Token: token);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var payload = new
        {
            email = request.Email,
            password = request.Password
        };

        var response = await PostToSupabaseAsync(
            "/auth/v1/token?grant_type=password", payload, ct);

        var userId = ExtractUserId(response);
        var token = ExtractToken(response);

        _logger.LogInformation(
            "User logged in. UserId={UserId}", userId);

        return new AuthResponse(UserId: userId, Token: token);
    }

    // ── Supabase HTTP helpers ─────────────────────────────

    private async Task<JsonDocument> PostToSupabaseAsync(
        string path,
        object payload,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{SupabaseUrl}{path}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("apikey", SupabaseAnon);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Supabase Auth error. Status={Status} Body={Body}",
                response.StatusCode, error);
            throw new UnauthorizedAccessException(
                "Authentication failed. Check your credentials.");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(content);
    }

    private static Guid ExtractUserId(JsonDocument doc)
    {
        // Supabase returns user.id or id at top level
        if (doc.RootElement.TryGetProperty("user", out var user))
        {
            if (user.TryGetProperty("id", out var id))
                return Guid.Parse(id.GetString()!);
        }

        if (doc.RootElement.TryGetProperty("id", out var directId))
            return Guid.Parse(directId.GetString()!);

        throw new InvalidOperationException("Could not extract user_id from Supabase response.");
    }

    private static string ExtractToken(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("access_token", out var token))
            return token.GetString()!;

        throw new InvalidOperationException("Could not extract token from Supabase response.");
    }
}