using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IMorningRepository : IRepository<MorningCheckin>
{
    Task<MorningCheckin?> GetTodayAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<bool> ExistsTodayAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<MorningCheckin?> GetYesterdayAsync(Guid userId, CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<MorningCheckin>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
}