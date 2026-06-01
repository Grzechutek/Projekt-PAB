// src/GameHub.Infrastructure/Persistence/AppDbContext.cs
using GameHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence;

/// <summary>
/// Główny kontekst bazy danych. Dziedziczy po DbContext z EF Core.
/// Żyje w Infrastructure — Domain o nim nie wie.
/// </summary>
public class AppDbContext : DbContext
{
    // Konstruktor przyjmuje opcje z zewnątrz (np. connection string).
    // Dzięki temu możemy wstrzyknąć różne bazy: SQLite na dev, inne na prod.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet = "tabela" widziana z C#. EF Core sam tłumaczy LINQ → SQL.
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<UserGame> UserGames => Set<UserGame>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // OnModelCreating = miejsce na "Fluent API" — precyzyjna konfiguracja
    // tabel, kolumn, indeksów i relacji. Alternatywa dla atrybutów [Key], [Required].
    // Fluent API ma pierwszeństwo przed atrybutami i jest bardziej czytelne.
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── USERS ──────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);

            e.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            e.HasIndex(u => u.Email).IsUnique();   // UNIQUE constraint

            e.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(64);

            e.HasIndex(u => u.Username).IsUnique(); // UNIQUE constraint

            e.Property(u => u.PasswordHash).IsRequired();

            e.Property(u => u.Role)
                .IsRequired()
                .HasDefaultValue("User");

            e.Property(u => u.IsBlocked)
                .IsRequired()
                .HasDefaultValue(false);

            e.Property(u => u.WalletBalance)
                .IsRequired()
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18,2)");

            // DateTime zapisujemy jako TEXT ISO 8601 — SQLite nie ma DATETIME
            e.Property(u => u.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),           // C# → baza: "2025-06-01T12:00:00Z"
                    v => DateTime.Parse(v));        // baza → C#
        });

        // ── GAMES ──────────────────────────────────────────────────────────
        mb.Entity<Game>(e =>
        {
            e.HasKey(g => g.Id);

            e.Property(g => g.Title).IsRequired().HasMaxLength(256);

            e.Property(g => g.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            e.Property(g => g.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);

            e.Property(g => g.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));
        });

        // ── USER_GAMES ─────────────────────────────────────────────────────
        mb.Entity<UserGame>(e =>
        {
            e.HasKey(ug => ug.Id);

            // UNIQUE(UserId, GameId) — nie można kupić tej samej gry dwa razy
            e.HasIndex(ug => new { ug.UserId, ug.GameId }).IsUnique();

            e.Property(ug => ug.PricePaid)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            e.Property(ug => ug.PurchasedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));

            // Relacja: UserGame → User
            e.HasOne(ug => ug.User)
                .WithMany(u => u.Library)
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja: UserGame → Game
            e.HasOne(ug => ug.Game)
                .WithMany(g => g.Owners)
                .HasForeignKey(ug => ug.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── REVIEWS ────────────────────────────────────────────────────────
        mb.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);

            // UNIQUE(UserId, GameId) — jedna recenzja per gra per user
            e.HasIndex(r => new { r.UserId, r.GameId }).IsUnique();

            // CHECK constraint: Rating między 1 a 10
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Reviews_Rating", "Rating BETWEEN 1 AND 10"));

            e.Property(r => r.Rating).IsRequired();
            e.Property(r => r.IsHidden).IsRequired().HasDefaultValue(false);

            e.Property(r => r.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));

            e.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Game)
                .WithMany(g => g.Reviews)
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FRIENDSHIPS ────────────────────────────────────────────────────
        mb.Entity<Friendship>(e =>
        {
            e.HasKey(f => f.Id);

            // UNIQUE(RequesterId, AddresseeId) — jeden kierunek zaproszenia
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();

            // Enum FriendshipStatus zapisujemy jako string ("Pending" etc.)
            // Czytelniejsze w bazie niż liczby 0/1/2
            e.Property(f => f.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(FriendshipStatus.Pending);

            e.Property(f => f.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));

            // Dwie relacje do tej samej tabeli Users — EF Core wymaga
            // jawnego wskazania, które nawigacje do siebie pasują.
            e.HasOne(f => f.Requester)
                .WithMany(u => u.SentFriendships)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict zamiast Cascade
                                                    // bo SQLite nie lubi
                                                    // kaskad na self-ref

            e.HasOne(f => f.Addressee)
                .WithMany(u => u.ReceivedFriendships)
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── REPORTS ────────────────────────────────────────────────────────
        mb.Entity<Report>(e =>
        {
            e.HasKey(r => r.Id);

            e.Property(r => r.Reason).IsRequired();

            e.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(ReportStatus.Open);

            e.Property(r => r.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));

            // Reporter — zawsze wypełniony
            e.HasOne(r => r.Reporter)
                .WithMany(u => u.FiledReports)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // TargetUser — opcjonalny (nullable FK)
            e.HasOne(r => r.TargetUser)
                .WithMany(u => u.ReportsAgainst)
                .HasForeignKey(r => r.TargetUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // TargetGame — opcjonalny (nullable FK)
            e.HasOne(r => r.TargetGame)
                .WithMany(g => g.Reports)
                .HasForeignKey(r => r.TargetGameId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TRANSACTIONS ───────────────────────────────────────────────────
        mb.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);

            e.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            e.Property(t => t.Type)
                .IsRequired()
                .HasConversion<string>(); // "Purchase" / "Refund" / "TopUp"

            e.Property(t => t.CreatedAt)
                .IsRequired()
                .HasConversion(
                    v => v.ToString("o"),
                    v => DateTime.Parse(v));

            e.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // GameId jest nullable — TopUp nie ma powiązanej gry
            e.HasOne(t => t.Game)
                .WithMany(g => g.Transactions)
                .HasForeignKey(t => t.GameId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}