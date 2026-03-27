using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

/// <summary>
/// Scans free text for crisis keywords.
/// Keywords loaded from app_settings — never hardcoded.
/// Adding keyword = update one DB row. Zero deployment.
/// </summary>
public class CrisisDetectionService : ICrisisDetectionService
{
    private readonly IAppSettingsService _settings;
    private readonly IConversationRepository _conversation;
    private readonly ILogger<CrisisDetectionService> _logger;

    public CrisisDetectionService(
        IAppSettingsService settings,
        IConversationRepository conversation,
        ILogger<CrisisDetectionService> logger)
    {
        _settings = settings;
        _conversation = conversation;
        _logger = logger;
    }

    public bool ContainsCrisisKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var lower = text.ToLowerInvariant();
        var keywords = _settings.GetCrisisKeywords();

        return keywords.Any(kw =>
            lower.Contains(kw.ToLowerInvariant()));
    }

    /// <summary>
    /// Logs crisis event. Never throws — must not break request pipeline.
    /// Logs user_id + timestamp only — never logs content.
    /// </summary>
    public async Task LogCrisisEventAsync(
        Guid userId,
        string sessionType,
        CancellationToken ct = default)
    {
        try
        {
            var message = new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = Guid.Empty,   // no session context during crisis
                SessionType = sessionType,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Timestamp = DateTime.UtcNow,
                Role = "system",
                Content = null,         // never store crisis content
                MessageType = "system",
                CrisisFlag = true,
                CreatedAt = DateTime.UtcNow
            };

            await _conversation.AddAsync(message, ct);

            // Log user_id + timestamp only — never log the text that triggered this
            _logger.LogWarning(
                "CRISIS_FLAG: UserId={UserId} SessionType={SessionType} At={At}",
                userId, sessionType, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Swallow — crisis logging must never crash the pipeline
            _logger.LogError(ex,
                "Failed to log crisis event for user {UserId}", userId);
        }
    }
}