# Dokumentacja Techniczna Projektu: GameHub

**Status:** Etapy 1-3 (Core System) ukończone.
**Cel projektu:** Zbudowanie pełnoprawnej platformy cyfrowej dystrybucji gier z systemem autoryzacji, wirtualnym portfelem oraz biblioteką użytkownika.

## Stos Technologiczny

- **Backend:** C# / .NET 8, ASP.NET Core WebAPI
- **Frontend:** Blazor WebAssembly (WASM)
- **Baza Danych:** SQLite
- **ORM:** Entity Framework Core
- **Interfejs Użytkownika:** MudBlazor (Material Design)
- **Autoryzacja:** JSON Web Tokens (JWT)

---

## 🏗️ Etap 1: Fundamenty, Architektura i Baza Danych

W pierwszym etapie skupiliśmy się na stworzeniu skalowalnego i łatwego w utrzymaniu szkieletu aplikacji, opierając się na założeniach Clean Architecture.

- **Podział na Warstwy (Clean Architecture):**
- **Domain:** Zawiera główne encje biznesowe (`User`, `Game`, `UserGame`, `Review`) oraz interfejsy repozytoriów.
- **Application:** Przechowuje logikę biznesową, DTOs (Data Transfer Objects) oraz interfejsy serwisów (m.in. `IWalletService`).
- **Infrastructure:** Implementuje dostęp do danych (EF Core), generowanie tokenów JWT oraz wzorzec Unit of Work.
- **WebAPI:** Wystawia końcówki REST (Controllers) i obsługuje żądania HTTP.

- **Baza Danych i ORM:** Skonfigurowano Entity Framework Core z relacyjną bazą SQLite. Utworzono tabele oraz zdefiniowano relacje (w tym relację wiele-do-wielu między użytkownikiem a grami poprzez tabelę asocjacyjną `UserGames`).
- **Wzorzec Unit of Work:** Wdrożono scentralizowany mechanizm zatwierdzania zmian w bazie danych, zapewniający spójność transakcji (np. jednoczesne dodanie gry i pobranie opłaty).
- **Data Seeding:** Napisano skrypt automatycznie inicjalizujący bazę danych początkowymi danymi (katalog 5 gier) oraz kontami testowymi (np. _Alice_ z przypisanym saldem 150 PLN na start).

---

## 🔒 Etap 2: Bezpieczeństwo i Autoryzacja

Zaimplementowano w pełni bezstanowy system autoryzacji oparty na tokenach, pozwalający na bezpieczną komunikację między aplikacją Blazor a serwerem WebAPI.

- **Rejestracja i Bezpieczeństwo Danych:** Endpoint `/api/auth/register` weryfikuje unikalność adresu email oraz tworzy nowego użytkownika z domyślnym saldem 0.00 PLN.
- **Generowanie Tokenów JWT:** Po poprawnym logowaniu serwer tworzy kryptograficznie podpisany token. Zawiera on zestaw informacji (Claims), w tym identyfikator użytkownika, email, rolę oraz `WalletBalance`.
- **Zarządzanie Stanem w Blazor WASM:** \* Wdrożono `CustomAuthStateProvider`, który na bieżąco odczytuje token JWT z lokalnej pamięci przeglądarki (`LocalStorage`).
- Nagłówki HTTP są automatycznie wzbogacane o token (Bearer), dzięki czemu każde kolejne żądanie do serwera jest autoryzowane.

- **Dynamiczny Interfejs (UX Security):** Użyto komponentu `<AuthorizeView>`, aby warunkowo renderować elementy interfejsu. Niezalogowani użytkownicy widzą przycisk "Zaloguj się", podczas gdy uwierzytelnieni otrzymują dostęp do portfela, Biblioteki oraz awatara z opcją wylogowania.

---

## 🛒 Etap 3: Przepływ Biznesowy (Sklep i Transakcje)

Zbudowano główny "silnik" platformy, łączący warstwę prezentacji z bazą danych poprzez bezpieczne transakcje.

- **Publiczny Katalog Gier (Sklep):**
- Stworzono endpoint `GET /api/games` zwracający gry z flagą `IsVisible = true`.
- Na frontendzie zaimplementowano widok `Home.razor`, wykorzystujący siatkę kart (`MudCard`) do atrakcyjnej prezentacji okładek, opisów i cen.

- **Mechanizm Zakupów i Portfel (WalletService):**
- Wdrożono zabezpieczony atrybutem `[Authorize]` endpoint `POST /api/games/{id}/buy`.
- Logika serwerowa sprawdza, czy użytkownik nie posiada już danego tytułu oraz czy jego `WalletBalance` jest wystarczający.
- System wykonuje atomową operację zapisu: odejmuje środki z konta użytkownika i natychmiastowo dodaje wpis do tabeli `UserGames`.

- **Prywatna Biblioteka Gracza:**
- Endpoint `GET /api/games/library` identyfikuje użytkownika po tokenie JWT i zwraca wyłącznie te produkcje, które zostały przez niego zakupione.
- Zbudowano dedykowany, zabezpieczony widok w Blazorze wyświetlający kolekcję gier użytkownika.

- **Interakcja z Użytkownikiem:** Dodano interaktywne okna dialogowe (`DialogService`) upewniające gracza przed zakupem oraz powiadomienia typu "toast" (`Snackbar`), informujące o sukcesie lub odrzuceniu transakcji z powodu braku środków.

---

## 🚀 Mapa Drogowa (Kolejne Etapy)

- **Etap 4:** Rozbudowa modułu profilu (doładowywanie portfela), system oceniania i recenzowania posiadanych gier.
- **Etap 5:** Moduł społecznościowy (znajomi) oraz optymalizacje Globalnego Stanu Aplikacji (reaktywne odświeżanie salda).
