// src/GameHub.Domain/Entities/Report.cs
using GameHub.SharedKernel;

namespace GameHub.Domain.Entities;

public enum ReportStatus
{
    Open,
    InProgress,
    Closed
}

/// <summary>
/// Zg³oszenie dotycz¹ce u¿ytkownika LUB gry.
/// Regu³a biznesowa: dok³adnie jedno z TargetUserId / TargetGameId musi byæ wype³nione.
/// Walidacja odbywa siê w warstwie Application, nie na poziomie bazy.
/// </summary>
public class Report : BaseEntity
{
    public int ReporterId { get; set; }

    /// <summary>U¿ytkownik sk³adaj¹cy zg³oszenie.</summary>
    public User Reporter { get; set; } = null!;

    /// <summary>Docelowy u¿ytkownik (opcjonalnie — jeœli zg³oszenie dotyczy usera).</summary>
    public int? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    /// <summary>Docelowa gra (opcjonalnie — jeœli zg³oszenie dotyczy gry).</summary>
    public int? TargetGameId { get; set; }
    public Game? TargetGame { get; set; }

    public string Reason { get; set; } = string.Empty;

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}