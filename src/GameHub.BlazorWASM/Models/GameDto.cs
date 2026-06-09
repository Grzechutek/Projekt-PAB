namespace GameHub.BlazorWASM.Models;

public record GameDto(
    int Id, 
    string Title, 
    string? Description, 
    string? Genre, 
    decimal Price, 
    string? CoverImageUrl, 
    bool IsVisible, 
    DateTime CreatedAt, 
    double? AverageRating
);