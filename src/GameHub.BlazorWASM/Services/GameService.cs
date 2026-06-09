using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using GameHub.BlazorWASM.Models;

namespace GameHub.BlazorWASM.Services;

public class GameService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public GameService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<List<GameDto>> GetGamesAsync()
    {
        return await _http.GetFromJsonAsync<List<GameDto>>("api/games") ?? new List<GameDto>();
    }

    public async Task<bool> BuyGameAsync(int gameId)
    {
        // Wyciągamy token z pamięci przeglądarki
        var token = await _localStorage.GetItemAsync<string>("authToken");
        
        if (!string.IsNullOrEmpty(token))
        {
            // Dołączamy token do nagłówka zapytania
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Wysyłamy zapytanie POST (bez body, samo ID w URLu)
        var response = await _http.PostAsync($"api/games/{gameId}/buy", null);
        
        return response.IsSuccessStatusCode;
    }
    public async Task<List<GameDto>> GetLibraryAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.GetFromJsonAsync<List<GameDto>>("api/games/library") ?? new List<GameDto>();
    }
}