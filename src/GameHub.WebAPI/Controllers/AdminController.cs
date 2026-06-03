// src/GameHub.WebAPI/Controllers/AdminController.cs
using GameHub.Application.DTOs.Admin;
using GameHub.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameHub.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]   // cały kontroler tylko dla admina
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AdminController> _logger;

    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "User", "Admin" };

    public AdminController(IUnitOfWork uow, ILogger<AdminController> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────
    // GET /api/admin/users  [Admin]
    //
    // Lista wszystkich użytkowników — zasila MudDataGrid w panelu admina.
    // Sortowanie po dacie rejestracji (najnowsi pierwsi).
    // ──────────────────────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _uow.Users.GetAllAsync(ct);

        var dtos = users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto(
                u.Id, u.Username, u.Email,
                u.Role, u.IsBlocked, u.WalletBalance, u.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GET /api/admin/users — zwrócono {Count} użytkowników", dtos.Count);

        return Ok(dtos);
    }

    // ──────────────────────────────────────────────────────────────────────
    // PATCH /api/admin/users/{id}/block  [Admin]
    //
    // Toggle blokady konta: IsBlocked = !IsBlocked.
    // Admin nie może zablokować własnego konta.
    // Zwraca aktualny stan IsBlocked po zmianie.
    // ──────────────────────────────────────────────────────────────────────
    [HttpPatch("users/{id:int}/block")]
    public async Task<IActionResult> ToggleBlock(int id, CancellationToken ct)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (id == adminId)
            return BadRequest(new { error = "Nie możesz zablokować własnego konta." });

        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound(new { error = $"Użytkownik o Id={id} nie istnieje." });

        user.IsBlocked = !user.IsBlocked;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        var action = user.IsBlocked ? "zablokował" : "odblokował";
        _logger.LogInformation(
            "Admin {AdminId} {Action} konto userId={UserId}",
            adminId, action, id);

        return Ok(new { id, isBlocked = user.IsBlocked });
    }

    // ──────────────────────────────────────────────────────────────────────
    // PATCH /api/admin/users/{id}/role  [Admin]
    //
    // Zmienia rolę użytkownika. Dozwolone: "User", "Admin".
    // Admin nie może zmienić własnej roli — uchroni się przed przypadkową
    // utratą dostępu do panelu admina.
    // ──────────────────────────────────────────────────────────────────────
    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> UpdateRole(
        int id,
        [FromBody] UpdateRoleRequest req,
        CancellationToken ct)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (id == adminId)
            return BadRequest(new { error = "Nie możesz zmienić własnej roli." });

        if (!AllowedRoles.Contains(req.Role))
            return BadRequest(new
            {
                error = "Nieprawidłowa rola. Dozwolone wartości: User, Admin."
            });

        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound(new { error = $"Użytkownik o Id={id} nie istnieje." });

        var previousRole = user.Role;
        user.Role = req.Role;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin {AdminId} zmienił rolę userId={UserId}: {From} → {To}",
            adminId, id, previousRole, req.Role);

        return Ok(new { id, role = user.Role });
    }
}