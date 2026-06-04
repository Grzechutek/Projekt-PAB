// src/GameHub.Application/DTOs/UserGames/UserGamesDtos.cs
namespace GameHub.Application.DTOs.UserGames;

/// <summary>
/// DTO zwracany przez GET /api/users/me/games.
/// Pokazuje gry z biblioteki zalogowanego użytkownika
/// razem z ceną zapłaconą i datą zakupu.
/// </summary>
public record UserGameDto(
    int      GameId,
    string   Title,
    string?  Genre,
    string?  CoverImageUrl,
    decimal  PricePaid,
    DateTime PurchasedAt);