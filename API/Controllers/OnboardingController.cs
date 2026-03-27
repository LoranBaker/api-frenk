using Frank.Application.DTOs.Requests;
using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/onboarding")]
public class OnboardingController : FrankBaseController
{
    private readonly IOnboardingService _onboarding;

    public OnboardingController(IOnboardingService onboarding)
    {
        _onboarding = onboarding;
    }

    /// <summary>
    /// POST /api/onboarding
    /// Saves all onboarding answers. Idempotent.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] OnboardingRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _onboarding.SaveAsync(userId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/onboarding/profile
    /// Returns current user profile.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();

        try
        {
            var result = await _onboarding.GetProfileAsync(userId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Profile not found. Complete onboarding first." });
        }
    }
}