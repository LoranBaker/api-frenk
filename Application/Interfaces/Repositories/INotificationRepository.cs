namespace FrankApi.Application.Interfaces.Repositories;

public interface IDeviceTokenRepository
{
    Task SaveTokenAsync(
        Guid userId,
        string token,
        int timezoneOffset,
        CancellationToken ct = default);

    Task<List<Guid>> GetUsersForMorningAsync(
        int currentUtcHour,
        CancellationToken ct = default);

    Task<List<Guid>> GetUsersForEveningAsync(
        int currentUtcHour,
        CancellationToken ct = default);

    Task<List<Guid>> GetUsersForWeeklyAsync(
        int currentUtcHour,
        int currentUtcDayOfWeek,
        CancellationToken ct = default);

    Task<List<Guid>> GetUsersForMiniMirrorAsync(CancellationToken ct = default);
}