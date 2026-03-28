using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserProfile profile, CancellationToken ct = default);

    Task DeleteAsync(Guid userId, CancellationToken ct = default);

}