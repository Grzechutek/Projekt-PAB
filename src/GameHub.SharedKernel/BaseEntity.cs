// src/GameHub.SharedKernel/BaseEntity.cs
namespace GameHub.SharedKernel;

/// <summary>
/// Wspólna klasa bazowa dla wszystkich encji domenowych.
/// Nie zawiera żadnych referencji do EF Core — czysta domena.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; protected set; }
}