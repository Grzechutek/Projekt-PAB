#!/usr/bin/env bash
# ============================================================
#  GameHub — inicjalizacja solution
#  Uruchom: chmod +x init-solution.sh && ./init-solution.sh
# ============================================================

set -e

echo ">> Tworzenie solution GameHub..."
dotnet new sln -n GameHub --force

# ── Projekty ────────────────────────────────────────────────
dotnet new classlib  -n GameHub.SharedKernel  -o src/GameHub.SharedKernel  -f net8.0
dotnet new classlib  -n GameHub.Domain        -o src/GameHub.Domain        -f net8.0
dotnet new classlib  -n GameHub.Application   -o src/GameHub.Application   -f net8.0
dotnet new classlib  -n GameHub.Infrastructure -o src/GameHub.Infrastructure -f net8.0
dotnet new webapi    -n GameHub.WebAPI        -o src/GameHub.WebAPI        -f net8.0
dotnet new blazor    -n GameHub.BlazorServer -o src/GameHub.BlazorServer -f net8.0
dotnet new blazorwasm -n GameHub.BlazorWASM  -o src/GameHub.BlazorWASM  -f net8.0 --empty

# ── Dodaj do sln ────────────────────────────────────────────
dotnet sln add src/GameHub.SharedKernel/GameHub.SharedKernel.csproj
dotnet sln add src/GameHub.Domain/GameHub.Domain.csproj
dotnet sln add src/GameHub.Application/GameHub.Application.csproj
dotnet sln add src/GameHub.Infrastructure/GameHub.Infrastructure.csproj
dotnet sln add src/GameHub.WebAPI/GameHub.WebAPI.csproj
dotnet sln add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj
dotnet sln add src/GameHub.BlazorWASM/GameHub.BlazorWASM.csproj

# ── Referencje między projektami ────────────────────────────
dotnet add src/GameHub.Domain/GameHub.Domain.csproj \
       reference src/GameHub.SharedKernel/GameHub.SharedKernel.csproj

dotnet add src/GameHub.Application/GameHub.Application.csproj \
       reference src/GameHub.Domain/GameHub.Domain.csproj

dotnet add src/GameHub.Infrastructure/GameHub.Infrastructure.csproj \
       reference src/GameHub.Application/GameHub.Application.csproj

dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj \
       reference src/GameHub.Application/GameHub.Application.csproj
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj \
       reference src/GameHub.Infrastructure/GameHub.Infrastructure.csproj

dotnet add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj \
       reference src/GameHub.Application/GameHub.Application.csproj
dotnet add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj \
       reference src/GameHub.Infrastructure/GameHub.Infrastructure.csproj

# BlazorWASM zazwyczaj komunikuje się przez HTTP (nie bezpośrednio z Application),
# ale dodajemy SharedKernel dla wspólnych DTOs/enumów:
dotnet add src/GameHub.BlazorWASM/GameHub.BlazorWASM.csproj \
       reference src/GameHub.SharedKernel/GameHub.SharedKernel.csproj

# ── NuGet: Infrastructure ────────────────────────────────────
dotnet add src/GameHub.Infrastructure/GameHub.Infrastructure.csproj \
       package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.* 
dotnet add src/GameHub.Infrastructure/GameHub.Infrastructure.csproj \
       package Microsoft.EntityFrameworkCore.Design --version 8.0.*

# ── NuGet: WebAPI ────────────────────────────────────────────
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj package Serilog.AspNetCore
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj package Serilog.Sinks.File
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj package Serilog.Sinks.Console
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj package Serilog.Enrichers.Environment
dotnet add src/GameHub.WebAPI/GameHub.WebAPI.csproj package Serilog.Enrichers.Thread

# ── NuGet: BlazorServer ──────────────────────────────────────
dotnet add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj package Serilog.AspNetCore
dotnet add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj package Serilog.Sinks.File
dotnet add src/GameHub.BlazorServer/GameHub.BlazorServer.csproj package Serilog.Sinks.Console

echo ""
echo "========================================="
echo "  Solution gotowy! Weryfikacja:"
dotnet sln list
echo "========================================="
echo ""
echo "Następny krok: skopiuj pliki z katalogu src/ do odpowiednich projektów."
