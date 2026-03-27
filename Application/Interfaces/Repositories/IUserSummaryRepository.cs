using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface IUserSummaryRepository
{
    Task<UserSummary?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserSummary summary, CancellationToken ct = default);
    Task IncrementDailyUrgeCountAsync(Guid userId, CancellationToken ct = default);
    Task ResetDailyUrgeCountAsync(Guid userId, CancellationToken ct = default);
}