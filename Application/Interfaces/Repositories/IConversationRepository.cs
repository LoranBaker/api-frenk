using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IConversationRepository
{
    Task AddAsync(ConversationHistory message, CancellationToken ct = default);

    Task AddRangeAsync(
        IEnumerable<ConversationHistory> messages,
        CancellationToken ct = default);

    Task<List<ConversationHistory>> GetByDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);

    Task<List<ConversationHistory>> GetAllByUserAsync(
    Guid userId, CancellationToken ct = default);

}