// src/GameHub.Application/DTOs/Auth/ProfileResponse.cs
namespace GameHub.Application.DTOs.Auth;

public record ProfileResponse(
    int Id,
    string Email,
    string Username,
    string? AvatarUrl,
    string? Bio,
    string Role,
    decimal WalletBalance,
    DateTime CreatedAt
);