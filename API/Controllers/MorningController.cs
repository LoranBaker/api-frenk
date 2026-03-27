using Frank.Application.DTOs.Requests;
using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/checkins/morning")]
public class MorningController : FrankBaseController
{
    private readonly IMorningService _morning;

    public MorningController(IMorningService morning)
    {
        _morning = morning;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        var result = await _morning.GetStatusAsync(userId, localDate, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] MorningCheckinRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        try
        {
            var result = await _morning.SubmitAsync(userId, request, localDate, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}