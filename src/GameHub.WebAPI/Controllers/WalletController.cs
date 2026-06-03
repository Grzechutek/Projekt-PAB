// src/GameHub.WebAPI/Controllers/WalletController.cs
using GameHub.Application.DTOs.Wallet;
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _wallet;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService wallet, ILogger<WalletController> logger)
    {
        _wallet = wallet;
        _logger = logger;
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp(
        [FromBody] TopUpRequest req, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            var balance = await _wallet.TopUpAsync(userId, req.Amount, ct);
            _logger.LogInformation(
                "TopUp {Amount} PLN for userId {UserId}", req.Amount, userId);
            return Ok(new { balance });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TopUp failed for userId {UserId}", userId);
            return StatusCode(500, new { error = "TopUp failed." });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _wallet.GetHistoryAsync(userId, ct));
    }
}