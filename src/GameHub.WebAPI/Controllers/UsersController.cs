using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public UsersController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<UserSearchDto>());

        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var users = await _uow.Users.FindAsync(
            u => u.Id != currentUserId && u.Username.Contains(query), 
            ct);

        var result = users
            .Take(10)
            .Select(u => new UserSearchDto(u.Id, u.Username, u.AvatarUrl))
            .ToList();

        return Ok(result);
    }

    // NOWY ENDPOINT: Pobieranie biblioteki znajomego
    [HttpGet("/api/users/{id:int}/library")]
    public async Task<IActionResult> GetFriendLibrary(int id, CancellationToken ct)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1. Ochrona prywatności: Sprawdzamy, czy są znajomymi
        var areFriends = await _uow.Friendships.ExistsAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.RequesterId == currentUserId && f.AddresseeId == id) ||
             (f.RequesterId == id && f.AddresseeId == currentUserId)), ct);

        if (!areFriends && currentUserId != id)
        {
            return BadRequest(new { error = "Możesz przeglądać tylko biblioteki swoich znajomych." });
        }

        // 2. Pobieramy gry
        var userGames = await _uow.UserGames.FindAsync(ug => ug.UserId == id, ct);
        var gameIds = userGames.Select(ug => ug.GameId).ToList();

        var games = await _uow.Games.FindAsync(g => gameIds.Contains(g.Id), ct);

        var dtos = games.Select(g => new
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Description,
            Price = g.Price,
            CoverImageUrl = g.CoverImageUrl
        }).ToList();

        return Ok(dtos);
    }
}

public record UserSearchDto(int Id, string Username, string? AvatarUrl);