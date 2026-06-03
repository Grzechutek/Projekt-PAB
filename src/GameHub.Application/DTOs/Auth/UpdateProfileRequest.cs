// src/GameHub.Application/DTOs/Auth/UpdateProfileRequest.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Auth;

public record UpdateProfileRequest(
    [MaxLength(500)] string? Bio,
    [MaxLength(300)] string? AvatarUrl
);