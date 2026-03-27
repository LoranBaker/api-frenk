using Frank.Application.DTOs.Requests;
using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/urge")]
public class UrgeController : FrankBaseController
{
    private readonly IUrgeService _urge;

    public UrgeController(IUrgeService urge)
    {
        _urge = urge;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(
        [FromBody] StartUrgeRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        try
        {
            var result = await _urge.StartAsync(userId, request, localDate, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(429, new { message = ex.Message });
        }
    }

    [HttpPost("resolve")]
    public async Task<IActionResult> Resolve(
        [FromBody] ResolveUrgeRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        var result = await _urge.ResolveAsync(userId, request, localDate, ct);
        return Ok(result);
    }

    [HttpPost("post-mood")]
    public async Task<IActionResult> SavePostMood(
        [FromBody] PostUrgeMoodRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _urge.SavePostMoodAsync(userId, request, ct);
        return Ok(result);
    }

    [HttpGet("today-count")]
    public async Task<IActionResult> GetTodayCount(CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        var count = await _urge.GetTodayCountAsync(userId, localDate, ct);
        return Ok(new { count });
    }
}