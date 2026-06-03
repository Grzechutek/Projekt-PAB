// src/GameHub.Application/DTOs/Wallet/TopUpRequest.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Wallet;

public record TopUpRequest([Range(1, 10000)] decimal Amount);