// src/GameHub.Application/DTOs/Admin/AdminDtos.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Admin;

/// <summary>
/// DTO użytkownika zwracany przez GET /api/admin/users [Admin].
/// Zawiera pola potrzebne do tabeli w panelu admina (MudDataGrid).
/// </summary>
public record UserAdminDto(
    int      Id,
    string   Username,
    string   Email,
    string   Role,
    bool     IsBlocked,
    decimal  WalletBalance,
    DateTime CreatedAt);

/// <summary>
/// Body dla PATCH /api/admin/users/{id}/role.
/// Dozwolone role: "User", "Admin".
/// </summary>
public record UpdateRoleRequest(
    [Required(ErrorMessage = "Rola jest wymagana.")]
    string Role);