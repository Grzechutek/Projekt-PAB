// src/GameHub.Infrastructure/Persistence/EfUnitOfWork.cs
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameHub.Infrastructure.Persistence;

/// <summary>
/// Implementacja Unit of Work.
///
/// Po co w ogóle UoW, skoro DbContext sam zarządza transakcjami?
/// - Ukrywa EF Core przed warstwą Application (ta zna tylko interfejs IUnitOfWork).
/// - Jeden SaveChangesAsync() zatwierdza zmiany z WIELU repozytoriów naraz.
/// - Łatwy do zamockowania w testach jednostkowych.
/// </summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    // Repozytoria tworzymy leniwie (lazy) — instancja powstaje dopiero
    // przy pierwszym odwołaniu. Oszczędza zasoby gdy nie każdy request
    // potrzebuje wszystkich repozytoriów.
    private IRepository<User>? _users;
    private IRepository<Game>? _games;
    private IRepository<UserGame>? _userGames;
    private IRepository<Review>? _reviews;
    private IRepository<Friendship>? _friendships;
    private IRepository<Report>? _reports;
    private IRepository<Transaction>? _transactions;

    // Przechowujemy aktywną transakcję bazodanową (jeśli używamy jawnej)
    private IDbContextTransaction? _currentTransaction;

    public EfUnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    // Operator ??= = "jeśli null, utwórz nową instancję i przypisz"
    public IRepository<User> Users => _users ??= new EfRepository<User>(_db);
    public IRepository<Game> Games => _games ??= new EfRepository<Game>(_db);
    public IRepository<UserGame> UserGames => _userGames ??= new EfRepository<UserGame>(_db);
    public IRepository<Review> Reviews => _reviews ??= new EfRepository<Review>(_db);
    public IRepository<Friendship> Friendships => _friendships ??= new EfRepository<Friendship>(_db);
    public IRepository<Report> Reports => _reports ??= new EfRepository<Report>(_db);
    public IRepository<Transaction> Transactions => _transactions ??= new EfRepository<Transaction>(_db);

    // ── Zapis ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Zatwierdza wszystkie oczekujące zmiany (INSERT / UPDATE / DELETE)
    /// w jednej transakcji bazodanowej.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    // ── Jawne transakcje ──────────────────────────────────────────────────
    // Używasz ich gdy jedna operacja biznesowa modyfikuje wiele tabel
    // i chcesz mieć pewność atomowości (wszystko albo nic).

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException(
                "Transakcja już jest aktywna. Zatwierdź lub wycofaj ją przed rozpoczęciem nowej.");

        _currentTransaction = await _db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("Brak aktywnej transakcji.");

        try
        {
            await _db.SaveChangesAsync(ct);
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null) return;

        try { await _currentTransaction.RollbackAsync(ct); }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────
    // DbContext jest Disposable — zwalnia połączenie z bazą.
    // DI w ASP.NET Core wywoła Dispose automatycznie po zakończeniu requestu
    // (gdy UoW jest zarejestrowany jako Scoped).

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _db.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();

        await _db.DisposeAsync();
    }
}