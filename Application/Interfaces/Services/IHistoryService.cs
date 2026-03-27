using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IHistoryService
{
    /// <summary>
    /// Returns all weeks with stats for the history week view.
    /// Pure SQL aggregation — zero Claude cost.
    /// </summary>
    Task<HistoryWeeksResponse> GetWeeksAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns full chat thread for a specific day.
    /// Reads conversation_history ordered by timestamp.
    /// Read-only — never modified.
    /// </summary>
    Task<DayChatResponse> GetDayAsync(
        Guid userId,
        DateOnly date,
        CancellationToken ct = default);
}