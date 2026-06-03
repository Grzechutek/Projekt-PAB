// src/GameHub.Application/Services/AuthService.cs
using GameHub.Application.DTOs.Auth;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;

namespace GameHub.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens)
    {
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req,
        CancellationToken ct = default)
    {
        // ExistsAsync → AnyAsync po stronie bazy, zero alokacji listy
        if (await _uow.Users.ExistsAsync(u => u.Email == req.Email, ct))
            throw new InvalidOperationException("Email already in use.");

        if (await _uow.Users.ExistsAsync(u => u.Username == req.Username, ct))
            throw new InvalidOperationException("Username already taken.");

        var user = new User
        {
            Email = req.Email,
            Username = req.Username,
            PasswordHash = _hasher.Hash(req.Password),
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);   // Id zostaje nadany przez bazę

        return new AuthResponse(_tokens.GenerateToken(user), user.Username, user.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req,
        CancellationToken ct = default)
    {
        // FirstOrDefaultAsync → jeden SELECT z WHERE, nie ściągamy listy
        var user = await _uow.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("Account is blocked.");

        return new AuthResponse(_tokens.GenerateToken(user), user.Username, user.Role);
    }

    public async Task<ProfileResponse> GetProfileAsync(int userId,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return new ProfileResponse(
            user.Id, user.Email, user.Username,
            user.AvatarUrl, user.Bio, user.Role,
            user.WalletBalance, user.CreatedAt);
    }

    public async Task UpdateProfileAsync(int userId, UpdateProfileRequest req,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (req.Bio is not null) user.Bio = req.Bio;
        if (req.AvatarUrl is not null) user.AvatarUrl = req.AvatarUrl;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
    }
}