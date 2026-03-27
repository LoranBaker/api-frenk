using Frank.Application.Interfaces.Services;
using FrankApi.Application.Interfaces.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Frank.Infrastructure.Services;

public class FcmNotificationService : INotificationService
{
    private readonly HttpClient _http;
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;
    private readonly ILogger<FcmNotificationService> _logger;

    private const string FcmEndpoint =
        "https://fcm.googleapis.com/v1/projects/{0}/messages:send";

    public FcmNotificationService(
        HttpClient http,
        IUserRepository users,
        IConfiguration config,
        ILogger<FcmNotificationService> logger)
    {
        _http = http;
        _users = users;
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(
        string deviceToken,
        string title,
        string body,
        string type,
        CancellationToken ct = default)
    {
        try
        {
            var projectId = _config["Firebase:ProjectId"];
            var accessToken = await GetAccessTokenAsync(ct);

            var payload = new
            {
                message = new
                {
                    token = deviceToken,
                    notification = new { title, body },
                    data = new { type },
                    android = new
                    {
                        priority = "high",
                        notification = new
                        {
                            sound = "default",
                            click_action = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var url = string.Format(FcmEndpoint, projectId);
            var response = await _http.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "FCM send failed: {Status} {Error}", response.StatusCode, error);
            }
            else
            {
                _logger.LogInformation(
                    "FCM notification sent. Type={Type}", type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM notification error");
        }
    }

    public async Task SchedulePostUrgeAsync(
        Guid urgeEventId,
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile?.PushToken is null) return;

        // In production — schedule 20-60 min delay
        // For now send after short delay via background task
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(30), ct);
            await SendAsync(
                profile.PushToken,
                "Frank",
                "How are you feeling now? A few minutes later — be honest.",
                "post_urge_mood",
                ct);
        }, ct);
    }

    public async Task SendMorningNudgeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile?.PushToken is null) return;

        await SendAsync(
            profile.PushToken,
            "Frank",
            "Good morning. Check in when you're ready.",
            "morning_nudge",
            ct);
    }

    public async Task SendEveningNudgeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile?.PushToken is null) return;

        await SendAsync(
            profile.PushToken,
            "Frank",
            "How did today actually go?",
            "evening_nudge",
            ct);
    }

    public async Task SendWeeklyMirrorNudgeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile?.PushToken is null) return;

        await SendAsync(
            profile.PushToken,
            "Frank",
            "Your weekly mirror is ready. Frank has been watching.",
            "weekly_mirror",
            ct);
    }

    public async Task SendMiniMirrorNudgeAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile?.PushToken is null) return;

        await SendAsync(
            profile.PushToken,
            "Frank noticed something",
            "Open the app to see what Frank found in your patterns.",
            "mini_mirror",
            ct);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var serviceAccountJson = _config["Firebase:ServiceAccountJson"];

        var credential = GoogleCredential
            .FromJson(serviceAccountJson)
            .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

        var token = await credential.UnderlyingCredential
            .GetAccessTokenForRequestAsync(cancellationToken: ct);

        return token;
    }
}