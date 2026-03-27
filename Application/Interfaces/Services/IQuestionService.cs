using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IQuestionService
{
    /// <summary>
    /// Selects today's morning question pair.
    /// 
    /// Logic:
    ///   1. Reads question_selection_rules WHERE session_type='morning'
    ///   2. For each required slot queries questions by frame
    ///   3. Excludes questions in question_usage within 7 days for this user
    ///   4. Picks by weight — higher weight selected more often
    ///   5. Records usage in question_usage table
    /// 
    /// Always returns: one frame1 (emotional) + one circumstance question.
    /// Never same pair two days in a row.
    /// </summary>
    Task<QuestionsResponse> GetMorningPairAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<QuestionsResponse> GetOnboardingQuestionsAsync(
        CancellationToken ct = default);

    Task<QuestionsResponse> GetHaltQuestionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Returns urge questions appropriate for this context.
    /// Used when user wants to go deeper past the default 4-interaction ceiling.
    /// </summary>
    Task<QuestionsResponse> GetUrgeQuestionsAsync(
        string addictionType,
        int intensity,
        string? haltRoot,
        CancellationToken ct = default);

    Task<TapOptionsMapResponse> GetAllTapOptionsAsync(CancellationToken ct = default);
}