using Frank.Application.DTOs.Responses;
using Frank.Application.Interfaces.Services;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frank.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly IMorningRepository _morning;
    private readonly IUrgeRepository _urge;
    private readonly IEveningRepository _evening;
    private readonly IWeeklyRepository _weekly;
    private readonly IConversationRepository _conversation;
    private readonly IAnswerRepository _answers;
    private readonly IChatSessionRepository _sessions;
    private readonly ISupabaseAdminService _supabase;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUserRepository users,
        IMorningRepository morning,
        IUrgeRepository urge,
        IEveningRepository evening,
        IWeeklyRepository weekly,
        IConversationRepository conversation,
        IAnswerRepository answers,
        IChatSessionRepository sessions,
        ISupabaseAdminService supabase,
        ILogger<AccountService> logger)
    {
        _users = users;
        _morning = morning;
        _urge = urge;
        _evening = evening;
        _weekly = weekly;
        _conversation = conversation;
        _answers = answers;
        _sessions = sessions;
        _supabase = supabase;
        _logger = logger;
    }

    public async Task DeleteAccountAsync(Guid userId, CancellationToken ct = default)
    {
        // Delete in order — children before parents
        await _conversation.DeleteAllByUserAsync(userId, ct);
        await _answers.DeleteAllByUserAsync(userId, ct);
        await _sessions.DeleteAllByUserAsync(userId, ct);
        await _urge.DeleteAllByUserAsync(userId, ct);
        await _morning.DeleteAllByUserAsync(userId, ct);
        await _evening.DeleteAllByUserAsync(userId, ct);
        await _weekly.DeleteAllByUserAsync(userId, ct);
        await _users.DeleteAsync(userId, ct);

        // Delete Supabase auth user last
        await _supabase.DeleteUserAsync(userId, ct);

        _logger.LogInformation("Account deleted for user {UserId}", userId);
    }

    public async Task<AccountExportDto> ExportDataAsync(
        Guid userId, CancellationToken ct = default)
    {
        var profile = await _users.GetByUserIdAsync(userId, ct)
                    ?? throw new KeyNotFoundException($"User {userId} not found");
        var mornings = await _morning.GetAllByUserAsync(userId, ct);
        var urges = await _urge.GetAllTimeAsync(userId, ct);
        var evenings = await _evening.GetAllByUserAsync(userId, ct);
        var weeklies = await _weekly.GetAllByUserAsync(userId, ct);

        return new AccountExportDto(
            FirstName: profile.FirstName,
            AddictionTypes: profile.AddictionTypes,
            PreUrgeDescription: profile.PreUrgeDescription,
            CoreMotivation: profile.CoreMotivation,
            HabitDuration: profile.HabitDuration,
            CreatedAt: profile.CreatedAt,
            MorningCheckins: mornings.Select(m => new MorningExportDto(
                m.Date, m.ForecastText, m.HighRiskFlag)).ToList(),
            UrgeEvents: urges.Select(u => new UrgeExportDto(
                u.Timestamp, u.AddictionType, u.UrgeIntensity,
                u.Resisted, u.PreUrgeContext, u.HaltRoot)).ToList(),
            EveningCheckins: evenings.Select(e => new EveningExportDto(
                e.Date, e.DayResult, e.TomorrowRisk)).ToList(),
            WeeklySummaries: weeklies.Select(w => new WeeklyExportDto(
                w.WeekStart, w.TotalUrges, w.TotalResisted, w.ResistRate,
                w.Observation1, w.Observation2, w.Observation3,
                w.ReflectionText)).ToList()
        );
    }
}