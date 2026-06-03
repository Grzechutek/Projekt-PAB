// src/GameHub.Infrastructure/Persistence/DataSeeder.cs
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        // Uruchamiamy seed tylko gdy baza jest pusta
        if (await db.Users.AnyAsync()) return;

        // ── Użytkownicy ───────────────────────────────────────────────────
        var admin = new User
        {
            Email = "admin@gamehub.com",
            Username = "admin",
            PasswordHash = hasher.Hash("Admin123!"),
            Role = "Admin",
            Bio = "GameHub administrator",
            CreatedAt = DateTime.UtcNow
        };

        var alice = new User
        {
            Email = "alice@gamehub.com",
            Username = "alice",
            PasswordHash = hasher.Hash("Alice123!"),
            Role = "User",
            Bio = "Zapalona graczka RPG",
            WalletBalance = 150m,
            CreatedAt = DateTime.UtcNow
        };

        var bob = new User
        {
            Email = "bob@gamehub.com",
            Username = "bob",
            PasswordHash = hasher.Hash("Bob123!"),
            Role = "User",
            Bio = "Fan strategii i FPS",
            WalletBalance = 80m,
            CreatedAt = DateTime.UtcNow
        };

        await db.Users.AddRangeAsync(admin, alice, bob);

        // ── Gry ───────────────────────────────────────────────────────────
        var games = new List<Game>
        {
            new() {
                Title       = "Cyber Odyssey",
                Description = "Otwarto-światowa gra RPG w cyberpunkowym świecie.",
                Genre       = "RPG",
                Price       = 59.99m,
                IsVisible   = true,
                CreatedAt   = DateTime.UtcNow
            },
            new() {
                Title       = "Shadow Tactics",
                Description = "Taktyczna gra skradankowa w feudalnej Japonii.",
                Genre       = "Strategy",
                Price       = 39.99m,
                IsVisible   = true,
                CreatedAt   = DateTime.UtcNow
            },
            new() {
                Title       = "Galactic Frontier",
                Description = "Kosmiczny shooter z trybem co-op.",
                Genre       = "Shooter",
                Price       = 49.99m,
                IsVisible   = true,
                CreatedAt   = DateTime.UtcNow
            },
            new() {
                Title       = "Dungeon Crawler X",
                Description = "Klasyczny dungeon crawler z proceduralnie generowanymi lochami.",
                Genre       = "RPG",
                Price       = 29.99m,
                IsVisible   = true,
                CreatedAt   = DateTime.UtcNow
            },
            new() {
                Title       = "Racing Legends",
                Description = "Symulator wyścigów z ponad 200 samochodami.",
                Genre       = "Racing",
                Price       = 44.99m,
                IsVisible   = true,
                CreatedAt   = DateTime.UtcNow
            },
            new() {
                Title       = "Beta Test Game",
                Description = "Gra w trybie ukrytym — widoczna tylko dla admina.",
                Genre       = "Action",
                Price       = 9.99m,
                IsVisible   = false,  // soft-delete / ukryta
                CreatedAt   = DateTime.UtcNow
            }
        };

        await db.Games.AddRangeAsync(games);
        await db.SaveChangesAsync();

        // ── Biblioteka Alice (kupiła 2 gry) ───────────────────────────────
        var cyberOdyssey = games[0];
        var shadowTactics = games[1];

        db.UserGames.Add(new UserGame
        {
            UserId = alice.Id,
            GameId = cyberOdyssey.Id,
            PricePaid = cyberOdyssey.Price,
            PurchasedAt = DateTime.UtcNow.AddDays(-10)
        });

        db.UserGames.Add(new UserGame
        {
            UserId = alice.Id,
            GameId = shadowTactics.Id,
            PricePaid = shadowTactics.Price,
            PurchasedAt = DateTime.UtcNow.AddDays(-3)
        });

        // ── Transakcje Alice ──────────────────────────────────────────────
        db.Transactions.Add(new Transaction
        {
            UserId = alice.Id,
            GameId = null,
            Amount = 200m,
            Type = TransactionType.TopUp,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        });

        db.Transactions.Add(new Transaction
        {
            UserId = alice.Id,
            GameId = cyberOdyssey.Id,
            Amount = cyberOdyssey.Price,
            Type = TransactionType.Purchase,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        db.Transactions.Add(new Transaction
        {
            UserId = alice.Id,
            GameId = shadowTactics.Id,
            Amount = shadowTactics.Price,
            Type = TransactionType.Purchase,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });

        // ── Recenzje Alice ────────────────────────────────────────────────
        db.Reviews.Add(new Review
        {
            UserId = alice.Id,
            GameId = cyberOdyssey.Id,
            Rating = 9,
            Content = "Niesamowity świat i fabuła, polecam każdemu fanowi RPG!",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        db.Reviews.Add(new Review
        {
            UserId = alice.Id,
            GameId = shadowTactics.Id,
            Rating = 8,
            Content = "Świetna taktyka, wymagająca ale satysfakcjonująca.",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });

        // ── Biblioteka Boba (kupił 1 grę) ─────────────────────────────────
        var galactic = games[2];

        db.UserGames.Add(new UserGame
        {
            UserId = bob.Id,
            GameId = galactic.Id,
            PricePaid = galactic.Price,
            PurchasedAt = DateTime.UtcNow.AddDays(-5)
        });

        db.Transactions.Add(new Transaction
        {
            UserId = bob.Id,
            GameId = null,
            Amount = 100m,
            Type = TransactionType.TopUp,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });

        db.Transactions.Add(new Transaction
        {
            UserId = bob.Id,
            GameId = galactic.Id,
            Amount = galactic.Price,
            Type = TransactionType.Purchase,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });

        // ── Znajomi (Alice i Bob) ─────────────────────────────────────────
        db.Friendships.Add(new Friendship
        {
            RequesterId = alice.Id,
            AddresseeId = bob.Id,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        });

        // ── Zgłoszenie ────────────────────────────────────────────────────
        db.Reports.Add(new Report
        {
            ReporterId = bob.Id,
            TargetUserId = null,
            TargetGameId = games[5].Id,  // ukryta gra
            Reason = "Podejrzana zawartość w opisie gry.",
            Status = ReportStatus.Open,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        await db.SaveChangesAsync();
    }
}