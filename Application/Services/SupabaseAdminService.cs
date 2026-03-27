using Frank.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Frank.Infrastructure.Services;

public class SupabaseAdminService : ISupabaseAdminService
{
    private readonly HttpClient _http;
    private readonly string _serviceRoleKey;
    private readonly string _supabaseUrl;
    private readonly ILogger<SupabaseAdminService> _logger;

    public SupabaseAdminService(
        HttpClient http,
        IConfiguration config,
        ILogger<SupabaseAdminService> logger)
    {
        _http = http;
        _serviceRoleKey = config["Supabase:ServiceRoleKey"]!;
        _supabaseUrl = config["Supabase:Url"]!;
        _logger = logger;
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_supabaseUrl}/auth/v1/admin/users/{userId}");

        req.Headers.Add("apikey", _serviceRoleKey);
        req.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");

        var res = await _http.SendAsync(req, ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Failed to delete Supabase user {UserId}: {Status} {Body}",
                userId, res.StatusCode, body);
            throw new InvalidOperationException(
                $"Supabase user deletion failed: {res.StatusCode}");
        }

        _logger.LogInformation("Supabase auth user deleted: {UserId}", userId);
    }
}