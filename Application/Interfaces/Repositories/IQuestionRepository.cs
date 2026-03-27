using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(string questionId, CancellationToken ct = default);

    /// <summary>
    /// Returns active questions for a session type filtered by frame.
    /// Excludes questions shown to user within cooldown period.
    /// Ordered by weight descending.
    /// </summary>
    Task<List<Question>> GetByFrameAsync(
        string sessionType,
        string frame,
        Guid userId,
        int cooldownDays,
        CancellationToken ct = default);

    Task<List<Question>> GetBySessionTypeAsync(
        string sessionType,
        CancellationToken ct = default);

    Task<List<QuestionSelectionRule>> GetSelectionRulesAsync(
        string sessionType,
        CancellationToken ct = default);

    Task RecordUsageAsync(
        Guid userId,
        string questionId,
        string sessionType,
        CancellationToken ct = default);

    /// <summary>
    /// Returns active frame2 questions matching a specific pattern tag.
    /// Used by InterventionService for tag-based routing.
    /// </summary>
    Task<List<Question>> GetByTagAsync(
        string tag,
        CancellationToken ct = default);
}