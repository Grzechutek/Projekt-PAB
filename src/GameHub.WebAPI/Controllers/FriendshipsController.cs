// src/GameHub.WebAPI/Controllers/FriendshipsController.cs
using GameHub.Application.DTOs.Friendships;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]   // wszystkie endpointy wymagają zalogowania
public class FriendshipsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FriendshipsController> _logger;

    public FriendshipsController(IUnitOfWork uow, ILogger<FriendshipsController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/friends
    //
    // Lista zaakceptowanych znajomych zalogowanego użytkownika.
    // Znajomy może być po stronie Requester LUB Addressee — sprawdzamy oba.
    // Zwracamy dane "drugiej" strony relacji (nie zalogowanego usera).
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var friendships = await _uow.Friendships.FindWithIncludesAsync(
            f => (f.RequesterId == userId || f.AddresseeId == userId)
              && f.Status == FriendshipStatus.Accepted,
            ct,
            f => f.Requester,
            f => f.Addressee);

        // Wyciągamy "drugiego" usera — nie zalogowanego
        var dtos = friendships.Select(f =>
        {
            var other = f.RequesterId == userId ? f.Addressee : f.Requester;
            return new FriendDto(f.Id, other.Id, other.Username, other.AvatarUrl, f.CreatedAt);
        }).ToList();

        _logger.LogInformation(
            "GET /api/friends — userId={UserId}, {Count} znajomych", userId, dtos.Count);

        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/friends/requests
    //
    // Oczekujące zaproszenia PRZYCHODZĄCE — zalogowany user jest Addressee.
    // Frontend może na tej podstawie wyświetlić przyciski "Akceptuj / Odrzuć".
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var requests = await _uow.Friendships.FindWithIncludesAsync(
            f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending,
            ct,
            f => f.Requester);

        var dtos = requests
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FriendRequestDto(
                f.Id,
                f.RequesterId,
                f.Requester.Username,
                f.Requester.AvatarUrl,
                f.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GET /api/friends/requests — userId={UserId}, {Count} oczekujących",
            userId, dtos.Count);

        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST /api/friends/invite/{targetUserId}
    //
    // Wysyła zaproszenie do znajomych.
    // Sprawdzamy OBA kierunki (A→B i B→A), żeby wykryć:
    //   - duplikat własnego zaproszenia
    //   - zaproszenie od drugiej strony (sugerujemy akceptację zamiast nowego)
    //   - istniejącą relację Accepted
    // ──────────────────────────────────────────────────────────────────────
    [HttpPost("invite/{targetUserId:int}")]
    public async Task<IActionResult> Invite(int targetUserId, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Nie możesz zaprosić samego siebie
        if (userId == targetUserId)
            return BadRequest(new { error = "Nie możesz zaprosić samego siebie." });

        // Sprawdź czy docelowy user istnieje
        var target = await _uow.Users.GetByIdAsync(targetUserId, ct);
        if (target is null || target.IsBlocked)
            return NotFound(new { error = "Użytkownik nie istnieje." });

        // Sprawdź czy jakakolwiek relacja między tymi userami już istnieje
        var existing = await _uow.Friendships.FirstOrDefaultAsync(
            f => (f.RequesterId == userId    && f.AddresseeId == targetUserId) ||
                 (f.RequesterId == targetUserId && f.AddresseeId == userId),
            ct);

        if (existing is not null)
        {
            return existing.Status switch
            {
                FriendshipStatus.Accepted => Conflict(new { error = "Jesteście już znajomymi." }),

                // Ja już wysłałem zaproszenie — czekam na odpowiedź
                FriendshipStatus.Pending when existing.RequesterId == userId
                    => Conflict(new { error = "Zaproszenie zostało już wysłane." }),

                // Druga strona mnie zaprosiła — powiedz userowi żeby zaakceptował
                FriendshipStatus.Pending
                    => Conflict(new { error = "Ta osoba już wysłała Ci zaproszenie — zaakceptuj je z sekcji \"Zaproszenia\"." }),

                _ => Conflict(new { error = "Nie można wysłać zaproszenia." })
            };
        }

        var friendship = new Friendship
        {
            RequesterId = userId,
            AddresseeId = targetUserId,
            Status      = FriendshipStatus.Pending,
            CreatedAt   = DateTime.UtcNow
        };

        await _uow.Friendships.AddAsync(friendship, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} wysłał zaproszenie do userId={TargetId}", userId, targetUserId);

        return Ok(new { message = $"Zaproszenie do {target.Username} zostało wysłane." });
    }

    // ──────────────────────────────────────────────────────────────────────
    // PUT /api/friends/{id}/accept
    //
    // Akceptuje zaproszenie. Tylko Addressee może zaakceptować —
    // Requester nie może sam siebie "zatwierdzić".
    // ──────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var friendship = await _uow.Friendships.GetByIdAsync(id, ct);
        if (friendship is null)
            return NotFound(new { error = "Zaproszenie nie istnieje." });

        // Tylko Addressee może akceptować
        if (friendship.AddresseeId != userId)
        {
            _logger.LogWarning(
                "User {UserId} próbował zaakceptować cudze zaproszenie Id={FriendshipId}",
                userId, id);
            return Forbid();
        }

        if (friendship.Status != FriendshipStatus.Pending)
            return BadRequest(new { error = "To zaproszenie nie jest już w stanie oczekiwania." });

        friendship.Status = FriendshipStatus.Accepted;
        _uow.Friendships.Update(friendship);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} zaakceptował zaproszenie Id={FriendshipId}", userId, id);

        return Ok(new { message = "Zaproszenie zaakceptowane." });
    }

    // ──────────────────────────────────────────────────────────────────────
    // DELETE /api/friends/{id}
    //
    // Jeden endpoint obsługuje trzy przypadki:
    //   - Odrzucenie zaproszenia  (Addressee usuwa Pending)
    //   - Anulowanie zaproszenia  (Requester usuwa Pending)
    //   - Usunięcie znajomego     (każda ze stron usuwa Accepted)
    //
    // Musisz być jedną ze stron relacji — nie możesz usunąć cudzej relacji.
    // ──────────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var friendship = await _uow.Friendships.GetByIdAsync(id, ct);
        if (friendship is null)
            return NotFound(new { error = "Relacja nie istnieje." });

        // Musisz być Requesterem lub Addressee
        if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
        {
            _logger.LogWarning(
                "User {UserId} próbował usunąć relację Id={FriendshipId}, do której nie należy",
                userId, id);
            return Forbid();
        }

        var action = friendship.Status == FriendshipStatus.Accepted
            ? "usunął znajomego"
            : friendship.RequesterId == userId ? "anulował zaproszenie" : "odrzucił zaproszenie";

        _uow.Friendships.Remove(friendship);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} {Action} (FriendshipId={FriendshipId})", userId, action, id);

        return NoContent();
    }
}