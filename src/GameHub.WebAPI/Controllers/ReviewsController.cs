// src/GameHub.WebAPI/Controllers/ReviewsController.cs
using GameHub.Application.DTOs.Reviews;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

// UWAGA: Brak [Route] na klasie — routes są podzielone na dwa prefiksy:
//   /api/games/{gameId}/reviews   (lista i tworzenie)
//   /api/reviews/{id}             (edycja, usuwanie, moderacja)
// Każda metoda podaje pełną ścieżkę.
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IUnitOfWork uow, ILogger<ReviewsController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/games/{gameId}/reviews
    //
    // Publiczny — nie wymaga tokena.
    // Zwykły user i anonim: tylko IsHidden=false.
    // Admin (token z rolą Admin): widzi wszystkie, łącznie z ukrytymi.
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("api/games/{gameId:int}/reviews")]
    public async Task<IActionResult> GetByGame(int gameId, CancellationToken ct)
    {
        // Weryfikujemy, czy gra istnieje i jest widoczna.
        var game = await _uow.Games.GetByIdAsync(gameId, ct);
        if (game is null || !game.IsVisible)
            return NotFound(new { error = $"Gra o Id={gameId} nie istnieje." });

        // Admin widzi ukryte recenzje (np. na potrzeby panelu moderacji).
        var isAdmin = User.Identity?.IsAuthenticated == true
                   && User.IsInRole("Admin");

        // FindWithIncludesAsync ładuje powiązanego User w jednym zapytaniu SQL.
        // Bez tego musielibyśmy robić osobny SELECT dla każdej recenzji (N+1).
        var reviews = await _uow.Reviews.FindWithIncludesAsync(
            r => r.GameId == gameId && (isAdmin || !r.IsHidden),
            ct,
            r => r.User);

        var dtos = reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id, r.UserId, r.User.Username,
                r.GameId, r.Rating, r.Content,
                r.IsHidden, r.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GET /api/games/{GameId}/reviews — zwrócono {Count} recenzji",
            gameId, dtos.Count);

        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST /api/games/{gameId}/reviews  [Authorize]
    //
    // Tworzy recenzję. Zasady:
    //   - max 1 recenzja na grę per użytkownik (sprawdzamy przed insertem)
    //   - gra musi istnieć i być widoczna
    // Zwraca 201 Created z body nowej recenzji.
    // ──────────────────────────────────────────────────────────────────────
    [Authorize]
    [HttpPost("api/games/{gameId:int}/reviews")]
    public async Task<IActionResult> Create(
        int gameId,
        [FromBody] CreateReviewRequest req,
        CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1. Gra musi istnieć i być widoczna.
        var game = await _uow.Games.GetByIdAsync(gameId, ct);
        if (game is null || !game.IsVisible)
            return NotFound(new { error = $"Gra o Id={gameId} nie istnieje." });

        // 2. Użytkownik nie może mieć dwóch recenzji tej samej gry.
        //    Constraint UNIQUE(UserId, GameId) jest też w bazie, ale sprawdzamy
        //    wcześniej, żeby zwrócić czytelny 409 zamiast wyjątku SQL.
        if (await _uow.Reviews.ExistsAsync(
                r => r.UserId == userId && r.GameId == gameId, ct))
        {
            return Conflict(new { error = "Już napisałeś recenzję dla tej gry." });
        }

        var review = new Review
        {
            UserId    = userId,
            GameId    = gameId,
            Rating    = req.Rating,
            Content   = req.Content,
            IsHidden  = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Reviews.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);

        // Pobieramy username do DTO (review.User nie jest załadowany po AddAsync).
        var user = await _uow.Users.GetByIdAsync(userId, ct);

        _logger.LogInformation(
            "User {UserId} dodał recenzję do gry {GameId} (Rating={Rating})",
            userId, gameId, req.Rating);

        var dto = new ReviewDto(
            review.Id, userId, user!.Username,
            gameId, review.Rating, review.Content,
            review.IsHidden, review.CreatedAt);

        return CreatedAtAction(
            nameof(GetByGame), new { gameId }, dto);
    }

    // ──────────────────────────────────────────────────────────────────────
    // PUT /api/reviews/{id}  [Authorize]
    //
    // Edytuje recenzję. Tylko właściciel może edytować własną recenzję.
    // Pola null w body są pomijane (partial update).
    // Zwraca 204 No Content.
    // ──────────────────────────────────────────────────────────────────────
    [Authorize]
    [HttpPut("api/reviews/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateReviewRequest req,
        CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var review = await _uow.Reviews.GetByIdAsync(id, ct);
        if (review is null)
            return NotFound(new { error = $"Recenzja o Id={id} nie istnieje." });

        // Tylko właściciel może edytować.
        if (review.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} próbował edytować cudzą recenzję Id={ReviewId}",
                userId, id);
            return Forbid();
        }

        if (req.Rating  is not null) review.Rating  = req.Rating.Value;
        if (req.Content is not null) review.Content = req.Content;

        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} zaktualizował recenzję Id={ReviewId}", userId, id);
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────
    // DELETE /api/reviews/{id}  [Authorize]
    //
    // Usuwa recenzję. Właściciel może usunąć własną; Admin może usunąć każdą.
    // Zwraca 204 No Content.
    // ──────────────────────────────────────────────────────────────────────
    [Authorize]
    [HttpDelete("api/reviews/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var review = await _uow.Reviews.GetByIdAsync(id, ct);
        if (review is null)
            return NotFound(new { error = $"Recenzja o Id={id} nie istnieje." });

        // Właściciel LUB Admin.
        if (review.UserId != userId && userRole != "Admin")
        {
            _logger.LogWarning(
                "User {UserId} próbował usunąć cudzą recenzję Id={ReviewId}",
                userId, id);
            return Forbid();
        }

        _uow.Reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Recenzja Id={ReviewId} usunięta przez userId={UserId} (rola: {Role})",
            id, userId, userRole);

        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────
    // PATCH /api/reviews/{id}/hide  [Admin]
    //
    // Przełącza IsHidden (toggle). Moderacja: ukrywa lub przywraca recenzję
    // bez trwałego usuwania. Zwraca aktualny stan IsHidden.
    // ──────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [HttpPatch("api/reviews/{id:int}/hide")]
    public async Task<IActionResult> ToggleHide(int id, CancellationToken ct)
    {
        var review = await _uow.Reviews.GetByIdAsync(id, ct);
        if (review is null)
            return NotFound(new { error = $"Recenzja o Id={id} nie istnieje." });

        review.IsHidden = !review.IsHidden;
        _uow.Reviews.Update(review);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin zmienił widoczność recenzji Id={ReviewId}: IsHidden={IsHidden}",
            id, review.IsHidden);

        return Ok(new { id, isHidden = review.IsHidden });
    }
}