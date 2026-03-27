using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;
using Frank.Domain.Entities;

namespace Frank.Application.Interfaces.Services;

public interface IUrgeService
{
    /// <summary>
    /// Processes urge button press:
    ///   1. Creates chat_session row
    ///   2. Saves HALT answers to answers table
    ///   3. Builds InterventionContext
    ///   4. Calls IInterventionService.SelectAsync()
    ///   5. Creates urge_events row with intervention_id
    ///   6. Writes messages to conversation_history
    ///   7. Returns intervention to show user
    /// </summary>
    Task<StartUrgeResponse> StartAsync(
         Guid userId,
         StartUrgeRequest request,
         DateOnly localDate,
         CancellationToken ct = default);

    Task<ResolveUrgeResponse> ResolveAsync(
        Guid userId,
        ResolveUrgeRequest request,
        DateOnly localDate,
        CancellationToken ct = default);

    Task<SuccessResponse> SavePostMoodAsync(
        Guid userId,
        PostUrgeMoodRequest request,
        CancellationToken ct = default);

    Task<int> GetTodayCountAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default);

}