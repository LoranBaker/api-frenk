using Frank.Application.Interfaces.Services;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

/// <summary>
/// Loads all app_settings from DB at startup.
/// Cached in memory. Refreshed every 5 minutes.
/// Nothing user-facing is hardcoded — everything comes from here.
/// Numeric thresholds (resist rates, intensity gates) also live here
/// so they can be tuned without a code deploy.
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppSettingsService> _logger;

    private Dictionary<string, string> _cache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private const int RefreshIntervalMinutes = 5;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AppSettingsService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppSettingsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!ShouldRefresh()) return;

        await _lock.WaitAsync(ct);
        try
        {
            if (!ShouldRefresh()) return;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider
                .GetRequiredService<IAppSettingRepository>();

            var all = await repo.GetAllAsync(ct);
            _cache = all;
            _lastRefresh = DateTime.UtcNow;

            _logger.LogInformation(
                "AppSettings refreshed. {Count} settings loaded.", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh AppSettings. Serving cached values.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool ShouldRefresh()
        => DateTime.UtcNow - _lastRefresh >
           TimeSpan.FromMinutes(RefreshIntervalMinutes);

    // ── Core getters ──────────────────────────────────────

    public string Get(string key, string fallback = "")
        => _cache.TryGetValue(key, out var value) ? value : fallback;

    // FIX 5: Numeric getter for configurable thresholds.
    // Intervention routing thresholds (resist rate gates, intensity gates)
    // live in app_settings so they're tunable without a code deploy.
    //
    // Seed these rows in app_settings:
    //   urge_low_resist_threshold  = 0.30
    //   urge_mid_resist_threshold  = 0.50
    //
    // To tune: UPDATE app_settings SET value='0.25' WHERE key='urge_low_resist_threshold'
    // Live immediately — no restart, no deploy.
    public double GetDouble(string key, double fallback = 0.0)
    {
        if (_cache.TryGetValue(key, out var raw) &&
            double.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        _logger.LogWarning(
            "AppSetting '{Key}' not found or not parseable as double. Using fallback {Fallback}.",
            key, fallback);

        return fallback;
    }

    // ── Crisis ────────────────────────────────────────────

    public string GetCrisisResponse()
        => Get("crisis_response",
               "I hear you. What you're feeling is real. Please reach out to someone right now.");

    public string[] GetCrisisKeywords()
    {
        var raw = Get("crisis_keywords", "suicidal,kill myself,want to die,self-harm,end it");
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    // ── Counter messages ──────────────────────────────────

    public string? GetCounterMessage(int count)
    {
        if (count <= 1) return null;

        var key = count >= 15 ? "counter_message_15" : $"counter_message_{count}";
        var msg = Get(key);
        return string.IsNullOrEmpty(msg) ? null : msg;
    }

    // ── Evening fixed strings ─────────────────────────────

    public string GetEveningResponse(string dayResult)
    {
        var key = dayResult switch
        {
            "stayed_in_control" => "evening_response_stayed",
            "slipped" => "evening_response_slipped",
            "hard_day" => "evening_response_hard",
            _ => "evening_response_stayed"
        };

        return Get(key, "Logged. See you tomorrow.");
    }

    // ── Fallbacks ─────────────────────────────────────────

    public string GetMorningForecastFallback()
        => Get("morning_forecast_fallback",
               "You opened the app this morning. That already means something.");

    public string GetWeeklyMirrorFallback()
        => Get("weekly_mirror_fallback",
               "This week happened. You showed up. See you next Sunday.");

    // ── Notification texts ────────────────────────────────

    public string GetNotificationText(string type)
    {
        var key = type switch
        {
            "morning_nudge" => "notif_morning_nudge",
            "evening_gentle" => "notif_evening_gentle",
            "post_urge_mood" => "notif_post_urge_mood",
            _ => $"notif_{type}"
        };

        return Get(key, "Frank is ready when you are.");
    }
}