# GameHub — Plan projektu zaliczeniowego

> **Zespół:** 4 osoby · **Czas:** 2 tygodnie · **Stack:** C# / Blazor / SQLite

---

## Spis treści

1. [Architektura solution](#1-architektura-solution)
2. [Podział ról](#2-podział-ról)
3. [Harmonogram](#3-harmonogram)
4. [Zakres funkcjonalności (MoSCoW)](#4-zakres-funkcjonalności-moscow)
5. [Wymagania prowadzącego — checklist](#5-wymagania-prowadzącego--checklist)
6. [Stack technologiczny](#6-stack-technologiczny)
7. [Konwencje i decyzje architektoniczne](#7-konwencje-i-decyzje-architektoniczne)
8. [Prompty AI — startowe sesje](#8-prompty-ai--startowe-sesje)

---

## 1. Architektura solution

```
GameHub.sln
├── GameHub.SharedKernel       # Typy wspólne, BaseEntity, wyjątki
├── GameHub.Domain             # Encje, interfejsy IRepository / IUnitOfWork
├── GameHub.Application        # Serwisy aplikacyjne, DTOs, logika biznesowa
├── GameHub.Infrastructure     # EF Core, DbContext, repozytoria, SQLite
├── GameHub.WebAPI             # Kontrolery CRUD + logika systemu, Serilog
├── GameHub.BlazorServer       # Panel admina (prosty, CLI-style), Serilog
└── GameHub.BlazorWASM         # Główna aplikacja użytkownika
```

### Zależności między projektami

```
WebAPI          → Application → Domain ← SharedKernel
BlazorServer    ↗             ↑
BlazorWASM      → (tylko API przez HttpClient)
Infrastructure  → Application → Domain
```

**Zasada:** Domain nie zna EF Core. Infrastructure nie zna kontrolerów. WASM komunikuje się wyłącznie przez HTTP.

---

## 2. Podział ról

### Dev A — Backend Lead

**Odpowiedzialność:** fundament projektu, autoryzacja, portfel

| Zadanie | Warstwa |
|---|---|
| Inicjalizacja solution (7 projektów, GitFlow) | wszystkie |
| Encje domenowe + interfejsy `IRepository<T>`, `IUnitOfWork` | Domain |
| Konfiguracja Serilog (daily rolling + osobny plik błędów) | WebAPI, BlazorServer |
| ASP.NET Identity — rejestracja, login, JWT, role | WebAPI |
| Endpointy portfela: doładowanie, historia transakcji | WebAPI |
| Zakup gry (walidacja salda, atomowa transakcja EF) | WebAPI |
| Integracja końcowa, `readme.txt`, pakowanie | — |

### Dev B — Backend Features

**Odpowiedzialność:** baza danych, repozytoria, CRUD, znajomi, moderacja

| Zadanie | Warstwa |
|---|---|
| SQLite + EF Core — DbContext, migracje, seed data | Infrastructure |
| `GenericRepository<T>` + konkretne repozytoria | Infrastructure |
| `UnitOfWork` implementacja | Infrastructure |
| CRUD Games (z filtrowaniem/sortowaniem) | WebAPI |
| CRUD Reviews + CRUD UserGames (biblioteka) | WebAPI |
| System znajomych — Friendships endpoints | WebAPI |
| Reports CRUD + zmiana statusu | WebAPI |
| Moderacja recenzji (PATCH IsHidden) | WebAPI |

### Dev C — Frontend Admin → WASM (znajomi)

**Odpowiedzialność:** BlazorServer panel admina, potem moduł znajomych w WASM

| Zadanie | Projekt | Czas |
|---|---|---|
| BlazorServer — layout, routing, AuthState | BlazorServer | Dzień 4–5 |
| Strona lista użytkowników + blokowanie (MudBlazor DataGrid) | BlazorServer | Dzień 5–7 |
| Strona lista gier + formularz dodaj/edytuj (Radzen Form) | BlazorServer | Dzień 5–7 |
| Strona moderacja recenzji (ukryj/pokaż) | BlazorServer | Dzień 6–7 |
| Strona zgłoszenia — zmiana statusu | BlazorServer | Dzień 7 |
| Strona `/friends` — karty zaproszeń, lista znajomych | BlazorWASM | Dzień 9–11 |
| Statyczne obrazy (logo, placeholder okładek) | oba | Dzień 11 |

### Dev D — Frontend User App

**Odpowiedzialność:** główna aplikacja BlazorWASM dla użytkowników

| Zadanie | Projekt | Czas |
|---|---|---|
| Setup: HttpClient, `CustomAuthStateProvider`, JWT | BlazorWASM | Dzień 4–5 |
| Login / Register — strony z walidacją | BlazorWASM | Dzień 4–5 |
| `/games` — katalog z filtrowaniem, sortowaniem, wyszukiwaniem | BlazorWASM | Dzień 6–8 |
| `/games/{id}` — szczegóły, okładka, sekcja recenzji | BlazorWASM | Dzień 7–9 |
| Formularz recenzji (Rating 1–10, Content, walidacja) | BlazorWASM | Dzień 8–9 |
| `/profile` — edycja bio i avatara | BlazorWASM | Dzień 8–10 |
| `/library` — biblioteka gier użytkownika | BlazorWASM | Dzień 9–10 |
| `/wallet` — saldo, doładowanie, historia transakcji | BlazorWASM | Dzień 10 |

---

## 3. Harmonogram

### Faza 1 — Fundament (Dzień 1–3) · wszyscy backend

| Dzień | Zadanie | Kto |
|---|---|---|
| 1 | Inicjalizacja 7 projektów w solution, GitHub repo, gitignore | Dev A |
| 1–2 | Encje domenowe + `IRepository<T>` + `IUnitOfWork` w Domain | Dev A |
| 2 | SQLite + EF Core + DbContext + migracje + seed data | Dev B |
| 2–3 | `GenericRepository` + konkretne repozytoria + UoW | Dev B |
| 3 | Serilog — daily rolling log + osobny plik `errors-*.log` | Dev A |

> **Blokada:** Dev C i Dev D nie mogą zacząć pisać serwisów przed tym, jak Dev A wypchnie encje na repo. Ustalcie nazwy pól zanim każdy zacznie generować kod.

### Faza 2 — Backend API (Dzień 3–8)

| Dzień | Zadanie | Kto |
|---|---|---|
| 3–5 | Auth: `/api/auth/register`, `/login`, `/profile` + JWT | Dev A |
| 4–6 | `GamesController` — CRUD + filtrowanie/sortowanie | Dev B |
| 4–6 | `ReviewsController` — CRUD + limit 1 recenzji na grę | Dev B |
| 5–6 | `UserGamesController` — biblioteka użytkownika | Dev B |
| 6–7 | Portfel: `/api/wallet/topup`, zakup gry (transakcja atomowa) | Dev A |
| 7–8 | `FriendshipsController` — invite/accept/reject/list | Dev B |
| 7–8 | `ReportsController` CRUD + `AdminController` (blokowanie userów) | Dev B |

### Faza 3 — Frontend (Dzień 4–12)

| Dzień | Zadanie | Kto |
|---|---|---|
| 4–5 | BlazorServer — skeleton, layout, MudBlazor setup | Dev C |
| 4–5 | BlazorWASM — skeleton, HttpClient, AuthStateProvider | Dev D |
| 5–7 | BlazorServer — strony admina (users, games, reviews, reports) | Dev C |
| 6–9 | BlazorWASM — `/games` katalog + `/games/{id}` szczegóły | Dev D |
| 8–10 | BlazorWASM — `/profile`, `/library`, `/wallet` | Dev D |
| 9–11 | BlazorWASM — `/friends` (Dev C przechodzi tu po zakończeniu Servera) | Dev C |
| 11 | Obrazy statyczne — okładki, avatary, logo portalu | Dev C + D |

### Faza 4 — Spinanie i dokumentacja (Dzień 12–14)

| Dzień | Zadanie | Kto |
|---|---|---|
| 12 | Integracja end-to-end — testy manualne wszystkich flow | Dev A + B |
| 12 | Naprawa błędów 500, walidacja tokenów, weryfikacja seeda | Dev A + B |
| 12–13 | Dokumentacja użytkownika — zrzuty ekranu + opis funkcjonalności | Dev C + D |
| 13 | `readme.txt` — skład grupy, instrukcja uruchomienia, konta testowe | Dev A |
| 14 | ZIP całego solution + baza SQLite z seedem, upload, link | Dev A + B |

---

## 4. Zakres funkcjonalności (MoSCoW)

### MUST HAVE — realizujemy na pewno

- Rejestracja / logowanie / profil użytkownika (ASP.NET Identity)
- Katalog gier + strona szczegółów gry
- Biblioteka — dodawanie gier do konta, symulacja zakupu
- Oceny i recenzje (dodaj / edytuj / usuń)
- Panel admina — zarządzanie użytkownikami i grami (BlazorServer)

### SHOULD HAVE — realizujemy w planie

- Sklep z portfelem i historią zakupów / transakcji
- Filtrowanie i sortowanie gier
- System znajomych (zaproś / akceptuj / lista)
- Moderacja recenzji przez admina

### COULD HAVE — odpada przy braku czasu

- Zgłoszenia użytkowników (Reports) + obsługa przez admina
- Promocje i rabaty na gry
- Forum / wątki dyskusyjne
- Chat w czasie rzeczywistym (SignalR)

### WON'T HAVE — odpada całkowicie

- Integracja Steam API
- System rekomendacji k-NN
- Dashboard ze statystykami
- Monitoring czatu przez admina

---

## 5. Wymagania prowadzącego — checklist

| Wymaganie | Realizacja | Status |
|---|---|---|
| Baza SQLite | `Microsoft.Data.Sqlite` + EF Core | ✅ |
| Co najmniej 5 tabel | Mamy 7: Users, Games, UserGames, Reviews, Friendships, Reports, Transactions | ✅ |
| Czysta architektura — 7 oddzielnych projektów | SharedKernel, Domain, Application, Infrastructure, WebAPI, BlazorServer, BlazorWASM | ✅ |
| WebAPI — CRUD dla wszystkich danych | Kontrolery: Games, Reviews, Users, Friendships, Reports, Transactions | ✅ |
| Repozytoria + jednostki pracy | `GenericRepository<T>` + `UnitOfWork` w Infrastructure | ✅ |
| Model danych w warstwie Domain | Encje + `IRepository` + `IUnitOfWork` | ✅ |
| Logger — nowy plik każdego dnia | Serilog `rollingInterval: Day` | ✅ |
| Logger — błędy w osobnym pliku | Serilog sink z filtrem `MinimumLevel: Error` | ✅ |
| Minimum 2 strony w BlazorServer (lista + detail) | Lista użytkowników, lista gier, lista recenzji, lista zgłoszeń | ✅ |
| Minimum 2 strony w BlazorWASM (lista + detail) | `/games` lista, `/games/{id}` szczegóły, `/library`, `/profile`, `/wallet`, `/friends` | ✅ |
| Min. 2 komponenty z zewnętrznych bibliotek w każdym UI | MudBlazor (DataGrid, Dialog, Snackbar) w BlazorServer; Radzen (Form, DataGrid) lub MudBlazor w WASM | ✅ |
| Obrazy (.jpg, .png) w UI | Okładki gier, avatary użytkowników, logo portalu | ✅ |
| Walidacja wszystkich formularzy | DataAnnotations + `EditForm` w Blazor | ✅ |
| `readme.txt` ze składem grupy i instrukcją | Przygotować w Dniu 13 | ⏳ |
| Dokumentacja użytkownika z zrzutami ekranu | Przygotować w Dniu 12–13 | ⏳ |
| Projekt spakowany na zewnętrznym serwerze | Upload w Dniu 14 | ⏳ |

---

## 6. Stack technologiczny

### Backend

| Technologia | Wersja | Zastosowanie |
|---|---|---|
| .NET | 8 | Runtime |
| ASP.NET Core Web API | 8 | REST API |
| Entity Framework Core | 8 | ORM |
| Microsoft.Data.Sqlite | — | Sterownik SQLite |
| ASP.NET Identity | — | Autoryzacja + role |
| Microsoft.AspNetCore.Authentication.JwtBearer | — | JWT tokeny |
| Serilog | latest | Logowanie (WebAPI + BlazorServer) |
| Serilog.Sinks.File | latest | Daily rolling + errors |

### Frontend

| Technologia | Zastosowanie |
|---|---|
| Blazor Server (.NET 8) | Panel admina |
| Blazor WebAssembly (.NET 8) | Główna aplikacja użytkownika |
| MudBlazor | Komponenty UI (DataGrid, Dialog, Snackbar, Form) |
| Radzen (opcjonalnie) | Drugi zestaw komponentów — DataGrid w BlazorServer |
| DataAnnotations | Walidacja formularzy |

### Decyzja: biblioteki komponentów

Opcja rekomendowana: **MudBlazor w BlazorWASM + Radzen w BlazorServer**.
Spełnia automatycznie wymaganie "min. 2 zewnętrzne biblioteki" i różnicuje oba projekty UI.

---

## 7. Konwencje i decyzje architektoniczne

### Portfel — Opcja A (WalletBalance w tabeli Users)

Wybieramy prostszą opcję A — pole `WalletBalance` bezpośrednio w tabeli `Users`.
Tabela `Transactions` loguje każdy ruch (TopUp / Purchase / Refund).
Tabela `WalletTransactions` (Opcja B) odpada.

### Reports — logika walidacji

Pole `TargetUserId` lub `TargetGameId` — dokładnie jedno musi być wypełnione.
Walidacja na poziomie serwisu aplikacyjnego (nie bazy), przy tworzeniu raportu.

### Nazwy kont testowych (seed)

| Email | Hasło | Rola |
|---|---|---|
| `admin@gamehub.com` | `Admin123!` | Admin |
| `user1@gamehub.com` | `User123!` | User |
| `user2@gamehub.com` | `User123!` | User |

### Konwencje nazewnictwa

- Encje: `PascalCase`, angielskie nazwy zgodne ze schematem DB
- Endpointy API: `/api/{resource}` — liczba mnoga, kebab-case
- Pliki Razor: `PascalCase.razor`
- Serwisy: sufiks `Service` (np. `GameService.cs`)
- Repozytoria: sufiks `Repository` (np. `GameRepository.cs`)

### Logowanie — konwencja komunikatów

```
[INFO]  GET /api/games — zwrócono 20 wyników
[INFO]  User user1@gamehub.com zalogowany pomyślnie
[WARN]  Próba zakupu gry {id} przy niewystarczającym saldzie — user {userId}
[ERROR] Nieoczekiwany błąd w GamesController.GetById: {message}
```

---

## 8. Prompty AI — startowe sesje

> Wklej schemat bazy na końcu każdego promptu przed wysłaniem.
> Każdy prompt to punkt startowy **jednej sesji** — nie łącz kilku tematów w jedną sesję.

---

### Dev A — Sesja 1: Solution setup + Domain + Serilog

```
Jesteś doświadczonym architektem .NET. Pomóż mi zainicjalizować solution dla projektu zaliczeniowego.

WYMAGANIA ARCHITEKTURY (obowiązkowe wg prowadzącego):
- 7 oddzielnych projektów w jednym .sln:
  1. GameHub.SharedKernel (Class Library)
  2. GameHub.Domain (Class Library)
  3. GameHub.Application (Class Library)
  4. GameHub.Infrastructure (Class Library)
  5. GameHub.WebAPI (ASP.NET Core Web API)
  6. GameHub.BlazorServer (Blazor Server)
  7. GameHub.BlazorWASM (Blazor WebAssembly)

WARSTWA DOMAIN powinna zawierać:
- Encje: User, Game, UserGame, Review, Friendship, Report, Transaction
- Interfejsy: IRepository<T>, IUnitOfWork
- Brak referencji do EF Core (czysta domena)

LOGOWANIE (Serilog) w WebAPI i BlazorServer:
- Nowy plik logu każdego dnia (rolling daily)
- Błędy w osobnym pliku (np. errors-YYYY-MM-DD.log)

Wygeneruj:
1. Komendy dotnet CLI do stworzenia solution i projektów
2. Kompletne encje domenowe (pola zgodne ze schematem SQLite poniżej)
3. Interfejsy IRepository<T> i IUnitOfWork
4. Konfigurację Serilog w Program.cs WebAPI

SCHEMAT BAZY (SQLite):
[wklej tutaj treść tabel z dokumentacji projektu]
```

---

### Dev A — Sesja 2: ASP.NET Identity + JWT + portfel

```
Pracuję nad projektem GameHub w ASP.NET Core 8 z SQLite. Mam już:
- Encje domenowe (User, Game, etc.) w GameHub.Domain
- DbContext z EF Core w GameHub.Infrastructure
- Solution z 7 projektami

Teraz potrzebuję zaimplementować AUTORYZACJĘ:

1. ASP.NET Identity zintegrowane z istniejącym DbContext i tabelą Users
   - Bez Identity własnych tabel — mapujemy na nasz model User
   - Role: "User" i "Admin"

2. JWT Bearer authentication:
   - POST /api/auth/register (walidacja, hash hasła, zapis)
   - POST /api/auth/login (zwróć JWT token)
   - GET  /api/auth/profile (wymagany JWT)
   - PUT  /api/auth/profile (edycja bio, avatarUrl)

3. PORTFEL:
   - POST /api/wallet/topup  — body: { amount: decimal }
   - GET  /api/wallet/history — lista Transactions zalogowanego usera
   - POST /api/games/{id}/buy — sprawdź saldo, odejmij, utwórz UserGame i Transaction atomowo

Wygeneruj kompletne kontrolery, konfigurację Program.cs i serwisy.
```

---

### Dev B — Sesja 1: EF Core + SQLite + seed + repozytoria

```
Pracuję nad GameHub — portalem społecznościowym z grami komputerowymi.
Stack: C#, ASP.NET Core 8, EF Core, SQLite.

Potrzebuję zaimplementować WARSTWĘ INFRASTRUKTURY:

1. DbContext (GameHubDbContext):
   - DbSet dla każdej tabeli: Users, Games, UserGames, Reviews, Friendships, Reports, Transactions
   - Fluent API: klucze, unique constraints, foreign keys
   - SQLite provider

2. GenericRepository<T> implementujący IRepository<T>:
   GetByIdAsync, GetAllAsync, FindAsync (Expression), AddAsync, Update, Delete, SaveAsync

3. Konkretne repozytoria:
   - GameRepository z GetWithFilters(genre, sortBy, searchQuery)
   - ReviewRepository z GetByGameId, GetByUserId
   - FriendshipRepository z GetFriendsList, GetPendingRequests

4. UnitOfWork implementujący IUnitOfWork

5. Dane seedowe:
   - 3 konta: admin@gamehub.com (Admin), user1@gamehub.com, user2@gamehub.com
   - 20 gier z gatunków: Action, RPG, Strategy, Indie — z ceną i opisem
   - Przykładowe recenzje i biblioteki

Interfejsy IRepository i IUnitOfWork mam już w GameHub.Domain.
```

---

### Dev B — Sesja 2: CRUD kontrolery (Games, Reviews, Znajomi, Moderacja)

```
Kontynuuję GameHub. Mam już: DbContext, repozytoria, UnitOfWork, autoryzację JWT.

Potrzebuję 4 kontrolery WebAPI:

1. GamesController:
   - GET    /api/games?genre=&sort=&search=
   - GET    /api/games/{id}  (+ średnia ocena)
   - POST   /api/games       [Admin]
   - PUT    /api/games/{id}  [Admin]
   - DELETE /api/games/{id}  [Admin] — ustawia IsVisible=false

2. ReviewsController:
   - GET    /api/games/{gameId}/reviews
   - POST   /api/games/{gameId}/reviews  [Authorize, max 1 na grę]
   - PUT    /api/reviews/{id}            [właściciel]
   - DELETE /api/reviews/{id}            [właściciel lub Admin]
   - PATCH  /api/reviews/{id}/hide       [Admin] — toggle IsHidden

3. FriendshipsController:
   - GET    /api/friends          — lista zaakceptowanych
   - GET    /api/friends/requests — oczekujące
   - POST   /api/friends/invite/{userId}
   - PUT    /api/friends/{id}/accept
   - DELETE /api/friends/{id}

4. ReportsController:
   - POST   /api/reports                  — zgłoś usera lub grę
   - GET    /api/reports         [Admin]  — lista
   - PATCH  /api/reports/{id}/status [Admin]

Dla każdego kontrolera: kompletny kod, walidacja, HTTP status codes, obsługa błędów.
```

---

### Dev C — Sesja 1: BlazorServer panel admina (MudBlazor)

```
Pracuję nad panelem administracyjnym w Blazor Server dla GameHub.

Panel admina ma być PROSTY I FUNKCJONALNY (CLI-style).
Użyj MudBlazor jako biblioteki komponentów.

Potrzebuję 4 strony (.razor):

1. /admin/users — lista użytkowników
   - MudDataGrid: Id, Username, Email, Role, IsBlocked, CreatedAt
   - Akcje w wierszu: Zablokuj/Odblokuj, Zmień rolę

2. /admin/games — lista gier
   - MudDataGrid: Id, Title, Genre, Price, IsVisible
   - Przycisk "Dodaj grę" → MudDialog z formularzem (walidacja obowiązkowa)
   - Edycja inline i ukrywanie gry

3. /admin/reviews — moderacja
   - Tabela z kolumną IsHidden
   - Przycisk Ukryj/Pokaż

4. /admin/reports — zgłoszenia
   - Tabela Reports z filtrem po statusie
   - Dropdown do zmiany statusu (Open / InProgress / Closed)

Komunikacja z WebAPI przez HttpClient z tokenem JWT.
Wszystkie formularze muszą mieć walidację DataAnnotations.
Wygeneruj kompletne pliki .razor.
```

---

### Dev D — Sesja 1: BlazorWASM setup + katalog gier + szczegóły

```
Buduję główną aplikację użytkownika GameHub w Blazor WebAssembly (.NET 8).
Połączenie z WebAPI przez HttpClient, JWT w localStorage.

Potrzebuję:

1. SETUP:
   - Program.cs z HttpClient baseAddress
   - CustomAuthStateProvider dziedziczący AuthenticationStateProvider
     (parsuje JWT z localStorage, zwraca ClaimsPrincipal)
   - LoginPage.razor i RegisterPage.razor z walidacją

2. /games — katalog:
   - Siatka kart (okładka, tytuł, gatunek, cena)
   - Filtrowanie: dropdown gatunku, sortowanie, pole wyszukiwania (debounce 300ms)
   - Paginacja

3. /games/{id} — szczegóły:
   - Baner z okładką, opis, gatunek, cena
   - Przycisk "Kup teraz" → modal z aktualnym saldem portfela
   - Sekcja recenzji: lista + formularz (Rating 1–10, Content, walidacja)

Użyj MudBlazor lub Radzen (minimum 2 zewnętrzne komponenty w projekcie).
Wygeneruj kompletne pliki .razor i serwisy HttpClient.
```

---

### Dev D — Sesja 2: Profil, biblioteka, portfel

```
Kontynuuję BlazorWASM GameHub. Mam: auth, katalog gier, szczegóły z recenzjami.

Potrzebuję 3 kolejne strony:

1. /profile:
   - Wyświetlenie: avatar (img), username, bio, data rejestracji
   - Formularz edycji: bio (textarea), avatarUrl (input)
   - PUT /api/auth/profile z walidacją

2. /library:
   - Lista gier zakupionych przez zalogowanego użytkownika
   - GET /api/users/me/games
   - Karty z datą zakupu i ceną zapłaconą

3. /wallet:
   - Aktualne saldo (WalletBalance)
   - Formularz doładowania (walidacja: min 5, max 500)
   - Historia transakcji — MudTable lub Radzen DataGrid
     kolumny: data, typ (Purchase/TopUp/Refund), kwota, nazwa gry

Wszystkie strony wymagają [Authorize]. Niezalogowany → redirect do /login.
Wygeneruj kompletne pliki .razor.
```

---

### Dev C — Sesja 2: BlazorWASM strona znajomych

```
Pracuję nad BlazorWASM GameHub. Mam gotowe: auth, katalog, profil, bibliotekę.

Potrzebuję strony /friends — system znajomych:

1. Sekcje (zakładki lub osobne listy):
   - "Znajomi" — lista zaakceptowanych
     Karta: avatar, username, przycisk "Usuń znajomego"
   - "Zaproszenia" — oczekujące przychodzące
     Karta: username, data, przyciski "Akceptuj" / "Odrzuć"
   - "Wyślij zaproszenie" — input username + przycisk "Zaproś"

2. API calls:
   - GET    /api/friends
   - GET    /api/friends/requests
   - POST   /api/friends/invite/{userId}
   - PUT    /api/friends/{id}/accept
   - DELETE /api/friends/{id}

3. UX:
   - Loading spinner (MudProgressCircular)
   - Snackbar po akcji (sukces/błąd)
   - Pusta lista → komunikat "Nie masz jeszcze znajomych"

Wygeneruj Friends.razor + FriendshipService.cs.
```
