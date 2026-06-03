// src/GameHub.Infrastructure/Persistence/EfRepository.cs
using System.Linq.Expressions;
using GameHub.Domain.Interfaces;
using GameHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Persistence;

/// <summary>
/// Generyczna implementacja IRepository&lt;T&gt; oparta na EF Core.
/// Jeden plik obsługuje WSZYSTKIE encje — nie musisz pisać UserRepository,
/// GameRepository itd. osobno (chyba że potrzebujesz specjalnych zapytań).
/// </summary>
public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    // DbContext wstrzykujemy przez konstruktor — standard DI w .NET
    protected readonly AppDbContext _db;

    // DbSet<T> to "tabela" dla konkretnego T.
    // Jeśli T = User, _set odpowiada tabeli Users.
    protected readonly DbSet<T> _set;

    public EfRepository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    // ── Odczyt ────────────────────────────────────────────────────────────

    // FindAsync(id) → EF sprawdza najpierw cache (Identity Map), potem bazę.
    // Zwraca null jeśli nie znaleziono — nie rzuca wyjątku.
    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _set.FindAsync(new object[] { id }, ct);

    // AsNoTracking() = EF nie śledzi zmian w zwróconych obiektach.
    // Szybsze przy odczycie, bo nie ma overhead'u change trackera.
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    // Expression<Func<T, bool>> to drzewo wyrażeń — EF tłumaczy je na WHERE w SQL.
    // Gdybyśmy przyjęli zwykły Func<T, bool>, EF ściągnąłby całą tabelę do pamięci
    // i filtrował w C#. Expression = filtrowanie po stronie bazy.
    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);

    // ── Zapis ─────────────────────────────────────────────────────────────

    // AddAsync tylko dodaje encję do "kolejki zmian" EF (change tracker).
    // Faktyczny INSERT wykonuje się dopiero przy SaveChangesAsync() w UnitOfWork.
    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await _set.AddRangeAsync(entities, ct);

    // Update i Remove są synchroniczne — tylko zmieniają stan w change trackerze,
    // nie robią żadnego I/O. SaveChangesAsync() wysyła zmiany do bazy.
    public void Update(T entity)
        => _set.Update(entity);

    public void Remove(T entity)
        => _set.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities)
        => _set.RemoveRange(entities);

    public async Task<IReadOnlyList<T>> FindWithIncludesAsync(
    Expression<Func<T, bool>> predicate,
    CancellationToken ct = default,
    params Expression<Func<T, object?>>[] includes)
    {
        IQueryable<T> query = _set.AsNoTracking().Where(predicate);

        foreach (var include in includes)
            query = query.Include(include);

        return await query.ToListAsync(ct);
    }
}