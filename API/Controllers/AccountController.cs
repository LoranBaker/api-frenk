using Frank.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrankApi.API.Controllers;

[Authorize]
[Route("api/account")]
public class AccountController : FrankBaseController
{
    private readonly IAccountService _account;

    public AccountController(IAccountService account)
    {
        _account = account;
    }

    /// <summary>
    /// DELETE /api/account
    /// GDPR — deletes all user data and Supabase auth account.
    /// Irreversible. Frontend must show confirmation before calling.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var userId = GetUserId();
        try
        {
            await _account.DeleteAccountAsync(userId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/account/export
    /// Returns all user data as JSON for GDPR export.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _account.ExportDataAsync(userId, ct);
        return Ok(result);
    }
}