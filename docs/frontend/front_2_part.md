Oto zaktualizowana i rozbudowana dokumentacja w formacie Markdown, podsumowująca wszystkie nowe moduły i zaawansowane mechanizmy, które wdrożyliśmy od momentu zakończenia Etapu 3.

Możesz skopiować poniższy kod i dołączyć go do swojego pliku `README.md` lub dokumentacji projektowej.

---

# Dokumentacja Techniczna Projektu: GameHub (Aktualizacja)

**Status:** Etapy 1-6 ukończone (Wersja Release Candidate).
**Nowe osiągnięcia:** Wdrożenie pełnego Panelu Administratora, modułu społecznościowego, systemu recenzji, zaawansowanej historii portfela oraz pełnej obsługi statycznych plików graficznych.

---

## 👤 Etap 4: Zaawansowany Profil, Ekonomia i Recenzje

W tym etapie skupiliśmy się na oddaniu większej kontroli w ręce użytkownika, rozbudowując aspekty ekonomiczne i interakcje ze społecznością.

- **Moduł Portfela i Transakcji (`Profile.razor`):**
- **Doładowanie salda:** Stworzono formularz z walidacją `[Range(1, 10000)]` pozwalający na symulowane zasilenie portfela. Środki są natychmiastowo aktualizowane na górnym pasku nawigacji.
- **Historia Transakcji:** Wdrożono endpoint pobierający kompletną historię konta. Dodano dynamiczną tabelę w Blazorze odróżniającą "Wpłaty" (kolor zielony) od "Zakupów" gier (kolor czerwony) z uwzględnieniem dat z dokładnością do minuty (`CreatedAt`).

- **System Ocen i Recenzji (`GameDetails.razor`):**
- Zbudowano dedykowaną stronę ze szczegółami gry (`/game/{id}`).
- **Wystawianie opinii:** Zalogowani gracze (którzy posiadają dany tytuł) mogą oceniać gry w skali 1-5 gwiazdek oraz pisać recenzje tekstowe. Użyto komponentu `<MudRating>`.
- **Agregacja Danych:** System automatycznie oblicza średnią ocenę gry ze wszystkich recenzji i wyświetla ją pod tytułem.
- Formularz dodawania opinii jest warunkowo ukrywany (`<AuthorizeView>`), jeśli użytkownik jest niezalogowany.

---

## 🤝 Etap 5: Moduł Społecznościowy (Znajomi)

Zaimplementowano system relacji między użytkownikami, wzorowany na największych platformach gamingowych.

- **Zarządzanie Relacjami (`Friends.razor`):**
- **Wysyłanie zaproszeń:** Możliwość wyszukania innego gracza po adresie email i wysłania zaproszenia do znajomych.
- **Oczekiwanie i Akceptacja:** Podział interfejsu na dwie listy: "Oczekujące zaproszenia" (z opcją akceptacji/odrzucenia) oraz "Twoi znajomi".

- **Podgląd Biblioteki (`FriendLibrary.razor`):**
- Dodano możliwość interakcji z profilem znajomego. Użytkownik może wejść w dedykowany widok, aby sprawdzić, jakie gry posiada jego znajomy, co zachęca do interakcji wewnątrz platformy.

---

## 👑 Etap 6: Centrum Dowodzenia (Panel Administratora)

Stworzono wysoce zabezpieczony, w pełni funkcjonalny CMS (Content Management System) do zarządzania całą platformą bez konieczności ingerencji w kod czy bazę danych.

- **Bezpieczeństwo i Architektura (`AdminController`):**
- Cały kontroler zabezpieczono atrybutem `[Authorize(Roles = "Admin")]`.
- Dodano zabezpieczenia logiczne uniemożliwiające administratorowi przypadkowe zablokowanie własnego konta lub odebranie sobie uprawnień.

- **Zarządzanie Użytkownikami (`Admin.razor` - Zakładka 1):**
- Wyświetlanie tabeli wszystkich użytkowników z ich rolami, stanem portfela i datą rejestracji.
- Przyciski szybkiej akcji: Toggle blokady konta (`IsBlocked`) oraz awansowanie/degradowanie ról (User/Admin).

- **Zarządzanie Katalogiem Sklepu (`Admin.razor` - Zakładka 2):**
- **CRUD Gier:** Wdrożono wysuwany z boku ekranu panel (Drawer) z formularzem umożliwiającym dodawanie nowych gier oraz edycję istniejących tytułów, ich ceny i opisów.
- **Soft Delete i Przywracanie:** Wprowadzono bezpieczne usuwanie gier. Zamiast fizycznie usuwać rekord z bazy, system wykorzystuje flagę `IsVisible`. Ukryte gry znikają ze sklepu graczy, ale w panelu administratora są widoczne (wyszarzone) z możliwością przywrócenia jednym kliknięciem (Endpoint `PATCH /restore`).

---

## 🎨 Etap 7: UX/UI i Zarządzanie Zasobami Statycznymi

Platforma zyskała profesjonalny szlif wizualny oraz inteligentne zarządzanie grafikami.

- **Integracja Okładek Gier:**
- Wdrożono obsługę plików graficznych serwowanych z folderu `wwwroot/images/games/`.
- Administrator może z poziomu formularza przypisać ścieżkę do obrazka.
- Wdrożono operatory warunkowe (ternary) – jeśli gra w bazie nie posiada grafiki, interfejs ładuje bezpieczny "Placeholder", zapobiegając załamaniom układu strony (tzw. layout shift).

- **Responsywność i Flexbox:**
- Karty gier w Sklepie oraz szczegóły produktu (Hero Section) wykorzystują płynne siatki (`Fluid="true"`, `object-fit: cover`), dostosowując się do szerokości kolumn bez najeżdżania na teksty.
- Zaimplementowano unikalne konteksty autoryzacji (`Context="userAuth"`), eliminując znane błędy `InvalidCastException` podczas szybkiego przeładowywania drzewa DOM (DOM diffing) po akcjach administratorskich.
