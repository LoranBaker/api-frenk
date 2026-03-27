using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class MorningRepository : BaseRepository<MorningCheckin>, IMorningRepository
{
    public MorningRepository(AppDbContext db) : base(db) { }

    public async Task<MorningCheckin?> GetTodayAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
        => await _db.MorningCheckins
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == date, ct);

    public async Task<bool> ExistsTodayAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
        => await _db.MorningCheckins
            .AnyAsync(m => m.UserId == userId && m.Date == date, ct);

    public async Task<MorningCheckin?> GetYesterdayAsync(
        Guid userId, CancellationToken ct = default)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        return await _db.MorningCheckins
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == yesterday, ct);
    }

    public async Task DeleteAllByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.MorningCheckins
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);
        _db.MorningCheckins.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MorningCheckin>> GetAllByUserAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.MorningCheckins
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Date)
            .ToListAsync(ct);
}

public class UrgeRepository : BaseRepository<UrgeEvent>, IUrgeRepository
{
    public UrgeRepository(AppDbContext db) : base(db) { }

    public async Task<UrgeEvent?> GetByIdAsync(
        Guid urgeEventId, CancellationToken ct = default)
        => await _db.UrgeEvents
            .FindAsync(new object[] { urgeEventId }, ct);

    public async Task<int> GetTodayCountAsync(
        Guid userId, CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        return await _db.UrgeEvents
            .CountAsync(e => e.UserId == userId
                          && e.Timestamp >= todayStart
                          && e.Timestamp < todayEnd, ct);
    }

    public async Task<int> GetTodayCountAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return await _db.UrgeEvents
            .CountAsync(e => e.UserId == userId
                          && e.Timestamp >= start
                          && e.Timestamp < end, ct);
    }

    public async Task<List<UrgeEvent>> GetThisWeekAsync(
        Guid userId, DateOnly weekStart, CancellationToken ct = default)
    {
        var startDt = DateTime.SpecifyKind(
            weekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDt = DateTime.SpecifyKind(
            weekStart.AddDays(7).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        return await _db.UrgeEvents
            .Where(e => e.UserId == userId
                     && e.Timestamp >= startDt
                     && e.Timestamp < endDt)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);
    }

    public async Task<bool> SlippedTodayAsync(
        Guid userId, CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        return await _db.UrgeEvents
            .AnyAsync(e => e.UserId == userId
                        && e.Resisted == false
                        && e.Timestamp >= todayStart
                        && e.Timestamp < todayEnd, ct);
    }

    public async Task<bool> SlippedTodayAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return await _db.UrgeEvents
            .AnyAsync(e => e.UserId == userId
                        && !e.Resisted
                        && e.Timestamp >= start
                        && e.Timestamp < end, ct);
    }

    public async Task UpdateResistAsync(
        Guid urgeEventId, bool resisted, CancellationToken ct = default)
    {
        var evt = await _db.UrgeEvents
            .FindAsync(new object[] { urgeEventId }, ct);
        if (evt is null) return;
        evt.Resisted = resisted;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePostMoodAsync(
        Guid urgeEventId, string mood, CancellationToken ct = default)
    {
        var evt = await _db.UrgeEvents
            .FindAsync(new object[] { urgeEventId }, ct);
        if (evt is null) return;
        evt.PostUrgeMood = mood;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetDaysSinceLastSlipAsync(
        Guid userId, CancellationToken ct = default)
    {
        var lastSlip = await _db.UrgeEvents
            .Where(e => e.UserId == userId && e.Resisted == false)
            .OrderByDescending(e => e.Timestamp)
            .Select(e => (DateTime?)e.Timestamp)
            .FirstOrDefaultAsync(ct);
        if (lastSlip is null) return 999;
        return (int)(DateTime.UtcNow - lastSlip.Value).TotalDays;
    }

    public async Task<List<UrgeEvent>> GetAllWithContextAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.UrgeEvents
            .Where(e => e.UserId == userId
                     && e.PreUrgeContext != null
                     && e.PreUrgeContext != "")
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task<List<UrgeEvent>> GetAllTimeAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.UrgeEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task DeleteAllByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.UrgeEvents          // ← fixed: was MorningCheckins
            .Where(e => e.UserId == userId)
            .ToListAsync(ct);
        _db.UrgeEvents.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }
}

public class EveningRepository : BaseRepository<EveningCheckin>, IEveningRepository
{
    public EveningRepository(AppDbContext db) : base(db) { }

    public async Task<EveningCheckin?> GetTodayAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
        => await _db.EveningCheckins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Date == date, ct);

    public async Task<bool> ExistsTodayAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
        => await _db.EveningCheckins
            .AnyAsync(e => e.UserId == userId && e.Date == date, ct);

    public async Task<EveningCheckin?> GetByDateAsync(
        Guid userId, DateOnly date, CancellationToken ct = default)
        => await _db.EveningCheckins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Date == date, ct);

    public async Task DeleteAllByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.EveningCheckins     // ← fixed: was MorningCheckins
            .Where(e => e.UserId == userId)
            .ToListAsync(ct);
        _db.EveningCheckins.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<EveningCheckin>> GetAllByUserAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.EveningCheckins             // ← fixed: was MorningCheckins
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
}