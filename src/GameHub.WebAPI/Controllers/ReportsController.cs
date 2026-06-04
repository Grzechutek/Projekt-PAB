// src/GameHub.WebAPI/Controllers/ReportsController.cs
using GameHub.Application.DTOs.Reports;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IUnitOfWork uow, ILogger<ReportsController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // POST /api/reports  [Authorize]
    //
    // Tworzy zgłoszenie. Reguła biznesowa:
    //   DOKŁADNIE JEDNO z TargetUserId / TargetGameId musi być podane.
    //   Oba null  → "nie wiem co zgłaszam"
    //   Oba podane → "zgłaszam dwie rzeczy naraz" — też niedozwolone
    //
    // Status domyślny: Open (ustawiamy explicite, choć baza też ma default).
    // ──────────────────────────────────────────────────────────────────────
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportRequest req, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ── Walidacja reguły "dokładnie jeden cel" ─────────────────────────
        var hasUser = req.TargetUserId.HasValue;
        var hasGame = req.TargetGameId.HasValue;

        if (hasUser == hasGame)   // oba null LUB oba podane
        {
            return BadRequest(new
            {
                error = hasUser
                    ? "Podaj tylko jeden cel zgłoszenia: TargetUserId albo TargetGameId, nie oba."
                    : "Musisz podać cel zgłoszenia: TargetUserId albo TargetGameId."
            });
        }

        // ── Weryfikacja czy cel istnieje ───────────────────────────────────
        if (hasUser)
        {
            var target = await _uow.Users.GetByIdAsync(req.TargetUserId!.Value, ct);
            if (target is null)
                return NotFound(new { error = $"Użytkownik o Id={req.TargetUserId} nie istnieje." });

            // Nie można zgłosić samego siebie
            if (req.TargetUserId == userId)
                return BadRequest(new { error = "Nie możesz zgłosić samego siebie." });
        }
        else
        {
            var target = await _uow.Games.GetByIdAsync(req.TargetGameId!.Value, ct);
            if (target is null || !target.IsVisible)
                return NotFound(new { error = $"Gra o Id={req.TargetGameId} nie istnieje." });
        }

        var report = new Report
        {
            ReporterId   = userId,
            TargetUserId = req.TargetUserId,
            TargetGameId = req.TargetGameId,
            Reason       = req.Reason,
            Status       = ReportStatus.Open,
            CreatedAt    = DateTime.UtcNow
        };

        await _uow.Reports.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);

        var target_desc = hasUser
            ? $"userId={req.TargetUserId}"
            : $"gameId={req.TargetGameId}";

        _logger.LogInformation(
            "User {UserId} złożył zgłoszenie na {Target} (ReportId={ReportId})",
            userId, target_desc, report.Id);

        return Ok(new { reportId = report.Id, message = "Zgłoszenie zostało przyjęte." });
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/reports  [Admin]
    //
    // Lista wszystkich zgłoszeń dla admina.
    // Eager loading: Reporter + TargetUser (nullable) + TargetGame (nullable).
    // Domyślne sortowanie: najnowsze pierwsze.
    // Opcjonalny filtr po statusie: ?status=Open|InProgress|Closed
    // ──────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        // Parsowanie opcjonalnego filtra statusu
        ReportStatus? filterStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<ReportStatus>(status, out var parsed))
                return BadRequest(new
                {
                    error = "Nieprawidłowy status. Dozwolone wartości: Open, InProgress, Closed."
                });
            filterStatus = parsed;
        }

        // FindWithIncludesAsync z trzema include'ami — Reporter zawsze istnieje,
        // TargetUser i TargetGame są nullable (LEFT JOIN w SQL).
        var reports = await _uow.Reports.FindWithIncludesAsync(
            r => filterStatus == null || r.Status == filterStatus,
            ct,
            r => r.Reporter,
            r => r.TargetUser!,
            r => r.TargetGame!);

        var dtos = reports
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportDto(
                r.Id,
                r.ReporterId,
                r.Reporter.Username,
                r.TargetUserId,
                r.TargetUser?.Username,
                r.TargetGameId,
                r.TargetGame?.Title,
                r.Reason,
                r.Status.ToString(),
                r.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GET /api/reports — Admin, zwrócono {Count} zgłoszeń (filtr: {Filter})",
            dtos.Count, status ?? "brak");

        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // PATCH /api/reports/{id}/status  [Admin]
    //
    // Zmienia status zgłoszenia: Open → InProgress → Closed (lub dowolna kolejność).
    // Status przesyłamy jako string — parsujemy do enum z czytelnym błędem.
    // ──────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateReportStatusRequest req,
        CancellationToken ct)
    {
        // Parsowanie stringa do enum ReportStatus
        if (!Enum.TryParse<ReportStatus>(req.Status, ignoreCase: true, out var newStatus))
        {
            return BadRequest(new
            {
                error = "Nieprawidłowy status. Dozwolone wartości: Open, InProgress, Closed."
            });
        }

        var report = await _uow.Reports.GetByIdAsync(id, ct);
        if (report is null)
            return NotFound(new { error = $"Zgłoszenie o Id={id} nie istnieje." });

        var previousStatus = report.Status.ToString();
        report.Status = newStatus;

        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin zmienił status zgłoszenia Id={ReportId}: {From} → {To}",
            id, previousStatus, newStatus);

        return Ok(new { id, status = newStatus.ToString() });
    }
}