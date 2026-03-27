namespace Frank.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendAsync(
        string deviceToken,
        string title,
        string body,
        string type,
        CancellationToken ct = default);

    Task SchedulePostUrgeAsync(
        Guid urgeEventId,
        Guid userId,
        CancellationToken ct = default);

    Task SendMorningNudgeAsync(
        Guid userId,
        CancellationToken ct = default);

    Task SendEveningNudgeAsync(
        Guid userId,
        CancellationToken ct = default);

    Task SendWeeklyMirrorNudgeAsync(
        Guid userId,
        CancellationToken ct = default);

    Task SendMiniMirrorNudgeAsync(
        Guid userId,
        CancellationToken ct = default);
}