// src/GameHub.Application/DTOs/Reports/ReportsDtos.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Reports;

/// <summary>
/// Body dla POST /api/reports.
/// Reguła: dokładnie jedno z TargetUserId / TargetGameId musi być podane.
/// Walidacja tej reguły odbywa się w kontrolerze (nie da się wyrazić przez DataAnnotations).
/// </summary>
public record CreateReportRequest(
    int? TargetUserId,
    int? TargetGameId,

    [Required(ErrorMessage = "Powód zgłoszenia jest wymagany.")]
    [MaxLength(1000, ErrorMessage = "Powód nie może przekraczać 1000 znaków.")]
    string Reason);

/// <summary>
/// Body dla PATCH /api/reports/{id}/status  [Admin].
/// Status jako string — dozwolone wartości: "Open", "InProgress", "Closed".
/// Parsowanie do enum ReportStatus odbywa się w kontrolerze z czytelnym błędem 400.
/// </summary>
public record UpdateReportStatusRequest(
    [Required(ErrorMessage = "Status jest wymagany.")]
    string Status);

/// <summary>
/// DTO zwracany przez GET /api/reports  [Admin].
/// Pola Target* są nullable — zgłoszenie dotyczy albo usera albo gry.
/// </summary>
public record ReportDto(
    int      Id,
    int      ReporterId,
    string   ReporterUsername,
    int?     TargetUserId,
    string?  TargetUsername,
    int?     TargetGameId,
    string?  TargetGameTitle,
    string   Reason,
    string   Status,
    DateTime CreatedAt);