// src/GameHub.Domain/Entities/Game.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

public class Game : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Genre { get; set; }
    public decimal Price { get; set; }
    public string? CoverImageUrl { get; set; }

    /// <summary>1 = widoczna w sklepie, 0 = ukryta (soft-delete).</summary>
    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Nawigacje ──────────────────────────────────────────
    public ICollection<UserGame> Owners { get; set; } = new List<UserGame>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}