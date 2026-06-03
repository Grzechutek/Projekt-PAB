// src/GameHub.Application/DTOs/Wallet/TransactionDto.cs
namespace GameHub.Application.DTOs.Wallet;

public record TransactionDto(
    int Id,
    int? GameId,
    string? GameTitle,
    decimal Amount,
    string Type,
    DateTime CreatedAt
);