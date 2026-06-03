// src/GameHub.Application/DTOs/Auth/RegisterRequest.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Auth;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(3), MaxLength(64)] string Username,
    [Required, MinLength(6)] string Password
);