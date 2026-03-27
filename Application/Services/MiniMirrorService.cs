using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Frank.Application.Services;

public class MiniMirrorService : IMiniMirrorService
{
    private readonly IMiniMirrorRepository _miniMirror;
    private readonly IUrgeRepository _urge;
    private readonly IUserRepository _users;
    private readonly IClaudeService _claude;
    private readonly ILogger<MiniMirrorService> _logger;

    private const int MinUrgeCount = 3;
    private const int MinDaysAfterSignup = 3;   // ← set back to 3 after testing
    private const int MaxDaysAfterSignup = 7; // ← set back to 7 after testing
    private readonly IConversationRepository _conversation;
    private readonly IChatSessionRepository _sessions;
    private readonly INotificationService _notificationService;

    public MiniMirrorService(
    IMiniMirrorRepository miniMirror,
    IUrgeRepository urge,
    IUserRepository users,
    IClaudeService claude,
    IConversationRepository conversation,
    IChatSessionRepository sessions,        // ← add
    ILogger<MiniMirrorService> logger,
    INotificationService notificationService)
    {
        _miniMirror = miniMirror;
        _urge = urge;
        _users = users;
        _claude = claude;
        _conversation = conversation;
        _sessions = sessions;              // ← add
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<MiniMirrorResponse?> TryGenerateAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        // 1. Load user
        var profile = await _users.GetByUserIdAsync(userId, ct);
        if (profile is null) return null;

        // 2. Check day window — day 3-7 after signup
        var daysSinceSignup = (DateTime.UtcNow - profile.CreatedAt).TotalDays;
        if (daysSinceSignup < MinDaysAfterSignup ||
            daysSinceSignup > MaxDaysAfterSignup)
        {
            _logger.LogInformation(
                "MiniMirror: user {UserId} not in day window ({Days:F1} days since signup)",
                userId, daysSinceSignup);
            return null;
        }

        // 3. Already shown — never show twice
        if (await _miniMirror.WasTriggeredAsync(userId, ct))
        {
            _logger.LogInformation(
                "MiniMirror: already shown to user {UserId}", userId);
            return null;
        }

        // 4. Get ALL urge events — context is optional
        var allUrges = await _urge.GetAllTimeAsync(userId, ct);
        if (allUrges.Count < MinUrgeCount)
        {
            _logger.LogInformation(
                "MiniMirror: user {UserId} only has {Count} total urges — need {Min}",
                userId, allUrges.Count, MinUrgeCount);
            return null;
        }

        // 5. Extract quotes from urges that have context
        var quotes = allUrges
            .Where(e => !string.IsNullOrWhiteSpace(e.PreUrgeContext))
            .OrderByDescending(e => e.Timestamp)
            .Take(5)
            .Select(e => e.PreUrgeContext!)
            .ToList();

        // If no quotes written — use a fallback so Claude still has something
        if (quotes.Count == 0)
        {
            quotes = new List<string>
            {
                $"Pressed the button {allUrges.Count} times without writing anything."
            };
        }

        // 6. Generate Claude observation
        var observation = await _claude.GetMiniMirrorObservationAsync(
            quotes, profile, ct);

        // 7. Save to mini_mirror_events
        var miniMirrorEvent = new MiniMirrorEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TriggeredAt = DateTime.UtcNow,
            UrgeCount = allUrges.Count,
            QuotesShown = JsonSerializer.Serialize(quotes),
            Observation = observation,
            Opened = false
        };

        await _miniMirror.AddAsync(miniMirrorEvent, ct);
        await _miniMirror.SaveChangesAsync(ct);
        // Create a real chat session for the mini mirror
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var session = await _sessions.CreateAsync(userId, "mini_mirror", today, ct);

        // Write to conversation history
        await _conversation.AddRangeAsync(new[]
        {
    new ConversationHistory
    {
        Id          = Guid.NewGuid(),
        UserId      = userId,
        SessionId   = session.Id,        // ← real session ID not mini mirror event ID
        SessionType = "mini_mirror",
        Date        = today,
        Timestamp   = DateTime.UtcNow,
        Role        = "frank",
        Content     = observation,
        MessageType = "mini_mirror",
        CreatedAt   = DateTime.UtcNow
    }
}, ct);
        await _notificationService.SendMiniMirrorNudgeAsync(userId, ct);



        _logger.LogInformation(
            "MiniMirror generated for user {UserId}. Quotes={Count} Observation={Obs}",
            userId, quotes.Count, observation);

        return new MiniMirrorResponse(
            Quotes: quotes.ToArray(),
            Observation: observation,
            UrgeCount: allUrges.Count);
    }

    public async Task<MiniMirrorEvent?> GetShownAsync(
    Guid userId, CancellationToken ct = default)
    => await _miniMirror.GetByUserIdAsync(userId, ct);
}