using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IMiniMirrorRepository
{
    /// <summary>
    /// Checks if mini mirror was already triggered for this user.
    /// Mini mirror fires once in the day 3-7 window — never again.
    /// </summary>
    Task<bool> WasTriggeredAsync(
        Guid userId,
        CancellationToken ct = default);

    Task AddAsync(
        MiniMirrorEvent evt,
        CancellationToken ct = default);

    Task<MiniMirrorEvent?> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default);

    Task MarkOpenedAsync(
        Guid eventId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}