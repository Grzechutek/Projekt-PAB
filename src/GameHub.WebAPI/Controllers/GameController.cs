// src/GameHub.WebAPI/Controllers/GamesController.cs
using GameHub.Application.DTOs.Games;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    // IUnitOfWork  → CRUD gier i recenzji (DEV B)
    // IWalletService → zakup gry (DEV A, BuyGameAsync)
    private readonly IUnitOfWork     _uow;
    private readonly IWalletService  _wallet;
    private readonly ILogger<GamesController> _logger;

    public GamesController(
        IUnitOfWork uow,
        IWalletService wallet,
        ILogger<GamesController> logger)
    {
        _uow    = uow;
        _wallet = wallet;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/games?genre=RPG&sort=price_asc&search=cyber
    // Publiczny. Zwraca wyłącznie gry z IsVisible=true.
    // sort: "price_asc" | "price_desc" | "newest" | "title" (domyślne)
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? genre,
        [FromQuery] string? sort,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var games = await _uow.Games.FindAsync(
            g => g.IsVisible
              && (genre  == null || g.Genre == genre)
              && (search == null || g.Title.Contains(search)
                                 || (g.Description != null && g.Description.Contains(search))),
            ct);

        IEnumerable<Game> ordered = sort switch
        {
            "price_asc"  => games.OrderBy(g => g.Price),
            "price_desc" => games.OrderByDescending(g => g.Price),
            "newest"     => games.OrderByDescending(g => g.CreatedAt),
            _            => games.OrderBy(g => g.Title)
        };

        var dtos = ordered.Select(g => new GameDto(
            g.Id, g.Title, g.Description, g.Genre,
            g.Price, g.CoverImageUrl, g.IsVisible,
            g.CreatedAt, AverageRating: null)).ToList();

        _logger.LogInformation("GET /api/games — zwrócono {Count} wyników", dtos.Count);
        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/games/{id}
    // Publiczny. Zwraca grę + średnią ocen (tylko nie-ukryte recenzje).
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var game = await _uow.Games.GetByIdAsync(id, ct);

        if (game is null || !game.IsVisible)
        {
            _logger.LogWarning("GET /api/games/{Id} — nie znaleziono gry", id);
            return NotFound(new { error = $"Gra o Id={id} nie istnieje lub jest ukryta." });
        }

        var reviews = await _uow.Reviews.FindAsync(
            r => r.GameId == id && !r.IsHidden, ct);

        double? avg = reviews.Count > 0
            ? Math.Round(reviews.Average(r => r.Rating), 2)
            : null;

        var dto = new GameDto(game.Id, game.Title, game.Description, game.Genre,
            game.Price, game.CoverImageUrl, game.IsVisible, game.CreatedAt, avg);

        _logger.LogInformation(
            "GET /api/games/{Id} — '{Title}', średnia ocen: {Avg}",
            id, game.Title, avg?.ToString("F2") ?? "brak");

        return Ok(dto);
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST /api/games  [Admin]
    // Tworzy nową grę. Zwraca 201 Created.
    // ──────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateGameRequest req, CancellationToken ct)
    {
        var game = new Game
        {
            Title         = req.Title,
            Description   = req.Description,
            Genre         = req.Genre,
            Price         = req.Price,
            CoverImageUrl = req.CoverImageUrl,
            IsVisible     = true,
            CreatedAt     = DateTime.UtcNow
        };

        await _uow.Games.AddAsync(game, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Admin dodał grę '{Title}' (Id={Id})", game.Title, game.Id);

        var dto = new GameDto(game.Id, game.Title, game.Description, game.Genre,
            game.Price, game.CoverImageUrl, game.IsVisible, game.CreatedAt, null);

        return CreatedAtAction(nameof(GetById), new { id = game.Id }, dto);
    }

    // ──────────────────────────────────────────────────────────────────────
    // PUT /api/games/{id}  [Admin]
    // Aktualizuje dane gry. Pola null w body są pomijane.
    // Zwraca 204 No Content.
    // ──────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateGameRequest req, CancellationToken ct)
    {
        var game = await _uow.Games.GetByIdAsync(id, ct);
        if (game is null)
            return NotFound(new { error = $"Gra o Id={id} nie istnieje." });

        if (req.Title         is not null) game.Title         = req.Title;
        if (req.Description   is not null) game.Description   = req.Description;
        if (req.Genre         is not null) game.Genre         = req.Genre;
        if (req.Price         is not null) game.Price         = req.Price.Value;
        if (req.CoverImageUrl is not null) game.CoverImageUrl = req.CoverImageUrl;

        _uow.Games.Update(game);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Admin zaktualizował grę Id={Id}", id);
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────
    // DELETE /api/games/{id}  [Admin]
    // Soft-delete: IsVisible = false. Zachowuje recenzje i historię zakupów.
    // Idempotentny — zwraca 204 nawet gdy gra jest już ukryta.
    // ──────────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var game = await _uow.Games.GetByIdAsync(id, ct);
        if (game is null)
            return NotFound(new { error = $"Gra o Id={id} nie istnieje." });

        game.IsVisible = false;
        _uow.Games.Update(game);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin ukrył grę '{Title}' Id={Id} (soft-delete)", game.Title, id);

        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST /api/games/{id}/buy  [Authorize]
    // Zakup gry przez zalogowanego użytkownika.
    // Logika biznesowa (walidacja salda, atomowa transakcja) w WalletService.
    // Oryginalnie napisane przez DEV A — zachowane bez zmian.
    // ──────────────────────────────────────────────────────────────────────
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