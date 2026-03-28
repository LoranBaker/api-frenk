using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/mini-mirror")]
public class MiniMirrorController : FrankBaseController
{
    private readonly IMiniMirrorService _miniMirror;

    public MiniMirrorController(IMiniMirrorService miniMirror)
    {
        _miniMirror = miniMirror;
    }

    /// <summary>
    /// GET /api/mini-mirror
    /// Returns mini mirror if user qualifies (day 3-7 + 3+ urges with context).
    /// Returns 204 No Content if not ready yet.
    /// Frontend calls on app open from day 3 onwards.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _miniMirror.TryGenerateAsync(userId, ct);

        if (result is null)
            return NoContent();

        return Ok(result);
    }

    /// <summary>
    /// GET /api/mini-mirror/shown
    /// Returns the mini mirror that was already shown to this user.
    /// Used to restore it after navigation.
    /// </summary>
    [HttpGet("shown")]
    public async Task<IActionResult> GetShown(CancellationToken ct)
    {
        var userId = GetUserId();
        var evt = await _miniMirror.GetShownAsync(userId, ct);
        if (evt is null) return NoContent();

        var quotes = System.Text.Json.JsonSerializer
            .Deserialize<string[]>(evt.QuotesShown) ?? Array.Empty<string>();

        return Ok(new
        {
            quotes = quotes,
            observation = evt.Observation,
            urgeCount = evt.UrgeCount,
            triggeredAt = evt.TriggeredAt.ToString("yyyy-MM-dd")  // ← date string only, no time
        });
    }
}