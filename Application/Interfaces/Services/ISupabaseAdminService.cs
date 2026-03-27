namespace Frank.Application.Interfaces.Services;

public interface ISupabaseAdminService
{
    Task DeleteUserAsync(Guid userId, CancellationToken ct = default);
}