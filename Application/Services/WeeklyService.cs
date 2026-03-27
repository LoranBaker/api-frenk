using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using Frank.Domain.Entities;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Frank.Application.Services;

public class WeeklyService : IWeeklyService
{
    private readonly IWeeklyRepository _weekly;
    private readonly IUrgeRepository _urge;
    private readonly IAnswerRepository _answers;
    private readonly IQuestionRepository _questions;
    private readonly IUserRepository _users;
    private readonly IConversationRepository _conversation;
    private readonly IChatSessionRepository _sessions;
    private readonly IClaudeService _claude;
    private readonly IAppSettingsService _settings;
    private readonly ILogger<WeeklyService> _logger;

    public WeeklyService(
        IWeeklyRepository weekly,
        IUrgeRepository urge,
        IAnswerRepository answers,
        IQuestionRepository questions,
        IUserRepository users,
        IConversationRepository conversation,
        IChatSessionRepository sessions,
        IClaudeService claude,
        IAppSettingsService settings,
        ILogger<WeeklyService> logger)
    {
        _weekly = weekly;
        _urge = urge;
        _answers = answers;
        _questions = questions;
        _users = users;
        _conversation = conversation;
        _sessions = sessions;
        _claude = claude;
        _settings = settings;
        _logger = logger;
    }

    public async Task<WeeklyMirrorResponse> GetMirrorAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var weekStart = GetMondayOfWeek(DateOnly.FromDateTime(DateTime.UtcNow));

        // Return cached if already generated this week
        var existing = await _weekly.GetByWeekStartAsync(userId, weekStart, ct);
        if (existing is not null && existing.Observation1 is not null)
            return await BuildResponseFromSummary(existing, ct);

        // Generate new
        var summary = await GenerateWeeklyMirrorAsync(userId, weekStart, ct);
        return await BuildResponseFromSummary(summary, ct);
    }

    public async Task<SuccessResponse> SaveReflectionAsync(
    Guid userId,
    WeeklyReflectionRequest request,
    CancellationToken ct = default)
    {
        var summary = await _weekly.GetByWeekStartAsync(
            userId, request.WeekStart, ct);

        if (summary is null)
            throw new KeyNotFoundException("Weekly summary not found.");

        summary.ReflectionText = request.ReflectionText.Trim();
        await _weekly.UpdateAsync(summary, ct);
        await _weekly.SaveChangesAsync(ct);

        // Get or create weekly session for today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var session = await _sessions.GetTodayAsync(userId, "weekly", ct)
                   ?? await _sessions.CreateAsync(userId, "weekly", today, ct);

        // Write reflection to conversation history
        await _conversation.AddRangeAsync(new[]
        {
        new ConversationHistory
        {
            Id          = Guid.NewGuid(),
            UserId      = userId,
            SessionId   = session.Id,
            SessionType = "weekly",
            Date        = today,
            Timestamp   = DateTime.UtcNow,
            Role        = "user",
            Content     = request.ReflectionText.Trim(),
            MessageType = "weekly_reflection",
            CreatedAt   = DateTime.UtcNow
        }
    }, ct);

        _logger.LogInformation(
            "Weekly reflection saved for user {UserId}", userId);

        return new SuccessResponse();
    }

    // ── Generation ────────────────────────────────────────

    public async Task<WeeklySummary> GenerateWeeklyMirrorAsync(
        Guid userId,
        DateOnly weekStart,
        CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(6);

        // Load all data needed
        var profile = await _users.GetByUserIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");
        var weekEvents = await _urge.GetThisWeekAsync(userId, weekStart, ct);
        if (!weekEvents.Any())
        {
            var empty = new WeeklySummary
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeekStart = weekStart,
                TotalUrges = 0,
                TotalResisted = 0,
                ResistRate = 0,
                Observation1 = _settings.GetWeeklyMirrorFallback()
            };

            try
            {
                await _weekly.AddAsync(empty, ct);
                await _weekly.SaveChangesAsync(ct);
            }
            catch
            {
                // Already exists — just return it
                var existingSummary = await _weekly.GetByWeekStartAsync(userId, weekStart, ct);
                if (existingSummary is not null) return existingSummary;
            }

            return empty;
        }
        var prevWeek = await _weekly.GetPreviousWeekAsync(userId, weekStart, ct);
        var preUrgeCtxs = await _answers.GetPreUrgeContextsAsync(
            userId, weekStart, weekEnd, ct);

        // Aggregate stats
        var totalUrges = weekEvents.Count;
        var totalResisted = weekEvents.Count(e => e.Resisted);
        var resistRate = totalUrges > 0
            ? (double)totalResisted / totalUrges : 0;
        var prevRate = prevWeek?.ResistRate ?? 0;
        var delta = resistRate - prevRate;
        var worstDay = GetWorstDay(weekEvents);
        var worstHour = GetWorstHour(weekEvents);
        var topHalt = GetTopHaltRoot(weekEvents);

        // Build or get existing summary row
        var summary = await _weekly.GetByWeekStartAsync(userId, weekStart, ct)
            ?? new WeeklySummary
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeekStart = weekStart
            };

        summary.TotalUrges = totalUrges;
        summary.TotalResisted = totalResisted;
        summary.ResistRate = resistRate;
        summary.ResistRateDelta = delta;
        summary.WorstDay = worstDay;
        summary.WorstHour = worstHour;
        summary.TopHaltRoot = topHalt;

        // Call Claude for observations + quotes
        var claudeContext = new WeeklyMirrorContext(
            Profile: profile,
            Summary: summary,
            PreUrgeContexts: preUrgeCtxs,
            PreviousWeek: prevWeek);

        var observations = await _claude.GetWeeklyObservationsAsync(
            claudeContext, ct);

        if (observations.Any())
        {
            summary.Observation1 = observations.ElementAtOrDefault(0)?.Observation;
            summary.Observation2 = observations.ElementAtOrDefault(1)?.Observation;
            summary.Observation3 = observations.ElementAtOrDefault(2)?.Observation;
            summary.QuotesJson = JsonSerializer.Serialize(
                observations.Select((o, i) => new { obs_index = i, quotes = o.Quotes }));
        }
        else
        {
            // Fallback observations
            summary.Observation1 = _settings.GetWeeklyMirrorFallback();
        }

        // Get reflection question
        if (summary.Observation1 is not null)
        {
            var recentQIds = await GetRecentReflectionQuestionIdsAsync(userId, ct);
            var reflectionQ = await _claude.GetReflectionQuestionAsync(
                new ReflectionContext(
                    Observation1: summary.Observation1 ?? "",
                    Observation2: summary.Observation2 ?? "",
                    Observation3: summary.Observation3 ?? "",
                    RecentQuestionIds: recentQIds), ct);

            summary.ReflectionQId = reflectionQ is not null
                ? $"W_CUSTOM_{weekStart}"
                : "W01";

            // If custom question — store it as a new question in DB
            // For MVP: store W01 as fallback
        }

        // Always try insert first — if it exists, update it
        var existing = await _weekly.GetByWeekStartAsync(userId, weekStart, ct);
        if (existing is null)
        {
            await _weekly.AddAsync(summary, ct);
        }
        else
        {
            // Update existing row fields
            existing.TotalUrges = summary.TotalUrges;
            existing.TotalResisted = summary.TotalResisted;
            existing.ResistRate = summary.ResistRate;
            existing.ResistRateDelta = summary.ResistRateDelta;
            existing.WorstDay = summary.WorstDay;
            existing.WorstHour = summary.WorstHour;
            existing.TopHaltRoot = summary.TopHaltRoot;
            existing.Observation1 = summary.Observation1;
            existing.Observation2 = summary.Observation2;
            existing.Observation3 = summary.Observation3;
            existing.QuotesJson = summary.QuotesJson;
            await _weekly.UpdateAsync(existing, ct);
        }

        await _weekly.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Weekly mirror generated for user {UserId}. ResistRate={Rate:P0}",
            userId, resistRate);

        return summary;
    }

    // ── Response builder ──────────────────────────────────

    private async Task<WeeklyMirrorResponse> BuildResponseFromSummary(
        WeeklySummary summary,
        CancellationToken ct)
    {
        // Parse quotes JSON
        var observations = new List<ObservationWithQuotes>();

        if (summary.QuotesJson is not null)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<QuotesJson>>(
                    summary.QuotesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var obs = new[] { summary.Observation1, summary.Observation2, summary.Observation3 };
                observations = obs
                    .Select((o, i) =>
                    {
                        if (o is null) return null;
                        var quotes = parsed?
                            .FirstOrDefault(p => p.ObsIndex == i)?.Quotes
                            ?? Array.Empty<string>();
                        return new ObservationWithQuotes(o, quotes);
                    })
                    .Where(o => o is not null)
                    .Cast<ObservationWithQuotes>()
                    .ToList();
            }
            catch
            {
                // Fallback without quotes
                var obs = new[] { summary.Observation1, summary.Observation2, summary.Observation3 };
                observations = obs
                    .Where(o => o is not null)
                    .Select(o => new ObservationWithQuotes(o!, Array.Empty<string>()))
                    .ToList();
            }
        }

        // Load reflection question
        QuestionResponse? reflectionQuestion = null;
        if (summary.ReflectionQId is not null)
        {
            var q = await _questions.GetByIdAsync(summary.ReflectionQId, ct);
            if (q is not null)
            {
                reflectionQuestion = new QuestionResponse(
                    Id: q.Id,
                    Text: q.Text,
                    InputType: q.InputType,
                    Frame: q.Frame,
                    TapOptions: Array.Empty<TapOptionResponse>());
            }
        }

        return new WeeklyMirrorResponse(
            Observations: observations.ToArray(),
            ReflectionQuestion: reflectionQuestion,
            ResistRate: summary.ResistRate,
            TotalUrges: summary.TotalUrges,
            WorstDay: summary.WorstDay,
            WorstHour: summary.WorstHour);
    }

    // ── Helpers ───────────────────────────────────────────

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var diff = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff);
    }

    private static string? GetWorstDay(List<UrgeEvent> events)
    {
        if (!events.Any()) return null;
        return events
            .Where(e => !e.Resisted)
            .GroupBy(e => e.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .Select(g => Enum.GetName(typeof(DayOfWeek), g.Key))
            .FirstOrDefault();
    }

    private static int? GetWorstHour(List<UrgeEvent> events)
    {
        if (!events.Any()) return null;
        return events
            .GroupBy(e => e.Timestamp.Hour)
            .OrderByDescending(g => g.Count())
            .Select(g => (int?)g.Key)
            .FirstOrDefault();
    }

    private static string? GetTopHaltRoot(List<UrgeEvent> events)
    {
        return events
            .Where(e => e.HaltRoot is not null)
            .GroupBy(e => e.HaltRoot!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    private async Task<string[]> GetRecentReflectionQuestionIdsAsync(
        Guid userId,
        CancellationToken ct)
    {
        // Get last 4 weeks of reflection question IDs to avoid repeating
        var allSummaries = await _weekly.GetAllForUserAsync(userId, ct);
        return allSummaries
            .OrderByDescending(s => s.WeekStart)
            .Take(4)
            .Where(s => s.ReflectionQId is not null)
            .Select(s => s.ReflectionQId!)
            .ToArray();
    }

    private record QuotesJson(
        int ObsIndex,
        string[] Quotes);
}