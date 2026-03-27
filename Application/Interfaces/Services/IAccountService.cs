using Frank.Application.DTOs.Responses;

namespace Frank.Application.Interfaces.Services;

public interface IAccountService
{
    Task DeleteAccountAsync(Guid userId, CancellationToken ct = default);
    Task<AccountExportDto> ExportDataAsync(Guid userId, CancellationToken ct = default);
}