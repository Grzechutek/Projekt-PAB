// src/GameHub.Application/Interfaces/IAuthService.cs
using GameHub.Application.DTOs.Auth;

namespace GameHub.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ProfileResponse> GetProfileAsync(int userId, CancellationToken ct = default);
    Task UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
}