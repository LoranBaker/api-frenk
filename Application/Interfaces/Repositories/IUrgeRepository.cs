using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IUrgeRepository : IRepository<UrgeEvent>
{
    Task<UrgeEvent?> GetByIdAsync(Guid urgeEventId, CancellationToken ct = default);
    Task<int> GetTodayCountAsync(Guid userId, CancellationToken ct = default);

    Task<List<UrgeEvent>> GetThisWeekAsync(
        Guid userId,
        DateOnly weekStart,
        CancellationToken ct = default);

    Task<bool> SlippedTodayAsync(Guid userId, CancellationToken ct = default);

    Task UpdateResistAsync(
        Guid urgeEventId,
        bool resisted,
        CancellationToken ct = default);

    Task UpdatePostMoodAsync(
        Guid urgeEventId,
        string mood,
        CancellationToken ct = default);

    Task<int> GetDaysSinceLastSlipAsync(Guid userId, CancellationToken ct = default);

    Task<List<UrgeEvent>> GetAllWithContextAsync(
    Guid userId,
    CancellationToken ct = default);

    // IUrgeRepository
    Task<List<UrgeEvent>> GetAllTimeAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTodayCountAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<bool> SlippedTodayAsync(Guid userId, DateOnly date, CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);

}