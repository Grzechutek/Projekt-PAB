// src/GameHub.Application/DTOs/Games/GamesDtos.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Games;

public record GameDto(
    int     Id,
    string  Title,
    string? Description,
    string? Genre,
    decimal Price,
    string? CoverImageUrl,
    bool    IsVisible,
    DateTime CreatedAt,
    double? AverageRating);

/// <summary>
/// Body dla POST /api/games  [Admin].
/// </summary>
public record CreateGameRequest(
    [Required(ErrorMessage = "Tytuł jest wymagany.")]
    [MaxLength(256, ErrorMessage = "Tytuł nie może przekraczać 256 znaków.")]
    string Title,

    string? Description,
    string? Genre,

    [Required(ErrorMessage = "Cena jest wymagana.")]
    [Range(0, 10_000, ErrorMessage = "Cena musi być między 0 a 10 000.")]
    decimal Price,

    string? CoverImageUrl);

/// <summary>
/// Body dla PUT /api/games/{id}  [Admin].
/// Wszystkie pola opcjonalne — aktualizujemy tylko te, które zostały podane (≠ null).
/// </summary>
public record UpdateGameRequest(
    [MaxLength(256, ErrorMessage = "Tytuł nie może przekraczać 256 znaków.")]
    string? Title,

    string? Description,
    string? Genre,

    [Range(0, 10_000, ErrorMessage = "Cena musi być między 0 a 10 000.")]
    decimal? Price,

    string? CoverImageUrl);