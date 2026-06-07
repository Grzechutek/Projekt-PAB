# GameHub — Dokumentacja Techniczna
## Warstwa autoryzacji, portfela i infrastruktury

> **Wersja:** 1.0  
> **Data:** Czerwiec 2026  
> **Technologie:** ASP.NET Core 8, Entity Framework Core 8, SQLite, JWT Bearer, BCrypt, Serilog, Clean Architecture

---

## Spis treści

1. [Architektura systemu](#1-architektura-systemu)
2. [Warstwa domenowa — GameHub.Domain](#2-warstwa-domenowa--gamehubdomain)
3. [Warstwa aplikacyjna — GameHub.Application](#3-warstwa-aplikacyjna--gamehubapplication)
4. [Warstwa infrastruktury — GameHub.Infrastructure](#4-warstwa-infrastruktury--gamehubinfrastructure)
5. [Warstwa API — GameHub.WebAPI](#5-warstwa-api--gamehubwebapi)
6. [Baza danych i migracje](#6-baza-danych-i-migracje)
7. [Autoryzacja JWT](#7-autoryzacja-jwt)
8. [System portfela](#8-system-portfela)
9. [Dane seedowe](#9-dane-seedowe)
10. [Konfiguracja i uruchomienie](#10-konfiguracja-i-uruchomienie)
11. [Endpointy API](#11-endpointy-api)

---

## 1. Architektura systemu

Projekt stosuje **Clean Architecture** — każda warstwa zna tylko warstwy wewnętrzne, nigdy zewnętrzne. Zależności płyną wyłącznie do środka.

```
┌─────────────────────────────────────────────────┐
│               GameHub.WebAPI                    │  ← Warstwa prezentacji
│         (Kontrolery, Program.cs)                │
└──────────────────────┬──────────────────────────┘
                       │ referencja
┌──────────────────────▼──────────────────────────┐
│             GameHub.Application                 │  ← Use cases, serwisy, DTOs
│      (IAuthService, IWalletService, DTOs)       │
└──────────────────────┬──────────────────────────┘
                       │ referencja
┌──────────────────────▼──────────────────────────┐
│               GameHub.Domain                    │  ← Encje, interfejsy
│    (User, Game, IRepository, IUnitOfWork)       │
└──────────────────────┬──────────────────────────┘
                       │ referencja
┌──────────────────────▼──────────────────────────┐
│             GameHub.SharedKernel                │  ← Wspólne typy
│                  (BaseEntity)                   │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│            GameHub.Infrastructure               │  ← Implementacje
│   (EfRepository, EfUnitOfWork, AppDbContext,    │
│    BcryptPasswordHasher, JwtTokenService,       │
│    DataSeeder)                                  │
└─────────────────────────────────────────────────┘
```

### Kluczowa zasada

`GameHub.Domain` nie zawiera **żadnych** referencji do `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*` ani żadnych zewnętrznych frameworków. Jest to gwarancja czystości architektury — domeny nie można „zainfekować" technologią.

### Struktura katalogów

```
GameHub.sln
└── src/
    ├── GameHub.SharedKernel/
    │   └── BaseEntity.cs
    ├── GameHub.Domain/
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── Game.cs
    │   │   ├── UserGame.cs
    │   │   ├── Review.cs
    │   │   ├── Friendship.cs
    │   │   ├── Report.cs
    │   │   └── Transaction.cs
    │   └── Interfaces/
    │       ├── IRepository.cs
    │       ├── IUnitOfWork.cs
    │       ├── IPasswordHasher.cs
    │       └── ITokenService.cs
    ├── GameHub.Application/
    │   ├── DTOs/
    │   │   ├── Auth/
    │   │   │   ├── RegisterRequest.cs
    │   │   │   ├── LoginRequest.cs
    │   │   │   ├── AuthResponse.cs
    │   │   │   ├── ProfileResponse.cs
    │   │   │   └── UpdateProfileRequest.cs
    │   │   └── Wallet/
    │   │       ├── TopUpRequest.cs
    │   │       └── TransactionDto.cs
    │   ├── Interfaces/
    │   │   ├── IAuthService.cs
    │   │   └── IWalletService.cs
    │   └── Services/
    │       ├── AuthService.cs
    │       └── WalletService.cs
    ├── GameHub.Infrastructure/
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs
    │   │   ├── AppDbContextFactory.cs
    │   │   ├── EfRepository.cs
    │   │   ├── EfUnitOfWork.cs
    │   │   ├── DataSeeder.cs
    │   │   └── Migrations/
    │   └── Security/
    │       ├── BcryptPasswordHasher.cs
    │       └── JwtTokenService.cs
    ├── GameHub.WebAPI/
    │   ├── Controllers/
    │   │   ├── AuthController.cs
    │   │   ├── WalletController.cs
    │   │   └── GamesController.cs
    │   ├── Program.cs
    │   └── appsettings.json
    ├── GameHub.BlazorServer/
    └── GameHub.BlazorWASM/
```

---

## 2. Warstwa domenowa — GameHub.Domain

### 2.1 BaseEntity (SharedKernel)

Wspólna klasa bazowa dla wszystkich encji. Zawiera wyłącznie klucz główny.

```csharp
public abstract class BaseEntity
{
    public int Id { get; protected set; }
}
```

`Id` ma setter `protected` — tylko EF Core (przez refleksję) może go ustawić. Kod aplikacyjny nie może ręcznie przypisywać Id.

### 2.2 Encja User

```csharp
public class User : BaseEntity
{
    public string Email        { get; set; }
    public string PasswordHash { get; set; }
    public string Username     { get; set; }
    public string? AvatarUrl  { get; set; }
    public string? Bio        { get; set; }
    public string Role        { get; set; } = "User";
    public bool IsBlocked     { get; set; } = false;
    public decimal WalletBalance { get; set; } = 0m;
    public DateTime CreatedAt { get; set; }

    // Nawigacje EF Core
    public ICollection<UserGame>    Library              { get; set; }
    public ICollection<Review>      Reviews              { get; set; }
    public ICollection<Transaction> Transactions         { get; set; }
    public ICollection<Friendship>  SentFriendships      { get; set; }
    public ICollection<Friendship>  ReceivedFriendships  { get; set; }
    public ICollection<Report>      FiledReports         { get; set; }
    public ICollection<Report>      ReportsAgainst       { get; set; }
}
```

**Decyzja projektowa:** `User` dziedziczy po `BaseEntity`, nie po `IdentityUser<int>`. Użycie `IdentityUser` wymagałoby referencji do `Microsoft.AspNetCore.Identity` w warstwie Domain, co łamie Clean Architecture. Zamiast tego hashowanie haseł obsługuje `IPasswordHasher` zdefiniowany jako interfejs domenowy, zaimplementowany w Infrastructure przez BCrypt.

### 2.3 Encja Transaction

```csharp
public enum TransactionType { Purchase, Refund, TopUp }

public class Transaction : BaseEntity
{
    public int UserId        { get; set; }
    public int? GameId       { get; set; }   // NULL dla operacji TopUp
    public decimal Amount    { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public User  User { get; set; }
    public Game? Game { get; set; }
}
```

`GameId` jest nullable — operacja doładowania portfela (TopUp) nie jest powiązana z żadną grą.

### 2.4 Interfejsy domenowe

#### IRepository\<T\>

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?>                GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>>  GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>>  FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?>                FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool>              ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<IReadOnlyList<T>>  FindWithIncludesAsync(Expression<Func<T, bool>> predicate,
                                CancellationToken ct = default,
                                params Expression<Func<T, object?>>[] includes);
    Task  AddAsync(T entity, CancellationToken ct = default);
    Task  AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void  Update(T entity);
    void  Remove(T entity);
    void  RemoveRange(IEnumerable<T> entities);
}
```

Predykaty są przekazywane jako `Expression<Func<T, bool>>`, nie jako `Func<T, bool>`. EF Core tłumaczy drzewa wyrażeń na klauzulę `WHERE` w SQL — filtrowanie odbywa się po stronie bazy, nie w pamięci C#.

#### IUnitOfWork

```csharp
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<User>        Users        { get; }
    IRepository<Game>        Games        { get; }
    IRepository<UserGame>    UserGames    { get; }
    IRepository<Review>      Reviews      { get; }
    IRepository<Friendship>  Friendships  { get; }
    IRepository<Report>      Reports      { get; }
    IRepository<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

#### IPasswordHasher

```csharp
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool   Verify(string plainPassword, string hash);
}
```

#### ITokenService

```csharp
public interface ITokenService
{
    string GenerateToken(User user);
}
```

---

## 3. Warstwa aplikacyjna — GameHub.Application

### 3.1 DTOs

#### Auth

| DTO | Pola | Kierunek |
|-----|------|----------|
| `RegisterRequest` | Email, Username, Password | Request → API |
| `LoginRequest` | Email, Password | Request → API |
| `AuthResponse` | Token, Username, Role | API → Client |
| `ProfileResponse` | Id, Email, Username, AvatarUrl, Bio, Role, WalletBalance, CreatedAt | API → Client |
| `UpdateProfileRequest` | Bio?, AvatarUrl? | Request → API |

#### Wallet

| DTO | Pola | Kierunek |
|-----|------|----------|
| `TopUpRequest` | Amount (1–10000) | Request → API |
| `TransactionDto` | Id, GameId?, GameTitle?, Amount, Type, CreatedAt | API → Client |

### 3.2 AuthService

Implementuje logikę rejestracji, logowania i zarządzania profilem. Korzysta z `IUnitOfWork`, `IPasswordHasher` i `ITokenService` — nigdy bezpośrednio z EF Core.

**Rejestracja:**
1. Sprawdź unikalność emaila — `ExistsAsync` (jeden `AnyAsync` po stronie DB)
2. Sprawdź unikalność nazwy użytkownika — `ExistsAsync`
3. Utwórz encję `User` z zahashowanym hasłem
4. Zapisz przez `AddAsync` + `SaveChangesAsync`
5. Zwróć `AuthResponse` z tokenem JWT

**Logowanie:**
1. Znajdź usera po emailu — `FirstOrDefaultAsync` (jeden `SELECT WHERE`)
2. Zweryfikuj hasło BCrypt
3. Sprawdź czy konto nie jest zablokowane
4. Zwróć `AuthResponse` z nowym tokenem JWT

### 3.3 WalletService

Implementuje operacje finansowe. Wszystkie operacje modyfikujące saldo używają jawnych transakcji bazodanowych.

**TopUp:**
1. Pobierz usera po Id
2. Rozpocznij transakcję DB (`BeginTransactionAsync`)
3. Zwiększ `WalletBalance`
4. Dodaj rekord `Transaction` typu `TopUp`
5. Zatwierdź (`CommitTransactionAsync`) lub wycofaj przy błędzie

**BuyGame:**
1. Pobierz usera i grę
2. Sprawdź widoczność gry (`IsVisible`)
3. Sprawdź czy user już posiada grę (`ExistsAsync`)
4. Sprawdź czy saldo jest wystarczające
5. W transakcji atomowo: odejmij saldo + dodaj `UserGame` + dodaj `Transaction` typu `Purchase`

---

## 4. Warstwa infrastruktury — GameHub.Infrastructure

### 4.1 AppDbContext

Dziedziczy po `DbContext` (nie po `IdentityDbContext` — nie używamy ASP.NET Identity). Cała konfiguracja tabel przez Fluent API w `OnModelCreating`.

Kluczowe konfiguracje:

```csharp
// Daty jako TEXT ISO 8601 (SQLite nie ma natywnego DATETIME)
e.Property(u => u.CreatedAt).HasConversion(
    v => v.ToString("o"),
    v => DateTime.Parse(v));

// Enumeracje jako string w bazie
e.Property(f => f.Status).HasConversion<string>();

// Unikalne indeksy
e.HasIndex(u => u.Email).IsUnique();
e.HasIndex(ug => new { ug.UserId, ug.GameId }).IsUnique();

// CHECK constraint na Rating
e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "Rating BETWEEN 1 AND 10"));

// Dwa FK do tej samej tabeli (Friendships)
e.HasOne(f => f.Requester).WithMany(u => u.SentFriendships)
    .HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Restrict);
```

### 4.2 EfRepository\<T\>

Generyczna implementacja — jeden plik obsługuje wszystkie encje. Nie ma osobnych klas `UserRepository`, `GameRepository` itp.

Kluczowe decyzje:
- `GetAllAsync` i `FindAsync` używają `AsNoTracking()` — EF nie śledzi zwróconych obiektów, co przyspiesza zapytania tylko do odczytu
- `FindWithIncludesAsync` umożliwia eager loading powiązanych encji bez naruszania abstrakcji repozytorium

```csharp
public async Task<IReadOnlyList<T>> FindWithIncludesAsync(
    Expression<Func<T, bool>> predicate,
    CancellationToken ct = default,
    params Expression<Func<T, object?>>[] includes)
{
    IQueryable<T> query = _set.AsNoTracking().Where(predicate);
    foreach (var include in includes)
        query = query.Include(include);
    return await query.ToListAsync(ct);
}
```

### 4.3 EfUnitOfWork

Implementuje wzorzec Unit of Work. Repozytoria tworzone są leniwie (lazy initialization przez operator `??=`). Zarządza jawną transakcją DB przechowywaną w polu `_currentTransaction`.

```csharp
public IRepository<User> Users => _users ??= new EfRepository<User>(_db);
```

`CommitTransactionAsync` wywołuje `SaveChangesAsync` przed `CommitAsync` — gwarantuje że wszystkie zmiany z change trackera są uwzględnione w transakcji.

### 4.4 BcryptPasswordHasher

```csharp
public string Hash(string plainPassword) =>
    BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

public bool Verify(string plainPassword, string hash) =>
    BCrypt.Net.BCrypt.Verify(plainPassword, hash);
```

`workFactor: 12` oznacza 2^12 = 4096 iteracji — dobry balans między bezpieczeństwem a wydajnością (ok. 250ms na hash).

### 4.5 JwtTokenService

Generuje token JWT z następującymi claimami:

| Claim | Wartość |
|-------|---------|
| `NameIdentifier` | `user.Id` |
| `Email` | `user.Email` |
| `Name` | `user.Username` |
| `Role` | `user.Role` |

Token podpisany algorytmem `HmacSha256`. Czas wygaśnięcia konfigurowalny przez `Jwt:ExpiresHours` (domyślnie 24h).

### 4.6 AppDbContextFactory

Używana wyłącznie przez narzędzie `dotnet ef` podczas generowania migracji — nie jest rejestrowana w kontenerze DI.

```csharp
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../GameHub.WebAPI"))
            .AddJsonFile("appsettings.json")
            .Build();
        // ...
    }
}
```

---

## 5. Warstwa API — GameHub.WebAPI

### 5.1 Program.cs — rejestracja serwisów

```
DbContext → AddDbContext<AppDbContext> (SQLite)
UnitOfWork → AddScoped<IUnitOfWork, EfUnitOfWork>
Security → AddScoped<IPasswordHasher, BcryptPasswordHasher>
           AddScoped<ITokenService, JwtTokenService>
Serwisy → AddScoped<IAuthService, AuthService>
          AddScoped<IWalletService, WalletService>
Auth → AddAuthentication(JwtBearer) + AddJwtBearer(...)
       AddAuthorization()
```

### 5.2 Pipeline middleware

```
UseHttpsRedirection
UseSerilogRequestLogging    ← loguje każdy request (metoda, ścieżka, status, czas)
UseAuthentication           ← MUSI być przed UseAuthorization
UseAuthorization
MapControllers
```

### 5.3 Uruchomienie — migracje i seed

```csharp
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await db.Database.MigrateAsync(); // tworzy/aktualizuje schemat bazy
    await DataSeeder.SeedAsync(db, hasher); // dane testowe (jeśli baza pusta)
}
```

`MigrateAsync` jest idempotentne — bezpieczne przy każdym restarcie.

### 5.4 Logowanie — Serilog

Dwa pliki logów rotowane dziennie:

| Plik | Zawartość |
|------|-----------|
| `logs/gamehub-YYYY-MM-DD.log` | Wszystkie logi (Information+) |
| `logs/errors-YYYY-MM-DD.log` | Tylko Warning/Error/Fatal |

---

## 6. Baza danych i migracje

### Schema ERD (uproszczony)

```
Users ──────< UserGames >────── Games
  │                               │
  ├──────< Reviews >──────────────┤
  │                               │
  ├──────< Transactions >─────────┤
  │                               │
  ├──────< Friendships (Requester)│
  ├──────< Friendships (Addressee)│
  │                               │
  ├──────< Reports (Reporter)     │
  ├──────< Reports (TargetUser)   │
                    Reports (TargetGame) >── Games
```

### Constraints

| Tabela | Constraint |
|--------|-----------|
| Users | UNIQUE(Email), UNIQUE(Username) |
| UserGames | UNIQUE(UserId, GameId) — jedna gra na usera |
| Reviews | UNIQUE(UserId, GameId) — jedna recenzja na grę |
| Reviews | CHECK(Rating BETWEEN 1 AND 10) |
| Friendships | UNIQUE(RequesterId, AddresseeId) |

### Soft delete

Zamiast fizycznego usuwania rekordów:

| Encja | Mechanizm |
|-------|-----------|
| Game | `IsVisible = false` |
| Review | `IsHidden = true` |
| User | `IsBlocked = true` |

### Komendy migracji

```bash
# Tworzenie nowej migracji
dotnet ef migrations add <NazwaMigracji> \
  --project src/GameHub.Infrastructure \
  --startup-project src/GameHub.WebAPI \
  --output-dir Persistence/Migrations

# Zastosowanie migracji
dotnet ef database update \
  --project src/GameHub.Infrastructure \
  --startup-project src/GameHub.WebAPI
```

---

## 7. Autoryzacja JWT

### Przepływ autoryzacji

```
Client                    WebAPI                    DB
  │                          │                       │
  ├─ POST /api/auth/login ──►│                       │
  │                          ├─ SELECT User WHERE ──►│
  │                          │◄─ User ───────────────┤
  │                          │                       │
  │                          │ BCrypt.Verify(password, hash)
  │                          │ JwtTokenService.GenerateToken(user)
  │                          │                       │
  │◄─ { token, username } ───┤                       │
  │                          │                       │
  ├─ GET /api/auth/profile ──►│                       │
  │  Authorization: Bearer {token}                   │
  │                          │ JwtBearer validates token
  │                          ├─ SELECT User WHERE ──►│
  │◄─ ProfileResponse ───────┤                       │
```

### Konfiguracja walidacji tokenu

```json
{
  "Jwt": {
    "Key": "min-32-znakowy-sekretny-klucz",
    "Issuer": "GameHub",
    "Audience": "GameHubUsers",
    "ExpiresHours": "24"
  }
}
```

Walidowane parametry: Issuer, Audience, czas życia, podpis klucza symetrycznego.

### Wyodrębnianie userId z tokenu

```csharp
var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

---

## 8. System portfela

### Model danych

Saldo przechowywane jest w kolumnie `WalletBalance` tabeli `Users` (Opcja A z ERD). Historia operacji w tabeli `Transactions`.

### Atomowość operacji

Wszystkie operacje modyfikujące saldo wykonywane są w jawnej transakcji bazodanowej:

```
BeginTransactionAsync()
  ├── UPDATE Users SET WalletBalance = ...
  ├── INSERT INTO Transactions (...)
CommitTransactionAsync()   ← SaveChanges() + Commit w jednym kroku
  └── (w razie błędu) RollbackTransactionAsync()
```

### Typy transakcji

| Type | Opis |
|------|------|
| `TopUp` | Doładowanie portfela — `GameId = NULL` |
| `Purchase` | Zakup gry — `GameId` wskazuje kupioną grę |
| `Refund` | Zwrot (zarezerwowany na przyszłość) |

---

## 9. Dane seedowe

`DataSeeder.SeedAsync` wykonuje się tylko gdy baza jest pusta (`Users.AnyAsync() == false`).

### Konta testowe

| Username | Email | Hasło | Rola | Saldo |
|----------|-------|-------|------|-------|
| admin | admin@gamehub.com | Admin123! | Admin | 0 |
| alice | alice@gamehub.com | Alice123! | User | 150 |
| bob | bob@gamehub.com | Bob123! | User | 80 |

### Gry seedowe

| Tytuł | Gatunek | Cena | Widoczna |
|-------|---------|------|----------|
| Cyber Odyssey | RPG | 59.99 | ✅ |
| Shadow Tactics | Strategy | 39.99 | ✅ |
| Galactic Frontier | Shooter | 49.99 | ✅ |
| Dungeon Crawler X | RPG | 29.99 | ✅ |
| Racing Legends | Racing | 44.99 | ✅ |
| Beta Test Game | Action | 9.99 | ❌ (ukryta) |

### Stan bibliotek

| User | Gry w bibliotece | Wydane łącznie |
|------|-----------------|----------------|
| alice | Cyber Odyssey, Shadow Tactics | 99.98 |
| bob | Galactic Frontier | 49.99 |

---

## 10. Konfiguracja i uruchomienie

### Wymagania

- .NET 8 SDK
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### Paczki NuGet (kluczowe)

| Projekt | Paczka |
|---------|--------|
| Infrastructure | `Microsoft.EntityFrameworkCore.Sqlite` |
| Infrastructure | `Microsoft.EntityFrameworkCore.Tools` |
| Infrastructure | `BCrypt.Net-Next` |
| Infrastructure | `System.IdentityModel.Tokens.Jwt` |
| WebAPI | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| WebAPI | `Microsoft.EntityFrameworkCore.Design` |
| WebAPI | `Serilog.AspNetCore` |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamehub.db"
  },
  "Jwt": {
    "Key": "twoj-super-tajny-klucz-min-32-znaki-!!",
    "Issuer": "GameHub",
    "Audience": "GameHubUsers",
    "ExpiresHours": "24"
  }
}
```

### Uruchomienie

```bash
# Budowanie
dotnet build

# Uruchomienie WebAPI (migracje i seed wykonują się automatycznie)
dotnet run --project src/GameHub.WebAPI

# Swagger UI
https://localhost:7024/swagger
http://localhost:5224/swagger
```

---

## 11. Endpointy API

### Autoryzacja

| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/auth/register` | ❌ | Rejestracja nowego konta |
| POST | `/api/auth/login` | ❌ | Logowanie, zwraca token JWT |
| GET | `/api/auth/profile` | ✅ JWT | Profil zalogowanego usera |
| PUT | `/api/auth/profile` | ✅ JWT | Edycja bio i avatarUrl |

#### POST /api/auth/register

```json
// Request
{
  "email": "jan@example.com",
  "username": "janek",
  "password": "haslo123"
}

// Response 200
{
  "token": "eyJhbGci...",
  "username": "janek",
  "role": "User"
}

// Response 409 — email lub username zajęty
{ "error": "Email already in use." }
```

#### POST /api/auth/login

```json
// Request
{
  "email": "jan@example.com",
  "password": "haslo123"
}

// Response 200
{
  "token": "eyJhbGci...",
  "username": "janek",
  "role": "User"
}

// Response 401
{ "error": "Invalid credentials." }
```

#### GET /api/auth/profile

```json
// Response 200
{
  "id": 1,
  "email": "jan@example.com",
  "username": "janek",
  "avatarUrl": null,
  "bio": null,
  "role": "User",
  "walletBalance": 150.00,
  "createdAt": "2026-06-03T17:00:00Z"
}
```

#### PUT /api/auth/profile

```json
// Request
{
  "bio": "Zapalony gracz RPG",
  "avatarUrl": "https://example.com/avatar.png"
}

// Response 204 No Content
```

### Portfel

| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/wallet/topup` | ✅ JWT | Doładowanie portfela |
| GET | `/api/wallet/history` | ✅ JWT | Historia transakcji |
| POST | `/api/games/{id}/buy` | ✅ JWT | Zakup gry z portfela |

#### POST /api/wallet/topup

```json
// Request
{ "amount": 100.00 }

// Response 200
{ "balance": 250.00 }

// Response 400 — amount poza zakresem 1-10000
```

#### GET /api/wallet/history

```json
// Response 200
[
  {
    "id": 3,
    "gameId": 1,
    "gameTitle": "Cyber Odyssey",
    "amount": 59.99,
    "type": "Purchase",
    "createdAt": "2026-05-24T10:00:00Z"
  },
  {
    "id": 1,
    "gameId": null,
    "gameTitle": null,
    "amount": 200.00,
    "type": "TopUp",
    "createdAt": "2026-05-19T10:00:00Z"
  }
]
```

#### POST /api/games/{id}/buy

```json
// Response 200
{
  "message": "Game purchased successfully."
}

// Response 400 — niewystarczające saldo
{ "error": "Insufficient wallet balance." }

// Response 409 — gra już w bibliotece
{ "error": "You already own this game." }

// Response 404 — gra nie istnieje lub ukryta
{ "error": "Game not found." }
```

---

*Dokumentacja wygenerowana na podstawie implementacji z sesji deweloperskiej — Czerwiec 2026.*