# showtime-service

Showtime Service des Kino-Ticketsystems (Modul 321, Microservices).

## Was der Service macht

Liefert die Stammdaten für den Ticketverkauf:

- **Filme** (Titel, Dauer, Altersfreigabe, Beschreibung)
- **Säle** (Name, Anzahl Reihen, Sitze pro Reihe)
- **Vorstellungen** (Film, Saal, Startzeit in UTC, Preis)
- **Sitzplan-Layout** einer Vorstellung als Labels `A1..E10`. Die Labels werden aus `RowCount` und `SeatsPerRow` berechnet und sind keine eigene Tabelle.

Der Booking Service ruft `GET /v1/showtimes/{id}` synchron auf, um Preis und gültige Sitzplätze zu erfahren.

## Was der Service bewusst NICHT macht

- Er kennt **keine Buchungen** und weiss **nicht, welche Sitze belegt sind**. Das ist Sache des Booking Service mit eigener Datenbank.
- Keine Auth, kein Messaging (RabbitMQ), kein Dockerfile, keine Tests, kein Logging-/OpenTelemetry-Setup (folgt in späteren Wochen).

## Endpunkte

| Methode | Pfad | Beschreibung |
|---|---|---|
| GET | `/v1/films` | Alle Filme |
| GET | `/v1/showtimes?date=YYYY-MM-DD` | Vorstellungen eines Tages (UTC), mit Filmtitel und Saalname. Ungültiges Format → 400. Ohne `date`: alle kommenden. |
| GET | `/v1/showtimes/{id}` | Eine Vorstellung inkl. Film, Saal, Preis und `seatLabels`. Unbekannt → 404 |
| GET | `/v2/films`, `/v2/showtimes`, `/v2/showtimes/{id}` | Wie v1, aber `price` ist `{ "amount": 18.50, "currency": "CHF" }` |
| GET | `/health/ready` | 200 nur wenn die Datenbank antwortet, sonst 503 |

`/v1` ist veraltet: Der Contract markiert die Operationen als `deprecated`, Antworten tragen einen `Deprecation`-Header und jeder
Aufruf wird geloggt (`Veraltete API aufgerufen`). `/v1` wird entfernt, sobald das Log keine Aufrufe mehr zeigt.

Fehler kommen als RFC 7807 ProblemDetails (`application/problem+json`).
Alle Zeiten sind UTC.

Die OpenAPI-Contracts werden bei jedem Build erzeugt und nach
[`contracts/showtime-service.v1.json`](contracts/showtime-service.v1.json) und
[`contracts/showtime-service.v2.json`](contracts/showtime-service.v2.json) kopiert.
Swagger UI: http://localhost:5001/swagger (nur in Development).

## Starten

Voraussetzungen: .NET SDK 10 (x64), Docker.

1. Datenbank aus dem Repo `cinema-deploy` starten:
   ```bash
   docker compose -f docker-compose.infra.yml up -d
   ```
2. Service starten:
   ```bash
   dotnet run --project src/ShowtimeService --launch-profile http
   ```
   Beim Start werden die Migrationen angewendet und die Seed-Daten eingefügt (nur wenn noch keine Filme existieren).
   Der Service läuft auf http://localhost:5001.

Der Connection String steht in `src/ShowtimeService/appsettings.Development.json`
(`showtime_db`, User `showtime_user`, siehe `init.sql` in `cinema-deploy`).

Neue Migration anlegen (dotnet-ef ist als lokales Tool eingetragen):

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/ShowtimeService --output-dir Data/Migrations
```

## Seed-Daten (feste IDs)

| Typ | ID | Inhalt |
|---|---|---|
| Film | `11111111-1111-1111-1111-000000000001` | Interstellar, 169 min, ab 12 |
| Film | `11111111-1111-1111-1111-000000000002` | Alles steht Kopf 2, 96 min, ab 0 |
| Saal | `22222222-2222-2222-2222-000000000001` | Saal 1, 5 Reihen × 10 Sitze (A1..E10) |
| Saal | `22222222-2222-2222-2222-000000000002` | Saal 2, 8 Reihen × 12 Sitze (A1..H12) |
| Vorstellung | `33333333-3333-3333-3333-000000000001` | Interstellar, Saal 1, heute 17:00 UTC, 18.50 |
| Vorstellung | `33333333-3333-3333-3333-000000000002` | Alles steht Kopf 2, Saal 2, heute 20:30 UTC, 15.00 |
| Vorstellung | `33333333-3333-3333-3333-000000000003` | Alles steht Kopf 2, Saal 1, morgen 14:00 UTC, 16.00 |
| Vorstellung | `33333333-3333-3333-3333-000000000004` | Interstellar, Saal 2, morgen 19:00 UTC, 22.00 |

Liegt beim Start die späteste Vorstellung in der Vergangenheit, werden die 4 Vorstellungen
auf dieselben Uhrzeiten von heute und morgen (UTC) verschoben. IDs, Filme, Säle und Preise
bleiben gleich, es werden keine neuen Zeilen angelegt.

## Beispiel: GET /v1/showtimes/{id}

```bash
curl http://localhost:5001/v1/showtimes/33333333-3333-3333-3333-000000000001
```

```json
{
  "id": "33333333-3333-3333-3333-000000000001",
  "film": {
    "id": "11111111-1111-1111-1111-000000000001",
    "title": "Interstellar",
    "durationMinutes": 169,
    "ageRating": 12
  },
  "hall": {
    "id": "22222222-2222-2222-2222-000000000001",
    "name": "Saal 1",
    "rowCount": 5,
    "seatsPerRow": 10
  },
  "startsAtUtc": "2026-09-27T17:00:00Z",
  "price": 18.50,
  "seatLabels": [
    "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "A10",
    "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10",
    "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10",
    "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D10",
    "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "E10"
  ]
}
```

Unbekannte ID:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Vorstellung nicht gefunden",
  "status": 404,
  "detail": "Es gibt keine Vorstellung mit der ID '00000000-0000-0000-0000-000000000099'."
}
```
