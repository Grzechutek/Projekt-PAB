// src/GameHub.Domain/Entities/UserGame.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

/// <summary>
/// Tabela łącząca Users ↔ Games (biblioteka użytkownika).
/// Constraint UNIQUE(UserId, GameId) — jeden zakup per gra per user.
/// </summary>
public class UserGame : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public decimal PricePaid { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}