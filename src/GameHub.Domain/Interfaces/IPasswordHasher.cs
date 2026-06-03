// src/GameHub.Domain/Interfaces/IPasswordHasher.cs
namespace GameHub.Domain.Interfaces;

/// <summary>
/// Abstrakcja hashowania hase³ — implementacja ¿yje w Infrastructure.
/// Domain definiuje kontrakt, nie zna BCrypt ani ASP.NET Identity.
/// </summary>
public interface IPasswordHasher
{
	string Hash(string plainPassword);
	bool Verify(string plainPassword, string hash);
}