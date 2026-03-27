using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FrankApi.API.Controllers;

/// <summary>
/// Base controller for all Frank API controllers.
/// Provides user_id extraction from JWT claims.
/// Never trust user_id from request body — always from JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class FrankBaseController : ControllerBase
{
    /// <summary>
    /// Extracts user_id from the validated JWT token.
    /// Supabase puts the user UUID in the 'sub' claim.
    /// </summary>
    protected Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        if (sub is null || !Guid.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user_id in token.");

        return userId;
    }

    protected DateOnly GetLocalDate()
    {
        if (Request.Headers.TryGetValue("X-Local-Date", out var val) &&
            DateOnly.TryParse(val, out var date))
            return date;
        // Fallback — should never happen if frontend sends header
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}