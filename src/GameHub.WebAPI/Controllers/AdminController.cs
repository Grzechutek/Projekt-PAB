// src/GameHub.WebAPI/Controllers/AdminController.cs
using GameHub.Application.DTOs.Admin;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdminController> _logger;

    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "User", "Admin" };

    public AdminController(IUnitOfWork uow, ILogger<AdminController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // ZARZĄDZANIE UŻYTKOWNIKAMI
    // ──────────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _uow.Users.GetAllAsync(ct);
        var dtos = users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto(
                u.Id, u.Username, u.Email,
                u.Role, u.IsBlocked, u.WalletBalance, u.CreatedAt))
            .ToList();

        return Ok(dtos);
    }

    [HttpPatch("users/{id:int}/block")]
    public async Task<IActionResult> ToggleBlock(int id, CancellationToken ct)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == adminId)
            return BadRequest(new { error = "Nie możesz zablokować własnego konta." });

        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        user.IsBlocked = !user.IsBlocked;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { id, isBlocked = user.IsBlocked });
    }

    // ──────────────────────────────────────────────────────────────────────
    // ZARZĄDZANIE KATALOGIEM GIER (NOWE)
    // ──────────────────────────────────────────────────────────────────────

    [HttpGet("games")]
    public async Task<IActionResult> GetAllGames(CancellationToken ct)
    {
        // Pobieramy wszystkie gry bez filtrów (UnitOfWork powinien mieć dostęp do bazy)
        var games = await _uow.Games.GetAllAsync(ct);
        
        var dtos = games.Select(g => new {
            g.Id,
            g.Title,
            g.Genre,
            g.Price,
            g.CoverImageUrl,
            g.Description,
            g.IsVisible // Flaga widoczna tylko dla Admina
        }).ToList();

        return Ok(dtos);
    }
    [HttpPatch("games/{id:int}/hide")]
    public async Task<IActionResult> HideGame(int id, CancellationToken ct)
    {
        var game = await _uow.Games.GetByIdAsync(id, ct);
        if (game is null) return NotFound();

        game.IsVisible = false; // Ukrywamy grę
        _uow.Games.Update(game);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Admin ukrył grę Id={Id}", id);
        return Ok();
    }

    [HttpPatch("games/{id:int}/restore")]
    public async Task<IActionResult> RestoreGame(int id, CancellationToken ct)
    {
        var game = await _uow.Games.GetByIdAsync(id, ct);
        if (game is null) return NotFound();

        // Jeśli gra JEST widoczna, to wyrzucamy błąd (nie można przywrócić czegoś, co już działa)
        if (game.IsVisible)
            return BadRequest(new { error = "Gra jest już widoczna w sklepie." });

        // PRZYWRACAMY GRĘ -> Ustawiamy flagę na true!
        game.IsVisible = true;
        
        _uow.Games.Update(game);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Admin przywrócił grę Id={Id}: {Title}", id, game.Title);
        return Ok(new { id, isVisible = true });
    }
}