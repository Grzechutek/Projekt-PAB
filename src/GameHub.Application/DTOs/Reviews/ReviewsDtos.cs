// src/GameHub.Application/DTOs/Reviews/ReviewsDtos.cs
using System.ComponentModel.DataAnnotations;

namespace GameHub.Application.DTOs.Reviews;

/// <summary>
/// DTO zwracany przez GET /api/games/{gameId}/reviews.
/// Username pochodzi z powiązanego User (eager loading).
/// IsHidden widoczny dla admina — zwykły user i tak nie dostanie ukrytych recenzji.
/// </summary>
public record ReviewDto(
    int      Id,
    int      UserId,
    string   Username,
    int      GameId,
    int      Rating,
    string?  Content,
    bool     IsHidden,
    DateTime CreatedAt);

/// <summary>
/// Body dla POST /api/games/{gameId}/reviews.
/// </summary>
public record CreateReviewRequest(
    [Required(ErrorMessage = "Ocena jest wymagana.")]
    [Range(1, 10, ErrorMessage = "Ocena musi być liczbą od 1 do 10.")]
    int Rating,

    [MaxLength(2000, ErrorMessage = "Treść recenzji nie może przekraczać 2000 znaków.")]
    string? Content);

/// <summary>
/// Body dla PUT /api/reviews/{id}.
/// Oba pola opcjonalne — aktualizujemy tylko te, które zostały podane.
/// </summary>
public record UpdateReviewRequest(
    [Range(1, 10, ErrorMessage = "Ocena musi być liczbą od 1 do 10.")]
    int? Rating,

    [MaxLength(2000, ErrorMessage = "Treść recenzji nie może przekraczać 2000 znaków.")]
    string? Content);