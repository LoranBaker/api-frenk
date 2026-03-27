using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IWeeklyRepository : IRepository<WeeklySummary>
{
    Task<WeeklySummary?> GetByWeekStartAsync(
        Guid userId,
        DateOnly weekStart,
        CancellationToken ct = default);

    Task<WeeklySummary?> GetPreviousWeekAsync(
        Guid userId,
        DateOnly currentWeekStart,
        CancellationToken ct = default);

    Task<List<WeeklySummary>> GetAllForUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<WeeklySummary>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
}