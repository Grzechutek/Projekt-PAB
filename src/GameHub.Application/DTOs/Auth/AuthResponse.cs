// src/GameHub.Application/DTOs/Auth/AuthResponse.cs
namespace GameHub.Application.DTOs.Auth;

public record AuthResponse(string Token, string Username, string Role);