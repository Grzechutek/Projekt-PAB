// src/GameHub.Application/Services/WalletService.cs
using GameHub.Application.DTOs.Wallet;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;

namespace GameHub.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _uow;
    public WalletService(IUnitOfWork uow) => _uow = uow;

    public async Task<decimal> TopUpAsync(int userId, decimal amount,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            user.WalletBalance += amount;
            _uow.Users.Update(user);

            await _uow.Transactions.AddAsync(new Transaction
            {
                UserId = userId,
                GameId = null,
                Amount = amount,
                Type = TransactionType.TopUp,
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _uow.CommitTransactionAsync(ct); // SaveChanges + Commit w jednym
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }

        return user.WalletBalance;
    }

    public async Task<List<TransactionDto>> GetHistoryAsync(int userId,
    CancellationToken ct = default)
    {
        var txs = await _uow.Transactions.FindWithIncludesAsync(
            t => t.UserId == userId,
            ct,
            t => t.Game!);

        return txs
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto(
                t.Id,
                t.GameId,
                t.Game?.Title,
                t.Amount,
                t.Type.ToString(),
                t.CreatedAt))
            .ToList();
    }

    public async Task BuyGameAsync(int userId, int gameId,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var game = await _uow.Games.GetByIdAsync(gameId, ct)
            ?? throw new KeyNotFoundException("Game not found.");

        if (!game.IsVisible)
            throw new KeyNotFoundException("Game not found.");

        // ExistsAsync → AnyAsync po stronie bazy
        if (await _uow.UserGames.ExistsAsync(
                ug => ug.UserId == userId && ug.GameId == gameId, ct))
            throw new InvalidOperationException("You already own this game.");

        if (user.WalletBalance < game.Price)
            throw new InvalidOperationException("Insufficient wallet balance.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            user.WalletBalance -= game.Price;
            _uow.Users.Update(user);

            await _uow.UserGames.AddAsync(new UserGame
            {
                UserId = userId,
                GameId = gameId,
                PricePaid = game.Price,
                PurchasedAt = DateTime.UtcNow
            }, ct);

            await _uow.Transactions.AddAsync(new Transaction
            {
                UserId = userId,
                GameId = gameId,
                Amount = game.Price,
                Type = TransactionType.Purchase,
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}