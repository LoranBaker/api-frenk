namespace FrankApi.Application.Interfaces.Repositories;

public interface IAppSettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetAllAsync(CancellationToken ct = default);
}