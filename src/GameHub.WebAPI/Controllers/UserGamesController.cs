// src/GameHub.WebAPI/Controllers/UserGamesController.cs
using GameHub.Application.DTOs.UserGames;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserGamesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UserGamesController> _logger;

    public UserGamesController(IUnitOfWork uow, ILogger<UserGamesController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/users/me/games  [Authorize]
    //
    // Zwraca bibliotekę gier zalogowanego użytkownika.
    // "me" jako segment route zamiast {id} — użytkownik widzi tylko swoje gry.
    // Dane gry (tytuł, gatunek, okładka) pobieramy przez eager loading.
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("me/games")]
    public async Task<IActionResult> GetMyLibrary(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // FindWithIncludesAsync ładuje powiązaną encję Game w jednym SELECT z JOIN.
        // Bez tego dla każdego UserGame robilibyśmy osobny SELECT po Game (N+1).
        var entries = await _uow.UserGames.FindWithIncludesAsync(
            ug => ug.UserId == userId,
            ct,
            ug => ug.Game);

        var dtos = entries
            .OrderByDescending(ug => ug.PurchasedAt)
            .Select(ug => new UserGameDto(
                ug.GameId,
                ug.Game.Title,
                ug.Game.Genre,
                ug.Game.CoverImageUrl,
                ug.PricePaid,
                ug.PurchasedAt))
            .ToList();

        _logger.LogInformation(
            "GET /api/users/me/games — userId={UserId}, {Count} gier w bibliotece",
            userId, dtos.Count);

        return Ok(dtos);
    }
}