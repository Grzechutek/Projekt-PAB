using System.Net.Http.Json;
using Blazored.LocalStorage;
using GameHub.BlazorWASM.Models;
using GameHub.BlazorWASM.Security;
using Microsoft.AspNetCore.Components.Authorization;

namespace GameHub.BlazorWASM.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    public async Task<string?> LoginAsync(LoginRequest loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            // Możemy zwrócić treść błędu z backendu
            return "Nieprawidłowe dane logowania.";
        }
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        
        await _localStorage.SetItemAsync("authToken", result!.Token);
        ((CustomAuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(result.Token);
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

        return null; // Null oznacza sukces (brak błędów)
    }
    public async Task<string?> RegisterAsync(RegisterRequest registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerRequest);

        if (!response.IsSuccessStatusCode)
        {
            return "Błąd rejestracji. Użytkownik o takim emailu lub nazwie może już istnieć.";
        }

        return null; // Null oznacza sukces
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    // --- METODA DOŁADOWANIA PORTFELA ---
    public async Task<bool> TopUpWalletAsync(decimal amount)
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Wysyłamy prośbę na nowy endpoint backendowy, który zaraz stworzymy
        var response = await _httpClient.PostAsJsonAsync("api/auth/topup", new { Amount = amount });
        
        return response.IsSuccessStatusCode;
    }
}