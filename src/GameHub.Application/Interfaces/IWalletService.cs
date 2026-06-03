// src/GameHub.Application/Interfaces/IWalletService.cs
using GameHub.Application.DTOs.Wallet;

namespace GameHub.Application.Interfaces;

public interface IWalletService
{
    Task<decimal> TopUpAsync(int userId, decimal amount, CancellationToken ct = default);
    Task<List<TransactionDto>> GetHistoryAsync(int userId, CancellationToken ct = default);
    Task BuyGameAsync(int userId, int gameId, CancellationToken ct = default);
}