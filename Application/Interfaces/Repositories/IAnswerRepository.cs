using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IAnswerRepository
{
    Task AddRangeAsync(IEnumerable<Answer> answers, CancellationToken ct = default);

    Task<List<Answer>> GetBySessionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    Task<List<Answer>> GetByUserAndQuestionAsync(
        Guid userId,
        string questionId,
        int limitDays = 30,
        CancellationToken ct = default);

    /// <summary>
    /// Returns pre_urge_context tap values (U01) for the given week.
    /// These are the raw quotes fed to Claude on Sunday cron.
    /// </summary>
    Task<List<string>> GetPreUrgeContextsAsync(
        Guid userId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken ct = default);

    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);

}