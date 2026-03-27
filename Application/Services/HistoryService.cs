using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class HistoryService : IHistoryService
{
    private readonly IWeeklyRepository _weekly;
    private readonly IUrgeRepository _urge;
    private readonly IMorningRepository _morning;
    private readonly IEveningRepository _evening;
    private readonly IConversationRepository _conversation;
    private readonly IUserRepository _users;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(
        IWeeklyRepository weekly,
        IUrgeRepository urge,
        IMorningRepository morning,
        IEveningRepository evening,
        IConversationRepository conversation,
        IUserRepository users,
        ILogger<HistoryService> logger)
    {
        _weekly = weekly;
        _urge = urge;
        _morning = morning;
        _evening = evening;
        _conversation = conversation;
        _users = users;
        _logger = logger;
    }

    /// <summary>
    /// Returns all weeks with aggregated stats.
    /// Week number is relative to user signup date — Week 1, Week 2 etc.
    /// Zero Claude cost — pure DB reads.
    /// </summary>
    public async Task<HistoryWeeksResponse> GetWeeksAsync(
     Guid userId,
     CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        var signupDate = DateOnly.FromDateTime(profile.CreatedAt);

        // Get all distinct dates from conversation history
        var allMessages = await _conversation.GetAllByUserAsync(userId, ct);

        if (!allMessages.Any())
            return new HistoryWeeksResponse(Array.Empty<WeekSummaryResponse>());

        // Group by week
        var weeks = allMessages
            .GroupBy(m => GetMondayOfWeek(m.Date))
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var weekStart = g.Key;
                var urgeCount = g.Count(m => m.SessionType == "urge" && m.MessageType == "result_badge");
                var resistCount = g.Count(m => m.SessionType == "urge" && m.MessageType == "result_badge" && m.TapValue == "resisted");
                var resistRate = urgeCount > 0 ? (double)resistCount / urgeCount : 0;

                return new WeekSummaryResponse(
                    WeekNumber: GetWeekNumber(signupDate, weekStart),
                    DateFrom: weekStart,
                    DateTo: weekStart.AddDays(6),
                    TotalUrges: urgeCount,
                    Resisted: resistCount,
                    ResistRate: resistRate);
            })
            .ToArray();

        return new HistoryWeeksResponse(weeks);
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var diff = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff);
    }

    /// <summary>
    /// Returns full chat thread for a specific day.
    /// Reads conversation_history ordered by timestamp.
    /// Read-only. Crisis-flagged messages excluded.
    /// </summary>
    public async Task<DayChatResponse> GetDayAsync(
        Guid userId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var messages = await _conversation.GetByDateAsync(userId, date, ct);

        var response = messages
    .Where(m => !m.CrisisFlag)
    .OrderBy(m => m.Timestamp)
    .ThenBy(m => m.CreatedAt)
    .Select(m => new ChatMessageResponse(
        Role: m.Role,
        Content: m.Content,
        MessageType: m.MessageType,
        TapValue: m.TapValue,
        Timestamp: m.Timestamp,
        SessionType: m.SessionType    // ← add this
    ))
    .ToArray();

        return new DayChatResponse(response);
    }

    // ── Helpers ───────────────────────────────────────────

    /// <summary>
    /// Returns week number relative to user signup.
    /// Week 1 = first 7 days. Week 2 = days 8-14. Etc.
    /// Never calendar week numbers.
    /// </summary>
    private static int GetWeekNumber(DateOnly signupDate, DateOnly weekStart)
    {
        var daysDiff = weekStart.DayNumber - signupDate.DayNumber;
        return Math.Max(1, (daysDiff / 7) + 1);
    }
}