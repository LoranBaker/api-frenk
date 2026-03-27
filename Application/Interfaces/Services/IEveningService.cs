using Frank.Application.DTOs.Requests;
using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IEveningService
{
    Task<EveningStatusResponse> GetStatusAsync(
        Guid userId,
        DateOnly localDate,
        CancellationToken ct = default);

    Task<EveningCheckinResponse> SubmitAsync(
        Guid userId,
        EveningCheckinRequest request,
        DateOnly localDate,
        CancellationToken ct = default);
}