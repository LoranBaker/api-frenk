using Frank.Domain.Entities;
using Frank.Infrastructure.Data;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Frank.Infrastructure.Repositories;

public class UserRepository : BaseRepository<UserProfile>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<UserProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _db.UserProfiles
            .AnyAsync(u => u.UserId == userId, ct);

    public async Task UpsertAsync(
        UserProfile profile,
        CancellationToken ct = default)
    {
        var existing = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == profile.UserId, ct);

        if (existing is null)
        {
            await _db.UserProfiles.AddAsync(profile, ct);
        }
        else
        {
            existing.FirstName = profile.FirstName;
            existing.AddictionTypes = profile.AddictionTypes;
            existing.RiskWindows = profile.RiskWindows;
            existing.PreUrgeDescription = profile.PreUrgeDescription;
            existing.CoreMotivation = profile.CoreMotivation;
            existing.EnvironmentRisks = profile.EnvironmentRisks;
            existing.ReplacementStack = profile.ReplacementStack;
            existing.HabitDuration = profile.HabitDuration;
            existing.IdentityFraming = profile.IdentityFraming;
            existing.FutureSelfText = profile.FutureSelfText;
            existing.ChangeStage = profile.ChangeStage;
            _db.UserProfiles.Update(existing);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is not null)
        {
            _db.UserProfiles.Remove(user);
            await _db.SaveChangesAsync(ct);
        }
    }
}