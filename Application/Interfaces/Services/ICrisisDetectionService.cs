namespace Frank.Application.Interfaces.Services;

public interface ICrisisDetectionService
{
    /// <summary>
    /// Scans free text for crisis keywords loaded from app_settings.
    /// Synchronous — called inline before any text is processed.
    /// Keywords loaded from app_settings key=crisis_keywords (comma-separated).
    /// Adding new keyword = update one DB row. Zero deployment.
    /// </summary>
    bool ContainsCrisisKeywords(string text);

    /// <summary>
    /// Logs crisis event to conversation_history with crisis_flag=true.
    /// Logs to monitoring: user_id + timestamp only — never logs content.
    /// Never throws — crisis logging must not break the request pipeline.
    /// </summary>
    Task LogCrisisEventAsync(
        Guid userId,
        string sessionType,
        CancellationToken ct = default);
}