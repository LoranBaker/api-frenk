using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<ChatSession?> GetTodayAsync(
        Guid userId,
        string sessionType,
        CancellationToken ct = default);

    Task<ChatSession> CreateAsync(
        Guid userId,
        string sessionType,
        DateOnly date,
        CancellationToken ct = default);

    Task CompleteAsync(Guid sessionId, CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);

}