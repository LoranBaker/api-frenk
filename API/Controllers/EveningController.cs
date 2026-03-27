using Frank.Application.DTOs.Requests;
using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/checkins/evening")]
public class EveningController : FrankBaseController
{
    private readonly IEveningService _evening;

    public EveningController(IEveningService evening)
    {
        _evening = evening;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        var result = await _evening.GetStatusAsync(userId, localDate, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] EveningCheckinRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var localDate = GetLocalDate();
        try
        {
            var result = await _evening.SubmitAsync(userId, request, localDate, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}