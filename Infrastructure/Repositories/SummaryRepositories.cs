using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class WeeklyRepository : BaseRepository<WeeklySummary>, IWeeklyRepository
{
    public WeeklyRepository(AppDbContext db) : base(db) { }

    public async Task<WeeklySummary?> GetByWeekStartAsync(
        Guid userId,
        DateOnly weekStart,
        CancellationToken ct = default)
        => await _db.WeeklySummaries
            .FirstOrDefaultAsync(w => w.UserId == userId
                                   && w.WeekStart == weekStart, ct);

    public async Task<WeeklySummary?> GetPreviousWeekAsync(
        Guid userId,
        DateOnly currentWeekStart,
        CancellationToken ct = default)
        => await _db.WeeklySummaries
            .Where(w => w.UserId == userId
                     && w.WeekStart < currentWeekStart)
            .OrderByDescending(w => w.WeekStart)
            .FirstOrDefaultAsync(ct);

    public async Task<List<WeeklySummary>> GetAllForUserAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.WeeklySummaries
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.WeekStart)
            .ToListAsync(ct);

    public async Task DeleteAllByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.WeeklySummaries
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);
        _db.WeeklySummaries.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<WeeklySummary>> GetAllByUserAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.WeeklySummaries
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.WeekStart)
            .ToListAsync(ct);
}
public class UserSummaryRepository : IUserSummaryRepository
{
    private readonly AppDbContext _db;

    public UserSummaryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserSummary?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.UserSummaries
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task UpsertAsync(
        UserSummary summary,
        CancellationToken ct = default)
    {
        var existing = await _db.UserSummaries
            .FirstOrDefaultAsync(s => s.UserId == summary.UserId, ct);

        if (existing is null)
        {
            await _db.UserSummaries.AddAsync(summary, ct);
        }
        else
        {
            existing.Last7Days = summary.Last7Days;
            existing.Last30Days = summary.Last30Days;
            existing.AllTime = summary.AllTime;
            existing.DailyUrgeCount = summary.DailyUrgeCount;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.UserSummaries.Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementDailyUrgeCountAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var summary = await _db.UserSummaries
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (summary is null) return;

        summary.DailyUrgeCount++;
        summary.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetDailyUrgeCountAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var summary = await _db.UserSummaries
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (summary is null) return;

        summary.DailyUrgeCount = 0;
        summary.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

public class PreUrgeVersionRepository : IPreUrgeVersionRepository
{
    private readonly AppDbContext _db;

    public PreUrgeVersionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PreUrgeVersion?> GetLatestAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.PreUrgeVersions
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.VersionNum)
            .FirstOrDefaultAsync(ct);

    public async Task<List<PreUrgeVersion>> GetAllAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.PreUrgeVersions
            .Where(v => v.UserId == userId)
            .OrderBy(v => v.VersionNum)
            .ToListAsync(ct);

    public async Task AddAsync(
        PreUrgeVersion version,
        CancellationToken ct = default)
    {
        await _db.PreUrgeVersions.AddAsync(version, ct);
    }

    public async Task<int> GetNextVersionNumAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var latest = await _db.PreUrgeVersions
            .Where(v => v.UserId == userId)
            .MaxAsync(v => (int?)v.VersionNum, ct);

        return (latest ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly AppDbContext _db;

    public NotificationLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetTodayCountAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.NotificationLogs
            .CountAsync(n => n.UserId == userId && n.Date == today, ct);
    }

    public async Task<bool> WasSentTodayAsync(
        Guid userId,
        string type,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.NotificationLogs
            .AnyAsync(n => n.UserId == userId
                        && n.Type == type
                        && n.Date == today, ct);
    }

    public async Task AddAsync(
        NotificationLog log,
        CancellationToken ct = default)
    {
        await _db.NotificationLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkOpenedAsync(
        Guid logId,
        CancellationToken ct = default)
    {
        var log = await _db.NotificationLogs
            .FindAsync(new object[] { logId }, ct);

        if (log is null) return;

        log.Opened = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

public class MiniMirrorRepository : IMiniMirrorRepository
{
    private readonly AppDbContext _db;

    public MiniMirrorRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> WasTriggeredAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.MiniMirrorEvents
            .AnyAsync(m => m.UserId == userId, ct);

    public async Task AddAsync(
        MiniMirrorEvent evt,
        CancellationToken ct = default)
    {
        await _db.MiniMirrorEvents.AddAsync(evt, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<MiniMirrorEvent?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.MiniMirrorEvents
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public async Task MarkOpenedAsync(
        Guid eventId,
        CancellationToken ct = default)
    {
        var evt = await _db.MiniMirrorEvents
            .FindAsync(new object[] { eventId }, ct);

        if (evt is null) return;

        evt.Opened = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}