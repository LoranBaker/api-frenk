using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class EveningService : IEveningService
{
    private readonly IEveningRepository _evening;
    private readonly IAnswerRepository _answers;
    private readonly IChatSessionRepository _sessions;
    private readonly IConversationRepository _conversation;
    private readonly IUrgeRepository _urge;
    private readonly ICrisisDetectionService _crisis;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<EveningService> _logger;

    public EveningService(
        IEveningRepository evening,
        IAnswerRepository answers,
        IChatSessionRepository sessions,
        IConversationRepository conversation,
        IUrgeRepository urge,
        ICrisisDetectionService crisis,
        IAppSettingsService settings,
        ILogger<EveningService> logger)
    {
        _evening = evening;
        _answers = answers;
        _sessions = sessions;
        _conversation = conversation;
        _urge = urge;
        _crisis = crisis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<EveningStatusResponse> GetStatusAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        var completed = await _evening.ExistsTodayAsync(userId, localDate, ct);
        return new EveningStatusResponse(completed);
    }

    public async Task<EveningCheckinResponse> SubmitAsync(
        Guid userId,
        EveningCheckinRequest request,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        // 1. One check-in per day
        if (await _evening.ExistsTodayAsync(userId, localDate, ct))
            throw new InvalidOperationException("Evening check-in already completed today.");

        // 2. Create or get today's evening session
        var session = await _sessions.GetTodayAsync(userId, "evening", ct)
                   ?? await _sessions.CreateAsync(userId, "evening", localDate, ct);

        var sessionId = session.Id;

        // 3. Crisis check on all free text answers
        foreach (var answer in request.Answers.Where(a => !string.IsNullOrEmpty(a.AnswerText)))
        {
            if (_crisis.ContainsCrisisKeywords(answer.AnswerText!))
            {
                await _crisis.LogCrisisEventAsync(userId, "evening", ct);
                return new EveningCheckinResponse(
                    ResponseText: _settings.GetCrisisResponse());
            }
        }

        // 4. Auto-compute used_urge_button using localDate
        var todayUrgeCount = await _urge.GetTodayCountAsync(userId, localDate, ct);
        var usedUrgeButton = todayUrgeCount > 0;

        // 5. Extract E11 and E14 from answers
        var hardEnvironment = GetTapValue(request.Answers, "E11");
        var tomorrowRisk = GetAnswerText(request.Answers, "E14")
                           ?? GetTapValue(request.Answers, "E14");

        // 6. Extract slip types if slipped
        string[]? slipTypes = null;
        if (request.DayResult == "slipped")
        {
            var slipTap = GetTapValue(request.Answers, "E04");
            slipTypes = slipTap is not null ? new[] { slipTap } : null;
        }

        // 7. Save answers
        if (request.Answers.Any())
        {
            var answerEntities = request.Answers.Select(a => new Answer
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                SessionType = "evening",
                QuestionId = a.QuestionId,
                QuestionText = a.QuestionText,
                TapValue = a.TapValue,
                AnswerText = a.AnswerText,
                Timestamp = DateTime.UtcNow
            });
            await _answers.AddRangeAsync(answerEntities, ct);
        }

        // 8. Save evening_checkins row using localDate
        var checkin = new EveningCheckin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = localDate,
            DayResult = request.DayResult,
            SlipType = slipTypes,
            UsedUrgeButton = usedUrgeButton,
            TomorrowRisk = tomorrowRisk,
            HardEnvironment = hardEnvironment,
            SessionId = sessionId
        };

        await _evening.AddAsync(checkin, ct);
        await _evening.SaveChangesAsync(ct);

        // 9. Get fixed response from app_settings
        var responseText = _settings.GetEveningResponse(request.DayResult);

        // 10. Write conversation history
        await WriteConversationHistoryAsync(
            userId, sessionId, request, responseText, localDate, ct);

        // 11. Complete session
        await _sessions.CompleteAsync(sessionId, ct);

        _logger.LogInformation(
            "Evening check-in saved for user {UserId}. DayResult={Result} UsedUrge={UsedUrge}",
            userId, request.DayResult, usedUrgeButton);

        return new EveningCheckinResponse(ResponseText: responseText);
    }

    // ── Helpers ───────────────────────────────────────────

    private static string? GetTapValue(AnswerItem[] answers, string questionId)
        => answers.FirstOrDefault(a => a.QuestionId == questionId)?.TapValue;

    private static string? GetAnswerText(AnswerItem[] answers, string questionId)
        => answers.FirstOrDefault(a => a.QuestionId == questionId)?.AnswerText;

    private async Task WriteConversationHistoryAsync(
        Guid userId,
        Guid sessionId,
        EveningCheckinRequest request,
        string responseText,
        DateOnly localDate,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var messages = new List<ConversationHistory>();

        // Frank's primary question
        messages.Add(new ConversationHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            SessionType = "evening",
            Date = localDate,
            Timestamp = now,
            Role = "frank",
            Content = "How did today actually go?",
            MessageType = "text",
            CreatedAt = now
        });

        // User's day result tap
        messages.Add(new ConversationHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            SessionType = "evening",
            Date = localDate,
            Timestamp = now.AddMilliseconds(100),
            Role = "user",
            Content = request.DayResult,
            MessageType = "tap_option",
            TapValue = request.DayResult,
            CreatedAt = now
        });

        // Follow-up Q&A pairs
        var offset = 200;
        foreach (var answer in request.Answers)
        {
            // Frank's followup question first
            messages.Add(new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                SessionType = "evening",
                Date = localDate,
                Timestamp = now.AddMilliseconds(offset),
                Role = "frank",
                Content = answer.QuestionText,
                MessageType = "text",
                CreatedAt = now
            });
            offset += 100;

            // Then user's answer
            messages.Add(new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                SessionType = "evening",
                Date = localDate,
                Timestamp = now.AddMilliseconds(offset),
                Role = "user",
                Content = answer.AnswerText ?? answer.TapValue,
                MessageType = answer.TapValue is not null ? "tap_option" : "text",
                TapValue = answer.TapValue,
                CreatedAt = now
            });
            offset += 100;
        }

        // Frank's closing response
        messages.Add(new ConversationHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            SessionType = "evening",
            Date = localDate,
            Timestamp = now.AddMilliseconds(offset),
            Role = "frank",
            Content = responseText,
            MessageType = "forecast",
            CreatedAt = now
        });

        await _conversation.AddRangeAsync(messages, ct);
    }
}