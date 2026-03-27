using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class MorningService : IMorningService
{
    private readonly IMorningRepository _morning;
    private readonly IAnswerRepository _answers;
    private readonly IChatSessionRepository _sessions;
    private readonly IConversationRepository _conversation;
    private readonly IUserRepository _users;
    private readonly IUserSummaryRepository _summaries;
    private readonly IEveningRepository _evening;
    private readonly IClaudeService _claude;
    private readonly ICrisisDetectionService _crisis;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<MorningService> _logger;

    public MorningService(
        IMorningRepository morning,
        IAnswerRepository answers,
        IChatSessionRepository sessions,
        IConversationRepository conversation,
        IUserRepository users,
        IUserSummaryRepository summaries,
        IEveningRepository evening,
        IClaudeService claude,
        ICrisisDetectionService crisis,
        IAppSettingsService settings,
        ILogger<MorningService> logger)
    {
        _morning = morning;
        _answers = answers;
        _sessions = sessions;
        _conversation = conversation;
        _users = users;
        _summaries = summaries;
        _evening = evening;
        _claude = claude;
        _crisis = crisis;
        _settings = settings;
        _logger = logger;
    }

    public async Task<MorningStatusResponse> GetStatusAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        var checkin = await _morning.GetTodayAsync(userId, localDate, ct);
        return new MorningStatusResponse(
            Completed: checkin is not null,
            ForecastText: checkin?.ForecastText);
    }

    public async Task<MorningCheckinResponse> SubmitAsync(
        Guid userId,
        MorningCheckinRequest request,
        DateOnly localDate,
        CancellationToken ct = default)
    {
        // 1. One check-in per day
        if (await _morning.ExistsTodayAsync(userId, localDate, ct))
            throw new InvalidOperationException("Morning check-in already completed today.");

        // 2. Create or get today's session
        var session = await _sessions.GetTodayAsync(userId, "morning", ct)
                   ?? await _sessions.CreateAsync(userId, "morning", localDate, ct);

        var sessionId = session.Id;

        // 3. Load user profile and summary
        var profile = await _users.GetByUserIdAsync(userId, ct)
                   ?? throw new KeyNotFoundException($"User {userId} not found");
        var summary = await _summaries.GetByUserIdAsync(userId, ct);

        // 4. Crisis check on all free text answers
        foreach (var answer in request.Answers.Where(a => !string.IsNullOrEmpty(a.AnswerText)))
        {
            if (_crisis.ContainsCrisisKeywords(answer.AnswerText!))
            {
                await _crisis.LogCrisisEventAsync(userId, "morning", ct);
                return new MorningCheckinResponse(
                    ForecastText: _settings.GetCrisisResponse(),
                    SessionId: sessionId);
            }
        }

        // 5. Save answers
        var answerEntities = request.Answers.Select(a => new Answer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            SessionType = "morning",
            QuestionId = a.QuestionId,
            QuestionText = a.QuestionText,
            TapValue = a.TapValue,
            AnswerText = a.AnswerText,
            Timestamp = DateTime.UtcNow
        }).ToList();

        await _answers.AddRangeAsync(answerEntities, ct);

        // 6. Extract key values
        var moodAnswer = GetTapValue(request.Answers, "M01");
        var sleepAnswer = GetTapValue(request.Answers, "M10");
        var structureAnswer = GetTapValue(request.Answers, "M04");
        var yesterdayScore = GetAnswerText(request.Answers, "M03");
        var emotionalCarry = GetAnswerText(request.Answers, "M02")
                           ?? GetAnswerText(request.Answers, "M11");

        // 7. Compute high_risk_flag
        var highRisk = ComputeHighRiskFlag(moodAnswer, sleepAnswer, structureAnswer);

        // 8. Get yesterday's tomorrow_risk using localDate
        var yesterday = await _evening.GetByDateAsync(userId, localDate.AddDays(-1), ct);
        var tomorrowRisk = yesterday?.TomorrowRisk;

        // 9. Build Claude context and get forecast
        int.TryParse(yesterdayScore, out var score);
        var context = new MorningForecastContext(
            Profile: profile,
            Summary: summary,
            MoodState: moodAnswer ?? "",
            SleepQuality: sleepAnswer ?? "",
            DayStructure: structureAnswer ?? "",
            EmotionalCarry: emotionalCarry,
            TomorrowRisk: tomorrowRisk,
            YesterdayScore: score > 0 ? score : null,
            Question1Text: request.Answers.ElementAtOrDefault(0)?.QuestionText ?? "",
            Answer1: GetFirstAnswer(request.Answers, 0),
            Question2Text: request.Answers.ElementAtOrDefault(1)?.QuestionText ?? "",
            Answer2: GetFirstAnswer(request.Answers, 1)
        );

        var forecastText = await _claude.GetMorningForecastAsync(context, ct);

        // 10. Save morning_checkins row using localDate
        var checkin = new MorningCheckin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = localDate,
            HighRiskFlag = highRisk,
            ForecastText = forecastText,
            YesterdayScore = score > 0 ? score : null,
            SessionId = sessionId
        };

        await _morning.AddAsync(checkin, ct);
        await _morning.SaveChangesAsync(ct);

        // 11. Write conversation history using localDate
        await WriteConversationHistoryAsync(
            userId, request.Answers, forecastText, sessionId, localDate, ct);

        _logger.LogInformation(
            "Morning check-in saved for user {UserId}. HighRisk={HighRisk}",
            userId, highRisk);

        return new MorningCheckinResponse(
            ForecastText: forecastText,
            SessionId: sessionId);
    }

    // ── Helpers ───────────────────────────────────────────

    private static bool ComputeHighRiskFlag(
        string? mood,
        string? sleep,
        string? structure)
    {
        var heavyMood = mood is "tired_behind" or "heavy_carryover" or "flat_baseline";
        var badSleep = sleep is "barely_slept" or "interrupted_sleep";
        var unstructuredDay = structure is "open_unstructured";
        var riskCount = (heavyMood ? 1 : 0) + (badSleep ? 1 : 0) + (unstructuredDay ? 1 : 0);
        return riskCount >= 2;
    }

    private static string? GetTapValue(AnswerItem[] answers, string questionId)
        => answers.FirstOrDefault(a => a.QuestionId == questionId)?.TapValue;

    private static string? GetAnswerText(AnswerItem[] answers, string questionId)
        => answers.FirstOrDefault(a => a.QuestionId == questionId)?.AnswerText;

    private static string GetFirstAnswer(AnswerItem[] answers, int index)
    {
        var answer = answers.ElementAtOrDefault(index);
        if (answer is null) return "";
        return answer.AnswerText ?? answer.TapValue ?? "";
    }

    private async Task WriteConversationHistoryAsync(
        Guid userId,
        AnswerItem[] answers,
        string forecastText,
        Guid sessionId,
        DateOnly localDate,
        CancellationToken ct)
    {
        var messages = new List<ConversationHistory>();
        var now = DateTime.UtcNow;
        var offset = 0;

        foreach (var answer in answers)
        {
            // Frank's question
            messages.Add(new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                SessionType = "morning",
                Date = localDate,
                Timestamp = now.AddSeconds(offset),
                Role = "frank",
                Content = answer.QuestionText,
                MessageType = "text",
                CreatedAt = now.AddSeconds(offset)
            });
            offset++;

            // User's answer
            var content = string.IsNullOrWhiteSpace(answer.AnswerText) || answer.AnswerText == "null"
                ? answer.TapValue
                : answer.AnswerText;

            messages.Add(new ConversationHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                SessionType = "morning",
                Date = localDate,
                Timestamp = now.AddSeconds(offset),
                Role = "user",
                Content = content,
                MessageType = answer.TapValue is not null ? "tap_option" : "text",
                TapValue = answer.TapValue,
                CreatedAt = now.AddSeconds(offset)
            });
            offset++;
        }

        // Frank's forecast
        messages.Add(new ConversationHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            SessionType = "morning",
            Date = localDate,
            Timestamp = now.AddSeconds(offset),
            Role = "frank",
            Content = forecastText,
            MessageType = "forecast",
            CreatedAt = now.AddSeconds(offset)
        });

        await _conversation.AddRangeAsync(messages, ct);
    }


}