using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly AppDbContext _db;

    public DeviceTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveTokenAsync(
        Guid userId,
        string token,
        int timezoneOffset,
        CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (profile is null) return;

        profile.PushToken = token;
        profile.TimezoneOffset = timezoneOffset;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Guid>> GetUsersForMorningAsync(
        int currentUtcHour,
        CancellationToken ct = default)
        // Find users where local time = 8am
        => await _db.UserProfiles
            .Where(u => u.PushToken != null
                     && (currentUtcHour + u.TimezoneOffset + 24) % 24 == 8)
            .Select(u => u.UserId)
            .ToListAsync(ct);

    public async Task<List<Guid>> GetUsersForEveningAsync(
        int currentUtcHour,
        CancellationToken ct = default)
        // Find users where local time = 20 (8pm)
        => await _db.UserProfiles
            .Where(u => u.PushToken != null
                     && (currentUtcHour + u.TimezoneOffset + 24) % 24 == 20)
            .Select(u => u.UserId)
            .ToListAsync(ct);

    public async Task<List<Guid>> GetUsersForWeeklyAsync(
     int currentUtcHour,
     int currentUtcDayOfWeek,
     CancellationToken ct = default)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1);

        // Load only users with push token AND activity this week
        var users = await _db.UserProfiles
            .Where(u =>
                u.PushToken != null &&
                _db.UrgeEvents.Any(e =>
                    e.UserId == u.UserId &&
                    e.Timestamp >= weekStart))
            .ToListAsync(ct);

        // Apply timezone check in memory — EF can't translate this math to SQL
        return users
            .Where(u =>
            {
                var localHour = (currentUtcHour + u.TimezoneOffset + 24) % 24;
                var localDayOffset = (currentUtcHour + u.TimezoneOffset) < 0 ? -1
                                   : (currentUtcHour + u.TimezoneOffset) >= 24 ? 1 : 0;
                var localDayOfWeek = (currentUtcDayOfWeek + localDayOffset + 7) % 7;
                return localHour == 20 && localDayOfWeek == 0;
            })
            .Select(u => u.UserId)
            .ToList();
    }

    public async Task<List<Guid>> GetUsersForMiniMirrorAsync(
    CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _db.UserProfiles
            .Where(u =>
                u.PushToken != null &&
                // Day 3-7 after signup
                (now - u.CreatedAt).TotalDays >= 3 &&
                (now - u.CreatedAt).TotalDays <= 7 &&
                // Not already shown
                !_db.MiniMirrorEvents.Any(m => m.UserId == u.UserId) &&
                // 3+ urge presses
                _db.UrgeEvents.Count(e => e.UserId == u.UserId) >= 3)
            .Select(u => u.UserId)
            .ToListAsync(ct);
    }


}