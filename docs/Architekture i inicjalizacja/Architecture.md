# GameHub — Architektura i inicjalizacja

## Struktura solution

```         
GameHub.sln
└── src/
    ├── GameHub.SharedKernel/      ← BaseEntity, wspólne typy
    ├── GameHub.Domain/            ← Encje, interfejsy (ZERO EF Core)
    ├── GameHub.Application/       ← Use cases, DTOs, walidacja
    ├── GameHub.Infrastructure/    ← EF Core, repozytoria, DbContext
    ├── GameHub.WebAPI/            ← REST API + Serilog
    ├── GameHub.BlazorServer/      ← Blazor SSR + Serilog
    └── GameHub.BlazorWASM/        ← Blazor WASM (client-only)
```

## Graf zależności

```         
SharedKernel
    └── Domain          (ref: SharedKernel)
          └── Application     (ref: Domain)
                ├── Infrastructure    (ref: Application)
                ├── WebAPI            (ref: Application + Infrastructure)
                └── BlazorServer      (ref: Application + Infrastructure)

BlazorWASM              (ref: SharedKernel)   ← komunikuje się z WebAPI przez HTTP
```

## Komendy CLI — uruchom raz po klonowaniu

``` bash
# 0. Upewnij się, że masz .NET 8 SDK
dotnet --version   # wymagane: 8.x.x

# 1. Uruchom skrypt inicjalizacji
chmod +x init-solution.sh
./init-solution.sh

# 2. Zbuduj całość
dotnet build

# 3. Uruchom WebAPI
dotnet run --project src/GameHub.WebAPI
```

## Pliki do wklejenia po inicjalizacji

| Plik | Docelowa lokalizacja |
|----|----|
| `BaseEntity.cs` | `src/GameHub.SharedKernel/` |
| `User.cs` | `src/GameHub.Domain/Entities/` |
| `Game.cs` | `src/GameHub.Domain/Entities/` |
| `UserGame.cs` | `src/GameHub.Domain/Entities/` |
| `Review.cs` | `src/GameHub.Domain/Entities/` |
| `Friendship.cs` | `src/GameHub.Domain/Entities/` |
| `Report.cs` | `src/GameHub.Domain/Entities/` |
| `Transaction.cs` | `src/GameHub.Domain/Entities/` |
| `IRepository.cs` | `src/GameHub.Domain/Interfaces/` |
| `IUnitOfWork.cs` | `src/GameHub.Domain/Interfaces/` |
| `Program.cs` (WebAPI) | `src/GameHub.WebAPI/` (zastąp wygenerowany) |
| `appsettings.json` | `src/GameHub.WebAPI/` (zastąp wygenerowany) |
| `Program.cs` (BlazorServer) | `src/GameHub.BlazorServer/` (zastąp wygenerowany) |

## Następne kroki (Infrastructure)

``` csharp
// 1. Dodaj AppDbContext w GameHub.Infrastructure
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    // ...
}

// 2. Zaimplementuj EfRepository<T> : IRepository<T>
// 3. Zaimplementuj EfUnitOfWork : IUnitOfWork

// 4. Dodaj migrację EF Core
dotnet ef migrations add InitialCreate --project src/GameHub.Infrastructure \
    --startup-project src/GameHub.WebAPI

dotnet ef database update --project src/GameHub.Infrastructure \
    --startup-project src/GameHub.WebAPI
```

## Logi — gdzie szukać

```         
src/GameHub.WebAPI/logs/
    gamehub-YYYY-MM-DD.log       ← wszystkie logi (Info+)
    errors-YYYY-MM-DD.log        ← tylko Warning/Error/Fatal

src/GameHub.BlazorServer/logs/
    blazorserver-YYYY-MM-DD.log
    blazorserver-errors-YYYY-MM-DD.log
```
