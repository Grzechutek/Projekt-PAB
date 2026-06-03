// src/GameHub.Application/DTOs/Auth/LoginRequest.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);