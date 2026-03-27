using Frank.Domain.Entities;

namespace FrankApi.Application.Interfaces.Repositories;

public interface INotificationLogRepository
{
    /// <summary>
    /// Returns how many notifications were sent to this user today.
    /// Max 2 per day enforced here before sending any notification.
    /// </summary>
    Task<int> GetTodayCountAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a specific notification type was already sent today.
    /// Prevents duplicate morning nudges, evening gentles etc.
    /// </summary>
    Task<bool> WasSentTodayAsync(
        Guid userId,
        string type,
        CancellationToken ct = default);

    Task AddAsync(
        NotificationLog log,
        CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as opened when user taps it.
    /// Used to track notification effectiveness.
    /// </summary>
    Task MarkOpenedAsync(
        Guid logId,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}