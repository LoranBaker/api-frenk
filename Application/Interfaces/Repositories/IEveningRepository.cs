using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IEveningRepository : IRepository<EveningCheckin>
{

    Task<EveningCheckin?> GetTodayAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<bool> ExistsTodayAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<EveningCheckin?> GetByDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<EveningCheckin>> GetAllByUserAsync(Guid userId, CancellationToken ct = default);
}