// src/GameHub.WebAPI/Controllers/GamesController.cs  (fragment — endpoint /buy)
using GameHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IWalletService _wallet;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IWalletService wallet, ILogger<GamesController> logger)
    {
        _wallet = wallet;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("{id:int}/buy")]
    public async Task<IActionResult> Buy(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _wallet.BuyGameAsync(userId, id, ct);
            _logger.LogInformation(
                "User {UserId} purchased game {GameId}", userId, id);
            return Ok(new { message = "Game purchased successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Purchase failed for user {UserId}, game {GameId}", userId, id);
            return StatusCode(500, new { error = "Purchase failed." });
        }
    }
}