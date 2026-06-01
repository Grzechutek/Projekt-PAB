// src/GameHub.Domain/Entities/Transaction.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

public enum TransactionType
{
    Purchase,
    Refund,
    TopUp   // do³adowanie portfela
}

/// <summary>
/// Historia operacji finansowych — zakupy, zwroty, do³adowania portfela.
/// </summary>
public class Transaction : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Powi¹zana gra. Null przy TopUp (do³adowanie portfela nie dotyczy konkretnej gry).
    /// </summary>
    public int? GameId { get; set; }
    public Game? Game { get; set; }

    /// <summary>Kwota operacji (zawsze dodatnia; typ okreœla kierunek).</summary>
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}