# M347 To-do-App

Die To-do-App ist der rote Faden durch das Modul M347 „Dienst mit Container anwenden“. Du baust sie Schritt für Schritt in Container und betreibst sie als Stack. Den Code musst du dafür nicht verstehen.

## Aufbau

```
frontend/          statische Webseite (HTML, CSS, JS) und nginx.conf
api/               ASP.NET Core Minimal API (.NET 10, C#)
  packages/        lokaler NuGet-Feed (Npgsql)
  nuget.config     sagt dotnet restore, dass nur packages/ verwendet wird
```

So spielen die Teile zusammen:

```
Browser ──▶ frontend (nginx, Port 80) ──/api──▶ api (Port 8080) ──▶ db (PostgreSQL, Port 5432)
```

- Das Frontend ruft die API immer relativ über `/api` auf.
- nginx leitet alles unter `/api` an den Container mit dem Namen `api` auf Port 8080 weiter (siehe `frontend/nginx.conf`).
- Ohne Datenbank speichert die API die To-dos im Arbeitsspeicher. Nach einem Neustart sind sie weg.

## Endpoints der API

| Methode | Pfad | Body | Antwort |
|---|---|---|---|
| GET | `/api/todos` | – | 200, Liste aller To-dos |
| POST | `/api/todos` | `{"title":"…"}` | 201, neues To-do; 400 bei leerem Titel |
| PUT | `/api/todos/{id}` | – | 200, „erledigt“ umgeschaltet; 404 |
| DELETE | `/api/todos/{id}` | – | 204; 404 |
| GET | `/health` | – | 200 `{"status":"ok"}`; 503, wenn die DB nicht erreichbar ist |
| GET | `/api/version` | – | 200 `{"version", "storage", "host"}` |

Ein To-do sieht so aus: `{"id":1,"title":"Dockerfile schreiben","done":false}`

Ist die Datenbank nicht erreichbar, antwortet die API mit 503. Sie stürzt nicht ab und arbeitet weiter, sobald die DB wieder da ist.

## Konfiguration

| ENV-Variable | Wirkung | Standard |
|---|---|---|
| `ConnectionStrings__Todo` | Gesetzt: PostgreSQL, z. B. `Host=db;Database=todo;Username=todo;Password=…`. Nicht gesetzt: In-Memory | nicht gesetzt |
| `APP_VERSION` | Version, die `/api/version` und das Frontend anzeigen | `dev` |
| `ASPNETCORE_HTTP_PORTS` | Port der API | `8080` |

Die Tabelle `todos` legt die API beim Start selbst an.

`ConnectionStrings__Todo`: .NET übersetzt den doppelten Unterstrich `__` in die Konfigurationshierarchie `ConnectionStrings:Todo`.

## Hinweise

- **Kein .NET SDK nötig.** Gebaut wird im Container mit `mcr.microsoft.com/dotnet/sdk:10.0`.
- **Offline:** In der LB gibt es kein Internet. Npgsql liegt deshalb als lokaler NuGet-Feed in `api/packages/`.
- **Frontend ohne API:** nginx startet auch, wenn es noch keinen Container `api` gibt. Die Seite zeigt dann „API nicht erreichbar“.
- **Images:** Verwende fixe Tags, nie `latest`: `nginx:1.30.6-alpine`, `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`, `postgres:17`.

## Wo du die App brauchst

| Block | Was du mit der App machst | Kompetenzen |
|---|---|---|
| 2 | Dockerfile für das Frontend, Port-Mapping, Bind-Mount | D2B, D1B, D1I |
| 3 | Dockerfile für die API, Image auf Docker Hub und in die lokale Registry pushen | C2B, D2I, D1I |
| 4 | API-Dockerfile auf Multi-Stage umbauen, `.dockerignore` | D1A, B1B |
| 5 | Frontend und API als Compose-Stack | B1I, D1I |
| 6 | Stack mit PostgreSQL, Netzwerke, `.env`, Healthchecks | B1I, D1I, D1A, B2A |
| 7 | Testkonzept für den Stack, Demo Kubernetes mit Version 1.0.0 → 1.1.0 | F1B, C1I, C2I, B1I |

**Bezug zur LB:** In LB1 containerisierst du eine unbekannte App und publizierst sie lokal, genau wie in Block 3 und 4. In LB2 baust du einen Stack mit `compose.yaml` und testest ihn, wie in Block 5 bis 7.
