using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IWeeklyService
{
    /// <summary>
    /// Returns this week's mirror data.
    /// If cron already ran today → returns cached weekly_summaries row.
    /// If not yet run → triggers generation immediately.
    /// Only available on Sundays — returns 403 on other days.
    /// </summary>
    Task<WeeklyMirrorResponse> GetMirrorAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<SuccessResponse> SaveReflectionAsync(
        Guid userId,
        WeeklyReflectionRequest request,
        CancellationToken ct = default);
}