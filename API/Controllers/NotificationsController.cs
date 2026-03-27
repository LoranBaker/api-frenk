using Frank.Application.Interfaces.Services;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Route("api/notifications")]
public class NotificationsController : FrankBaseController
{
    private readonly INotificationService _notifications;
    private readonly IDeviceTokenRepository _tokens;
    private readonly IConfiguration _config;

    public NotificationsController(
        INotificationService notifications,
        IDeviceTokenRepository tokens,
        IConfiguration config)
    {
        _notifications = notifications;
        _tokens = tokens;
        _config = config;
    }

    /// <summary>
    /// POST /api/notifications/device-token
    /// Called on app startup after permission granted.
    /// Stores push token + timezone offset.
    /// </summary>
    [Authorize]
    [HttpPost("device-token")]
    public async Task<IActionResult> SaveToken(
        [FromBody] DeviceTokenRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        await _tokens.SaveTokenAsync(userId, request.Token, request.TimezoneOffset, ct);
        return Ok();
    }

    /// <summary>
    /// POST /api/notifications/send-scheduled
    /// Called every hour by external cron (cron-job.org).
    /// Protected by secret key in header.
    /// Sends morning, evening, weekly notifications.
    /// </summary>
    [HttpPost("send-scheduled")]
    public async Task<IActionResult> SendScheduled(
     [FromHeader(Name = "X-Cron-Secret")] string secret,
     CancellationToken ct)
    {
        var expected = _config["Notifications:CronSecret"];
        if (secret != expected)
            return Unauthorized();

        var now = DateTime.UtcNow;
        var currentHour = now.Hour;
        var currentDayOfWeek = (int)now.DayOfWeek;

        // Morning nudge — 8am local
        var morningUsers = await _tokens.GetUsersForMorningAsync(currentHour, ct);
        foreach (var userId in morningUsers)
            await _notifications.SendMorningNudgeAsync(userId, ct);

        // Evening nudge — 8pm local
        var eveningUsers = await _tokens.GetUsersForEveningAsync(currentHour, ct);
        foreach (var userId in eveningUsers)
            await _notifications.SendEveningNudgeAsync(userId, ct);

        // Weekly mirror — Sunday 8pm local
        var weeklyUsers = await _tokens.GetUsersForWeeklyAsync(currentHour, currentDayOfWeek, ct);
        foreach (var userId in weeklyUsers)
            await _notifications.SendWeeklyMirrorNudgeAsync(userId, ct);

        // Mini mirror — check daily, send when conditions met
        if (currentHour == 9) // Run once per day at 9am UTC
        {
            var miniMirrorUsers = await _tokens.GetUsersForMiniMirrorAsync(ct);
            foreach (var userId in miniMirrorUsers)
                await _notifications.SendMiniMirrorNudgeAsync(userId, ct);
        }

        return Ok(new
        {
            morning = morningUsers.Count,
            evening = eveningUsers.Count,
            weekly = weeklyUsers.Count,
            miniMirror = currentHour == 9
                ? (await _tokens.GetUsersForMiniMirrorAsync(ct)).Count
                : 0
        });
    }
}

public record DeviceTokenRequest(string Token, int TimezoneOffset);