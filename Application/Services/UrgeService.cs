using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class UrgeService : IUrgeService
{
    private readonly IUrgeRepository _urge;
    private readonly IAnswerRepository _answers;
    private readonly IChatSessionRepository _sessions;
    private readonly IConversationRepository _conversation;
    private readonly IUserRepository _users;
    private readonly IUserSummaryRepository _summaries;
    private readonly IWeeklyRepository _weekly;
    private readonly IInterventionService _intervention;
    private readonly INotificationService _notifications;
    private readonly ICrisisDetectionService _crisis;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<UrgeService> _logger;

    private const int MaxUrgesPerDay = 20;

    public UrgeService(
        IUrgeRepository urge,
        IAnswerRepository answers,
        IChatSessionRepository sessions,
        IConversationRepository conversation,
        IUserRepository users,
        IUserSummaryRepository summaries,
        IWeeklyRepository weekly,
        IInterventionService intervention,
        INotificationService notifications,
        ICrisisDetectionService crisis,
        IAppSettingsService settings,
        ILogger<UrgeService> logger)
    {
        _urge = urge;
        _answers = answers;
        _sessions = sessions;
        _conversation = conversation;
        _users = users;
        _summaries = summaries;
        _weekly = weekly;
        _intervention = intervention;
        _notifications = notifications;
        _crisis = crisis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<StartUrgeResponse> StartAsync(
        Guid userId,
        StartUrgeRequest request,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        // 1. Rate limit using localDate
        var todayCount = await _urge.GetTodayCountAsync(userId, localDate, ct);
        if (todayCount >= MaxUrgesPerDay)
            throw new InvalidOperationException(
                "Daily urge limit reached. That's a lot for one day.");

        // 2. Load user
        var profile = await _users.GetByUserIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        // 3. Slipped today using localDate
        var slippedToday = await _urge.SlippedTodayAsync(userId, localDate, ct);

        // 4. Get this week's resist rate
        var weekStart = GetMondayOfWeek(localDate);
        var weeklyEvents = await _urge.GetThisWeekAsync(userId, weekStart, ct);
        var resistRate = weeklyEvents.Count > 0
            ? (double)weeklyEvents.Count(e => e.Resisted) / weeklyEvents.Count
            : 1.0;

        // 5. Determine halt_root
        var haltRoot = DetermineHaltRoot(request.HaltAnswers);

        // 6. Create chat session using localDate
        var session = await _sessions.CreateAsync(userId, "urge", localDate, ct);

        // 7. Save HALT answers
        if (request.HaltAnswers.Any())
        {
            var haltAnswers = request.HaltAnswers.Select(h => new Answer
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = session.Id,
                SessionType = "urge",
                QuestionId = h.QuestionId,
                QuestionText = h.QuestionId,
                TapValue = h.TapValue,
                Timestamp = DateTime.UtcNow
            });
            await _answers.AddRangeAsync(haltAnswers, ct);
        }

        // 8. Build intervention context
        // FIX 7: Use local hour from the request timezone offset so midnight_regret
        // fires at actual local midnight, not UTC midnight. TimezoneOffsetHours is
        // sent by the frontend (device local offset in whole hours, e.g. +2 for CEST).
        // Guard: clamp offset to ±14 before applying.
        var clampedOffset = Math.Clamp(request.TimezoneOffsetHours, -14, 14);
        var localHour = ((DateTime.UtcNow.Hour + clampedOffset) % 24 + 24) % 24;

        var context = new InterventionContext(
            UserId: userId,
            AddictionType: request.AddictionType,
            UrgeIntensity: request.UrgeIntensity,
            MoodValence: request.MoodValence,
            Environment: request.Environment,
            HaltRoot: haltRoot,
            HabitDuration: profile.HabitDuration,
            IdentityFraming: profile.IdentityFraming,
            SlippedToday: slippedToday,
            ResistRateThisWeek: resistRate,
            HourOfDay: localHour   // ← local hour, not UTC
        );

        // 9. Select intervention
        var intervention = await _intervention.SelectAsync(context, ct);

        // 10. Create urge_events row
        var urgeEvent = new UrgeEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            AddictionType = request.AddictionType,
            UrgeIntensity = request.UrgeIntensity,
            HaltRoot = haltRoot,
            InterventionId = intervention.Id,
            Resisted = false,
            MoodValence = request.MoodValence,
            Environment = request.Environment,
            DayOfWeek = (int)localDate.DayOfWeek,
            SessionId = session.Id
        };

        await _urge.AddAsync(urgeEvent, ct);
        await _urge.SaveChangesAsync(ct);

        // 11. Write conversation history
        await WriteUrgeStartHistoryAsync(userId, session.Id, request, intervention, ct);

        _logger.LogInformation(
            "Urge started for user {UserId}. Addiction={Addiction} Intensity={Intensity} LocalHour={Hour}",
            userId, request.AddictionType, request.UrgeIntensity, localHour);

        return new StartUrgeResponse(
            UrgeEventId: urgeEvent.Id,
            Intervention: intervention);
    }

    public async Task<ResolveUrgeResponse> ResolveAsync(
        Guid userId,
        ResolveUrgeRequest request,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        // 1. Update resisted + pre_urge_context
        await _urge.UpdateResistAsync(request.UrgeEventId, request.Resisted, ct);

        if (!string.IsNullOrWhiteSpace(request.PreUrgeContextTap))
        {
            var evt = await _urge.GetByIdAsync(request.UrgeEventId, ct);
            if (evt is not null)
            {
                evt.PreUrgeContext = request.PreUrgeContextTap;
                await _urge.SaveChangesAsync(ct);
            }
        }

        // 2. Save pre_urge_context to answers table
        if (!string.IsNullOrEmpty(request.PreUrgeContextTap))
        {
            var urgeEvent = await _urge.GetByIdAsync(request.UrgeEventId, ct);
            if (urgeEvent is not null)
            {
                await _answers.AddRangeAsync(new[]
                {
                    new Answer
                    {
                        Id           = Guid.NewGuid(),
                        UserId       = userId,
                        SessionId    = urgeEvent.SessionId,
                        SessionType  = "urge",
                        QuestionId   = "U01",
                        QuestionText = "Just before this — what was happening?",
                        TapValue     = request.PreUrgeContextTap,
                        Timestamp    = DateTime.UtcNow
                    }
                }, ct);
            }
        }

        // 3. Increment daily count
        await _summaries.IncrementDailyUrgeCountAsync(userId, ct);
        var dailyCount = await _urge.GetTodayCountAsync(userId, localDate, ct);

        // 4. Complete session
        var urgeEvt = await _urge.GetByIdAsync(request.UrgeEventId, ct);
        if (urgeEvt is not null)
            await _sessions.CompleteAsync(urgeEvt.SessionId, ct);

        // 5. Write resolve history
        await WriteUrgeResolveHistoryAsync(userId, request, dailyCount, ct);

        // 6. Schedule post-urge notification
        await _notifications.SchedulePostUrgeAsync(request.UrgeEventId, userId, ct);

        // 7. Get counter message
        var counterMessage = _settings.GetCounterMessage(dailyCount);

        _logger.LogInformation(
            "Urge resolved for user {UserId}. Resisted={Resisted} DailyCount={Count}",
            userId, request.Resisted, dailyCount);

        return new ResolveUrgeResponse(
            DailyCount: dailyCount,
            CounterMessage: counterMessage);
    }

    public Task<int> GetTodayCountAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default)
        => _urge.GetTodayCountAsync(userId, localDate, ct);

    public async Task<SuccessResponse> SavePostMoodAsync(
        Guid userId,
        PostUrgeMoodRequest request,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(request.WhatWasThat) &&
            _crisis.ContainsCrisisKeywords(request.WhatWasThat))
        {
            await _crisis.LogCrisisEventAsync(userId, "urge", ct);
        }

        await _urge.UpdatePostMoodAsync(request.UrgeEventId, request.Mood, ct);

        if (!string.IsNullOrEmpty(request.WhatWasThat))
        {
            var urgeEvent = await _urge.GetByIdAsync(request.UrgeEventId, ct);
            if (urgeEvent is not null)
            {
                await _answers.AddRangeAsync(new[]
                {
                    new Answer
                    {
                        Id           = Guid.NewGuid(),
                        UserId       = userId,
                        SessionId    = urgeEvent.SessionId,
                        SessionType  = "urge",
                        QuestionId   = "U01_post",
                        QuestionText = "What was that about?",
                        AnswerText   = request.WhatWasThat,
                        Timestamp    = DateTime.UtcNow
                    }
                }, ct);
            }
        }

        _logger.LogInformation(
            "Post-urge mood saved for user {UserId}. Mood={Mood}", userId, request.Mood);

        return new SuccessResponse();
    }

    // ── Helpers ───────────────────────────────────────────

    private static string? DetermineHaltRoot(HaltAnswer[] haltAnswers)
    {
        foreach (var answer in haltAnswers)
        {
            var value = answer.TapValue.ToLower();

            switch (answer.QuestionId)
            {
                case "H01":
                    if (value == "not_eaten") return "hungry";
                    break;
                case "H02":
                    if (value == "properly_angry") return "angry";
                    break;
                case "H03":
                    if (value == "very_lonely") return "lonely";
                    break;
                case "H04":
                    if (value == "exhausted") return "tired";
                    break;
                case "H05":
                    if (value == "very_stressed") return "stressed";
                    break;
            }
        }
        return null;
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var diff = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff);
    }

    private async Task WriteUrgeStartHistoryAsync(
        Guid userId,
        Guid sessionId,
        StartUrgeRequest request,
        InterventionResponse intervention,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var messages = new List<ConversationHistory>
        {
            new()
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                SessionId   = sessionId,
                SessionType = "urge",
                Date        = today,
                Timestamp   = now,
                Role        = "user",
                Content     = request.AddictionType,
                MessageType = "tap_option",
                TapValue    = request.AddictionType,
                CreatedAt   = now
            },
            new()
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                SessionId   = sessionId,
                SessionType = "urge",
                Date        = today,
                Timestamp   = now.AddMilliseconds(100),
                Role        = "user",
                Content     = request.EmotionTap,
                MessageType = "tap_option",
                TapValue    = request.EmotionTap,
                CreatedAt   = now
            },
            new()
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                SessionId   = sessionId,
                SessionType = "urge",
                Date        = today,
                Timestamp   = now.AddMilliseconds(200),
                Role        = "user",
                Content     = $"{request.UrgeIntensity} / 10",
                MessageType = "tap_option",
                TapValue    = request.UrgeIntensity.ToString(),
                CreatedAt   = now
            },
            new()
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                SessionId   = sessionId,
                SessionType = "urge",
                Date        = today,
                Timestamp   = now.AddMilliseconds(300),
                Role        = "frank",
                Content     = intervention.Text,
                MessageType = "interrupt",
                CreatedAt   = now
            }
        };

        await _conversation.AddRangeAsync(messages, ct);
    }

    private async Task WriteUrgeResolveHistoryAsync(
        Guid userId,
        ResolveUrgeRequest request,
        int dailyCount,
        CancellationToken ct)
    {
        var urgeEvent = await _urge.GetByIdAsync(request.UrgeEventId, ct);
        if (urgeEvent is null) return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var messages = new List<ConversationHistory>
        {
            new()
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                SessionId   = urgeEvent.SessionId,
                SessionType = "urge",
                Date        = today,
                Timestamp   = now,
                Role        = "user",
                Content     = request.Resisted ? "resisted" : "gave_in",
                MessageType = "result_badge",
                TapValue    = request.Resisted ? "resisted" : "gave_in",
                CreatedAt   = now
            }
        };

        var counterMessage = _settings.GetCounterMessage(dailyCount);
        if (counterMessage is not null)
        {
            messages.Add(new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = urgeEvent.SessionId,
                SessionType = "urge",
                Date = today,
                Timestamp = now.AddMilliseconds(100),
                Role = "frank",
                Content = counterMessage,
                MessageType = "system",
                CreatedAt = now
            });
        }

        await _conversation.AddRangeAsync(messages, ct);
    }
}