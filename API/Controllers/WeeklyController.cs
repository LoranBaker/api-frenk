using Frank.Application.DTOs.Requests;
using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/weekly")]
public class WeeklyController : FrankBaseController
{
    private readonly IWeeklyService _weekly;

    public WeeklyController(IWeeklyService weekly)
    {
        _weekly = weekly;
    }

    /// <summary>
    /// GET /api/weekly/mirror
    /// Returns this week's mirror — observations + quotes + reflection question.
    /// Returns cached if already generated. Generates fresh if not.
    /// </summary>
    [HttpGet("mirror")]
    public async Task<IActionResult> GetMirror(CancellationToken ct)
    {
        var userId = GetUserId();

        try
        {
            var result = await _weekly.GetMirrorAsync(userId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/weekly/reflection
    /// Saves weekly reflection answer.
    /// </summary>
    [HttpPost("reflection")]
    public async Task<IActionResult> SaveReflection(
        [FromBody] WeeklyReflectionRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();

        try
        {
            var result = await _weekly.SaveReflectionAsync(userId, request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}