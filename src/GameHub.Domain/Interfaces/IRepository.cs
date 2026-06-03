// src/GameHub.Domain/Interfaces/IRepository.cs
using System.Linq.Expressions;
using GameHub.SharedKernel;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Generyczny interfejs repozytorium — czysta domena, zero EF Core.
/// Implementacja znajduje się w GameHub.Infrastructure.
/// </summary>
/// <typeparam name="T">Encja dziedzicząca po BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    // ── Odczyt ────────────────────────────────────────────────────────────

    /// <summary>Zwraca encję po Id lub null gdy nie istnieje.</summary>
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Zwraca wszystkie encje danego typu.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Zwraca encje spełniające predykat.</summary>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>Zwraca pierwszą encję spełniającą predykat lub null.</summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>Sprawdza czy istnieje encja spełniająca predykat.</summary>
    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    // ── Zapis ─────────────────────────────────────────────────────────────

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    /// Wersja FindAsync z eager loadingiem powiązanych encji.
    Task<IReadOnlyList<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default,
        params Expression<Func<T, object?>>[] includes);
}