// src/GameHub.Domain/Interfaces/IUnitOfWork.cs
using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Unit of Work — koordynuje repozytoria i zatwierdza zmiany w jednej transakcji.
/// Implementacja w GameHub.Infrastructure (EfUnitOfWork).
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
	// ── Repozytoria ──────────────────────────────────────────────────────
	IRepository<User> Users { get; }
	IRepository<Game> Games { get; }
	IRepository<UserGame> UserGames { get; }
	IRepository<Review> Reviews { get; }
	IRepository<Friendship> Friendships { get; }
	IRepository<Report> Reports { get; }
	IRepository<Transaction> Transactions { get; }

	// ── Zapis ────────────────────────────────────────────────────────────

	/// <summary>
	/// Zatwierdza wszystkie oczekujące zmiany w bazie danych.
	/// Zwraca liczbę zmodyfikowanych rekordów.
	/// </summary>
	Task<int> SaveChangesAsync(CancellationToken ct = default);

	// ── Transakcje (opcjonalnie) ─────────────────────────────────────────

	/// <summary>Rozpoczyna jawną transakcję bazodanową.</summary>
	Task BeginTransactionAsync(CancellationToken ct = default);

	/// <summary>Zatwierdza jawną transakcję.</summary>
	Task CommitTransactionAsync(CancellationToken ct = default);

	/// <summary>Wycofuje jawną transakcję.</summary>
	Task RollbackTransactionAsync(CancellationToken ct = default);
}