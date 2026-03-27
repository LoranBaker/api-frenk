using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IPreUrgeVersionRepository
{
    /// <summary>
    /// Returns the latest version of pre_urge_description for this user.
    /// Claude always uses this version — never the original if user has updated it.
    /// </summary>
    Task<PreUrgeVersion?> GetLatestAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<List<PreUrgeVersion>> GetAllAsync(
        Guid userId,
        CancellationToken ct = default);

    Task AddAsync(
        PreUrgeVersion version,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the next version number for this user.
    /// 1 = onboarding original. Increments on each edit.
    /// </summary>
    Task<int> GetNextVersionNumAsync(
        Guid userId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}