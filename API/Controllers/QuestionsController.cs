using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/questions")]
public class QuestionsController : FrankBaseController
{
    private readonly IQuestionService _questions;

    public QuestionsController(IQuestionService questions)
    {
        _questions = questions;
    }

    /// <summary>
    /// GET /api/questions/morning
    /// Returns today's morning question pair.
    /// One frame1 (emotional) + one circumstance.
    /// Respects 7-day cooldown per user.
    /// </summary>
    [HttpGet("morning")]
    public async Task<IActionResult> GetMorningPair(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _questions.GetMorningPairAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/questions/onboarding
    /// Returns onboarding questions with tap options.
    /// No auth required — called before login exists.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboarding(CancellationToken ct)
    {
        var result = await _questions.GetOnboardingQuestionsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/questions/halt
    /// Returns HALT questions H01-H05.
    /// Static — same for every user.
    /// </summary>
    [HttpGet("halt")]
    public async Task<IActionResult> GetHalt(CancellationToken ct)
    {
        var result = await _questions.GetHaltQuestionsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/questions/urge?addictionType=gaming&intensity=7&haltRoot=tired
    /// Returns scenario-appropriate urge questions.
    /// Used for optional deeper flow past 4-interaction ceiling.
    /// </summary>
    [HttpGet("urge")]
    public async Task<IActionResult> GetUrgeQuestions(
        [FromQuery] string addictionType,
        [FromQuery] int intensity,
        [FromQuery] string? haltRoot,
        CancellationToken ct)
    {
        var result = await _questions.GetUrgeQuestionsAsync(
            addictionType, intensity, haltRoot, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/questions/tap-options
    /// Returns all active tap options (value → text map).
    /// Used by frontend to resolve raw DB values to display text.
    /// Public — no auth required.
    /// </summary>
    [HttpGet("tap-options")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllTapOptions(CancellationToken ct)
    {
        var result = await _questions.GetAllTapOptionsAsync(ct);
        return Ok(result);
    }

}