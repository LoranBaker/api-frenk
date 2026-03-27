using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IMorningService
{
    Task<MorningStatusResponse> GetStatusAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default);

    Task<MorningCheckinResponse> SubmitAsync(
        Guid userId,
        MorningCheckinRequest request,
        DateOnly localDate,
        CancellationToken ct = default);
}