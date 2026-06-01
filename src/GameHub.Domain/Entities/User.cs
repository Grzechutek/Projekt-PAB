// src/GameHub.Domain/Entities/User.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    /// <summary>"User" | "Admin" | "Moderator"</summary>
    public string Role { get; set; } = "User";

    /// <summary>0 = aktywny, 1 = zablokowany (soft-delete)</summary>
    public bool IsBlocked { get; set; } = false;

    public decimal WalletBalance { get; set; } = 0m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Nawigacje ──────────────────────────────────────────
    public ICollection<UserGame> Library { get; set; } = new List<UserGame>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>Zaproszenia do znajomych wysłane przez tego użytkownika.</summary>
    public ICollection<Friendship> SentFriendships { get; set; } = new List<Friendship>();

    /// <summary>Zaproszenia do znajomych odebrane przez tego użytkownika.</summary>
    public ICollection<Friendship> ReceivedFriendships { get; set; } = new List<Friendship>();

    /// <summary>Zgłoszenia złożone przez tego użytkownika.</summary>
    public ICollection<Report> FiledReports { get; set; } = new List<Report>();

    /// <summary>Zgłoszenia, w których ten użytkownik jest celem.</summary>
    public ICollection<Report> ReportsAgainst { get; set; } = new List<Report>();
}