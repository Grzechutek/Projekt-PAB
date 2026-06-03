// src/GameHub.Domain/Interfaces/ITokenService.cs
using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Kontrakt generowania JWT. Implementacja (JwtTokenService) ¿yje w Infrastructure.
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}