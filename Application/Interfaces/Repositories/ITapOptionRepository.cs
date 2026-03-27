using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface ITapOptionRepository
{
    Task<List<TapOption>> GetByQuestionIdAsync(
        string questionId,
        CancellationToken ct = default);

    Task<List<TapOption>> GetByQuestionIdsAsync(
        IEnumerable<string> questionIds,
        CancellationToken ct = default);
    Task<List<TapOption>> GetAllActiveAsync(CancellationToken ct = default);
}