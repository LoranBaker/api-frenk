using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/history")]
public class HistoryController : FrankBaseController
{
    private readonly IHistoryService _history;

    public HistoryController(IHistoryService history)
    {
        _history = history;
    }

    /// <summary>
    /// GET /api/history/weeks
    /// Returns all weeks with stats.
    /// Week numbers relative to signup date — Week 1, Week 2 etc.
    /// Zero Claude cost — pure DB reads.
    /// </summary>
    [HttpGet("weeks")]
    public async Task<IActionResult> GetWeeks(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _history.GetWeeksAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/history/day/2026-03-15
    /// Returns full chat thread for a specific day.
    /// Ordered by timestamp. Crisis rows excluded.
    /// </summary>
    [HttpGet("day/{date}")]
    public async Task<IActionResult> GetDay(
        [FromRoute] DateOnly date,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _history.GetDayAsync(userId, date, ct);
        return Ok(result);
    }
}