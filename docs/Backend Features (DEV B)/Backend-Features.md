# GameHub — Backend Features (DEV B)

Dokumentacja warstwy WebAPI dostarczonej przez **DEV B**: kontrolery CRUD,
DTOs, moderacja, system znajomych i zgłoszeń, panel admina.

> **Uzupełnienie do:** `docs/general/PLAN.md` (podział ról, harmonogram)
> i `docs/Architekture i inicjalizacja/Architecture.md` (struktura solution).

---

## Spis treści

1. [Cel i zakres](#1-cel-i-zakres)
2. [Architektura kodu](#2-architektura-kodu)
3. [Endpointy WebAPI — przegląd](#3-endpointy-webapi--przegląd)
4. [DTOs — struktura](#4-dtos--struktura)
5. [Decyzje projektowe](#5-decyzje-projektowe)
6. [Mapowanie na wytyczne prowadzącego](#6-mapowanie-na-wytyczne-prowadzącego)
7. [Konta testowe](#7-konta-testowe)
8. [Przykładowy flow end-to-end](#8-przykładowy-flow-end-to-end)
9. [Logowanie](#9-logowanie)
10. [Co dalej](#10-co-dalej)

---

## 1. Cel i zakres

DEV B odpowiada w GameHubie za **funkcjonalności biznesowe** zbudowane na fundamencie
(autoryzacji + EF Core), który dostarczył DEV A. Dostarczone artefakty:

- **6 kontrolerów WebAPI** (24 endpointy łącznie)
- **13 DTOs** w warstwie Application
- **Decyzja:** brak konkretnych repozytoriów — wystarcza generyczny `IRepository<T>`

Kod nie modyfikuje warstwy Domain ani Infrastructure DEV A — wszystkie zmiany
dodawane są w `GameHub.WebAPI/Controllers/` oraz `GameHub.Application/DTOs/`.

---

## 2. Architektura kodu

```
src/
├── GameHub.Application/
│   └── DTOs/
│       ├── Admin/AdminDtos.cs
│       ├── Friendships/FriendshipsDtos.cs
│       ├── Games/GamesDtos.cs
│       ├── Reports/ReportsDtos.cs
│       ├── Reviews/ReviewsDtos.cs
│       └── UserGames/UserGamesDtos.cs
│
└── GameHub.WebAPI/
    └── Controllers/
        ├── AdminController.cs
        ├── FriendshipsController.cs
        ├── GamesController.cs          (+ /buy od DEV A, scalone)
        ├── ReportsController.cs
        ├── ReviewsController.cs
        └── UserGamesController.cs
```

Kontrolery komunikują się z bazą wyłącznie przez `IUnitOfWork` (z `GameHub.Domain`),
co utrzymuje warstwę WebAPI niezależną od EF Core.

---

## 3. Endpointy WebAPI — przegląd

### 3.1 `GamesController` — `/api/games`

Metoda | Ścieżka | Auth | Opis
---|---|---|---
GET | `/api/games?genre=&sort=&search=` | Publiczny | Lista widocznych gier z filtrowaniem
GET | `/api/games/{id}` | Publiczny | Szczegóły gry + średnia ocen z widocznych recenzji
POST | `/api/games` | Admin | Dodanie nowej gry
PUT | `/api/games/{id}` | Admin | Aktualizacja gry (partial — null pomijane)
DELETE | `/api/games/{id}` | Admin | Soft-delete (`IsVisible = false`)
POST | `/api/games/{id}/buy` | Authorize | Zakup gry (logika w `WalletService` DEV A)

**Filtry GET `/api/games`:**
- `genre` — dokładne dopasowanie nazwy gatunku
- `sort` — `price_asc` / `price_desc` / `newest` / `title` (domyślnie)
- `search` — fragmentaryczne dopasowanie w `Title` lub `Description`

### 3.2 `ReviewsController` — routes split

Metoda | Ścieżka | Auth | Opis
---|---|---|---
GET | `/api/games/{gameId}/reviews` | Publiczny / Admin | Recenzje gry (admin widzi też ukryte)
POST | `/api/games/{gameId}/reviews` | Authorize | Tworzy recenzję — limit **1 per user per gra**
PUT | `/api/reviews/{id}` | Authorize, właściciel | Edycja własnej recenzji
DELETE | `/api/reviews/{id}` | Authorize, właściciel lub Admin | Usunięcie
PATCH | `/api/reviews/{id}/hide` | Admin | Toggle `IsHidden` (moderacja)

> **Uwaga:** brak `[Route]` na klasie, bo endpointy są pod dwoma prefiksami
> (`/api/games/{gameId}/reviews` i `/api/reviews/{id}`). Każda metoda ma pełną ścieżkę.

### 3.3 `UserGamesController` — `/api/users`

Metoda | Ścieżka | Auth | Opis
---|---|---|---
GET | `/api/users/me/games` | Authorize | Biblioteka gier zalogowanego użytkownika

Endpoint zakupu (`POST /api/games/{id}/buy`) znajduje się w `GamesController`.

### 3.4 `FriendshipsController` — `/api/friends`

Metoda | Ścieżka | Auth | Opis
---|---|---|---
GET | `/api/friends` | Authorize | Lista zaakceptowanych znajomych
GET | `/api/friends/requests` | Authorize | Oczekujące zaproszenia przychodzące
POST | `/api/friends/invite/{targetUserId}` | Authorize | Wyślij zaproszenie
PUT | `/api/friends/{id}/accept` | Authorize, addressee | Zaakceptuj
DELETE | `/api/friends/{id}` | Authorize, dowolna strona | Odrzuć / anuluj / usuń znajomego

**Walidacje przy `POST invite`:**
- nie można zaprosić samego siebie
- użytkownik docelowy musi istnieć i nie być zablokowany
- relacja w którymkolwiek kierunku już istnieje → `409 Conflict` z czytelnym komunikatem
  (np. „Ta osoba już wysłała Ci zaproszenie — zaakceptuj je…")

### 3.5 `ReportsController` — `/api/reports`

Metoda | Ścieżka | Auth | Opis
---|---|---|---
POST | `/api/reports` | Authorize | Zgłoszenie usera lub gry
GET | `/api/reports?status=` | Admin | Lista wszystkich zgłoszeń (filtr po statusie)
PATCH | `/api/reports/{id}/status` | Admin | Zmiana statusu

**Reguła biznesowa:** dokładnie jedno z `TargetUserId` / `TargetGameId` musi być
podane — oba `null` lub oba ustawione zwracają `400`.

**Status:** `Open` / `InProgress` / `Closed` (enum `ReportStatus`, parsowany ze stringa).

### 3.6 `AdminController` — `/api/admin`

Metoda | Ścieżka | Auth | Opis
---|---|---|---
GET | `/api/admin/users` | Admin | Lista wszystkich użytkowników
PATCH | `/api/admin/users/{id}/block` | Admin | Toggle `IsBlocked`
PATCH | `/api/admin/users/{id}/role` | Admin | Zmiana roli (`User` / `Admin`)

**Zabezpieczenia:**
- admin nie może zablokować ani zmienić roli własnego konta (`400 Bad Request`)
- nieprawidłowa rola → `400` z listą dozwolonych wartości

---

## 4. DTOs — struktura

Plik | Zawartość
---|---
`Games/GamesDtos.cs` | `GameDto`, `CreateGameRequest`, `UpdateGameRequest`
`Reviews/ReviewsDtos.cs` | `ReviewDto`, `CreateReviewRequest`, `UpdateReviewRequest`
`UserGames/UserGamesDtos.cs` | `UserGameDto`
`Friendships/FriendshipsDtos.cs` | `FriendDto`, `FriendRequestDto`
`Reports/ReportsDtos.cs` | `CreateReportRequest`, `UpdateReportStatusRequest`, `ReportDto`
`Admin/AdminDtos.cs` | `UserAdminDto`, `UpdateRoleRequest`

Wszystkie DTOs to **`record`** (immutable), zgodnie z konwencją zastosowaną przez DEV A
w `Application.DTOs.Auth`. Walidacja request DTOs przez `DataAnnotations` —
`[ApiController]` automatycznie zwraca `400` przy błędach.

---

## 5. Decyzje projektowe

### 5.1 Generyczne repozytorium zamiast konkretnych
`PLAN.md` przewidywał `GameRepository.GetWithFilters`, `ReviewRepository.GetByGameId`
itd. Komentarz w `EfRepository.cs` (DEV A) wprost dopuszcza pominięcie konkretnych repo,
o ile generyczny `FindAsync(Expression)` wystarcza. **Wystarczył** — całe filtrowanie
i sortowanie żyje w kontrolerach jako `Expression<Func<T, bool>>` przekazywany do
`FindAsync` lub `FindWithIncludesAsync`. To redukuje liczbę plików o 3 i nie narusza
wymagań prowadzącego (pkt 5 — „podejście oparte na repozytoriach i jednostkach pracy"
jest spełnione przez `EfRepository<T>` i `EfUnitOfWork`).

### 5.2 Pobieranie `userId` z JWT
Konsekwentnie w każdym kontrolerze:
```csharp
var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```
DEV A wkłada `user.Id.ToString()` pod `ClaimTypes.NameIdentifier` w `JwtTokenService`,
więc parsowanie do `int` jest bezpieczne.

### 5.3 Autoryzacja oparta na rolach
Role są wstrzykiwane do JWT jako claim `ClaimTypes.Role`. Endpointy adminowe używają
`[Authorize(Roles = "Admin")]` — atrybut jest egzekwowany przez middleware przed
wejściem do akcji. Tam gdzie trzeba odróżnić właściciela od admina ręcznie
(np. `DELETE /api/reviews/{id}`), odczytujemy claim:
```csharp
var userRole = User.FindFirstValue(ClaimTypes.Role)!;
if (review.UserId != userId && userRole != "Admin") return Forbid();
```

### 5.4 Soft-delete dla gier i toggle dla moderacji
- `DELETE /api/games/{id}` ustawia `IsVisible = false` zamiast usuwać rekord —
  zachowuje historię zakupów i recenzji.
- `PATCH /api/reviews/{id}/hide` i `PATCH /api/admin/users/{id}/block` są **toggle**:
  jeden endpoint przełącza między dwoma stanami. Frontend nie musi wiedzieć
  z góry, w którym stanie jest rekord.

### 5.5 Eager loading przez `FindWithIncludesAsync`
Tam gdzie kontroler potrzebuje powiązanych encji (np. lista recenzji z nazwą autora,
lista zgłoszeń z danymi reportera i celu), używamy metody dodanej do `IRepository<T>`
przez DEV A:
```csharp
var reviews = await _uow.Reviews.FindWithIncludesAsync(
    r => r.GameId == gameId, ct, r => r.User);
```
Unika problemu N+1 — jeden SQL z JOIN zamiast `1 + N` osobnych `SELECT`.

### 5.6 Idempotencja i czytelne statusy HTTP
- POST tworzy → `201 Created` z nagłówkiem `Location`
- POST naruszające unique constraint → `409 Conflict` (nie 400 ani 500)
- PUT/PATCH/DELETE bez ciała odpowiedzi → `204 No Content`
- Nieuprawniona próba edycji cudzego zasobu → `403 Forbidden`
- Endpointy z `Authorize(Roles = "Admin")` zwracają `403`, gdy zalogowany user
  nie jest adminem, oraz `401`, gdy brak tokena

### 5.7 Reguła „dokładnie jeden cel" w `ReportsController`
Nie da się tego wyrazić przez `DataAnnotations` — walidacja jest jawna w akcji POST.
Sprawdzenie `hasUser == hasGame` wyłapuje oba błędne przypadki naraz
(oba `null` lub oba ustawione).

---

## 6. Mapowanie na wytyczne prowadzącego

Punkt wytycznych | Spełnienie
---|---
2 — baza SQLite, min. 5 tabel | 7 tabel (Users, Games, UserGames, Reviews, Friendships, Reports, Transactions)
3 — czysta architektura, 7 projektów | Struktura `src/` zgodna z `Architecture.md`
4 — kontrolery CRUD dla wszystkich danych | 9 kontrolerów łącznie pokrywa wszystkie 7 encji
5 — repozytoria + UoW | `EfRepository<T>` + `EfUnitOfWork`
6 — model domenowy + `IRepository` | warstwa `GameHub.Domain`
7 — logger w WebAPI z dziennym rolowaniem i osobnym plikiem błędów | Serilog DEV A; nasze kontrolery używają `ILogger<>`

---

## 7. Konta testowe

Z seeda DEV A (`DataSeeder.cs`):

Email | Hasło | Rola
---|---|---
`admin@gamehub.com` | `Admin123!` | Admin
`alice@gamehub.com` | `Alice123!` | User
`bob@gamehub.com` | `Bob123!` | User

> **Uwaga:** wartości haseł różnią się od oryginalnego planu (`user1` / `User123!`),
> który zakładał inne nazewnictwo. Aktualny stan to ten zaseedowany w bazie.

---

## 8. Przykładowy flow end-to-end

**Scenariusz:** zalogowanie się jako Alice, kupno gry, dodanie recenzji,
zaproszenie Boba do znajomych.

```http
# 1. Login
POST /api/auth/login
{ "email": "alice@gamehub.com", "password": "Alice123!" }
→ { "token": "eyJ...", "username": "alice", "role": "User" }

# 2. Przegląd katalogu
GET /api/games?genre=Strategy
→ [{ "id": 2, "title": "Shadow Tactics", "price": 39.99, ... }]

# 3. Zakup
POST /api/games/3/buy        (Bearer token)
→ 200 { "message": "Game purchased successfully." }

# 4. Sprawdzenie biblioteki
GET /api/users/me/games      (Bearer token)
→ [{ "gameId": 1, "title": "Cyber Odyssey", ... }, ... ]

# 5. Dodanie recenzji
POST /api/games/3/reviews    (Bearer token)
{ "rating": 9, "content": "Świetna gra!" }
→ 201 Created

# 6. Wysłanie zaproszenia do znajomych
POST /api/friends/invite/3   (Bearer token, targetUserId = bob)
→ 200 { "message": "Zaproszenie do bob zostało wysłane." }
```

**Scenariusz administratora:** moderacja recenzji + zarządzanie userem.

```http
POST /api/auth/login         { "email": "admin@gamehub.com", "password": "Admin123!" }

GET   /api/reports?status=Open                      → lista otwartych zgłoszeń
PATCH /api/reports/1/status  { "status": "Closed" } → zamknięcie zgłoszenia

GET   /api/admin/users                              → lista 3 userów
PATCH /api/admin/users/3/block                      → blokada Boba
PATCH /api/reviews/2/hide                           → ukrycie recenzji
```

---

## 9. Logowanie

Każdy kontroler wstrzykuje `ILogger<NazwaKontrolera>` i loguje w stylu zgodnym
z konwencją z `PLAN.md`:

```
[INF] GET /api/games — zwrócono 6 wyników
[INF] User 2 dodał recenzję do gry 3 (Rating=9)
[INF] Admin 1 zablokował konto userId=3
[WRN] User 2 próbował edytować cudzą recenzję Id=5
[ERR] (przez Serilog HTTP middleware przy 5xx)
```

Logi trafiają do plików konfigurowanych w `Program.cs` przez DEV A:
- `logs/gamehub-YYYY-MM-DD.log` (Info+)
- `logs/errors-YYYY-MM-DD.log` (Warning+)

---

## 10. Co dalej

**Integracja z frontendem (Dzień 12, wspólnie z DEV A):**
- testy manualne end-to-end z BlazorWASM (DEV D) i BlazorServer (DEV C)
- weryfikacja CORS, jeśli WASM hostowany jest osobno od WebAPI
- sanity check tokenów JWT i ich claimów po stronie obu frontendów

**Edge cases do testów manualnych:**
- próba edycji cudzej recenzji → `403`
- POST recenzji po raz drugi na tę samą grę → `409`
- POST `/api/friends/invite/{siebie}` → `400`
- DELETE gry, która była już ukryta → `204` (idempotentnie)
- PATCH `/api/reports/1/status` z błędną wartością statusu → `400`
- niezalogowany dostęp do endpointu `[Authorize]` → `401` z nagłówkiem `WWW-Authenticate`

**Drobiazgi do uzgodnienia z zespołem:**
- W `Program.cs` DEV A jest duplikat bloku `using (var scope ...)` wywołującego
  `DataSeeder.SeedAsync` dwa razy. Niegroźne (seed jest idempotentny), ale warto
  usunąć drugi blok.
- `PLAN.md` w sekcji „Konta testowe" wciąż wskazuje stare emaile `user1@/user2@` —
  faktyczne konta po zaseedowaniu to `alice@/bob@`. Warto zaktualizować PLAN lub seed.