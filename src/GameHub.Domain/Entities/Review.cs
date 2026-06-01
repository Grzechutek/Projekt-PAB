// src/GameHub.Domain/Entities/Review.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

/// <summary>
/// Recenzja gry wystawiona przez u¿ytkownika.
/// Constraint UNIQUE(UserId, GameId) — jedna recenzja per gra per user.
/// Rating: 1–10 (walidacja w warstwie Application).
/// </summary>
public class Review : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    /// <summary>Ocena w skali 1–10.</summary>
    public int Rating { get; set; }

    public string? Content { get; set; }

    /// <summary>0 = widoczna, 1 = ukryta przez moderatora.</summary>
    public bool IsHidden { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}