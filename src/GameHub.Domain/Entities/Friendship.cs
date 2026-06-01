// src/GameHub.Domain/Entities/Friendship.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected
}

/// <summary>
/// Relacja znajomoœci miêdzy dwoma u¿ytkownikami.
/// Constraint UNIQUE(RequesterId, AddresseeId) — jeden kierunek zaproszenia.
/// </summary>
public class Friendship : BaseEntity
{
    public int RequesterId { get; set; }

    /// <summary>U¿ytkownik wysy³aj¹cy zaproszenie.</summary>
    public User Requester { get; set; } = null!;

    public int AddresseeId { get; set; }

    /// <summary>U¿ytkownik odbieraj¹cy zaproszenie.</summary>
    public User Addressee { get; set; } = null!;

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}