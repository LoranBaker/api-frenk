namespace Frank.Application.Interfaces.Services;

/// <summary>
/// Provides access to all user-facing strings stored in app_settings table.
/// Loaded at startup, cached in memory, refreshed every 5 minutes.
/// 
/// Nothing user-facing is hardcoded in backend code.
/// Crisis response, counter messages, evening strings,
/// fallbacks, notification texts — all come from here.
/// </summary>
public interface IAppSettingsService
{
    string Get(string key, string fallback = "");

    // ── Crisis ────────────────────────────────────────────
    string GetCrisisResponse();
    string[] GetCrisisKeywords();

    // ── Urge counter messages ─────────────────────────────
    /// <summary>
    /// Returns counter message for given press count.
    /// count 1 → null (silent)
    /// count 2 → "That's twice today. Still watching."
    /// count 3 → "Three times today. The pattern is showing itself."
    /// count 4 → "Four today. Something is driving this harder than usual."
    /// count 5+ → "Five times today. This is worth looking at when you're ready."
    /// </summary>
    string? GetCounterMessage(int count);

    // ── Evening fixed strings ─────────────────────────────
    /// <summary>
    /// Returns fixed response string based on day_result.
    /// stayed_in_control → "Logged. Rest well."
    /// slipped           → "It happens. You still showed up."
    /// hard_day          → "Hard days are data too. See you tomorrow."
    /// </summary>
    string GetEveningResponse(string dayResult);

    // ── Fallbacks ─────────────────────────────────────────
    string GetMorningForecastFallback();
    string GetWeeklyMirrorFallback();
    double GetDouble(string key, double fallback = 0.0);

    // ── Notification texts ────────────────────────────────
    string GetNotificationText(string type);

    // ── Refresh ───────────────────────────────────────────
    Task RefreshAsync(CancellationToken ct = default);
}