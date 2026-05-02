# URL Shortener with Analytics

ASP.NET Core 10 Web API + MongoDB. Shortens URLs, tracks clicks, and provides analytics including device detection, referrers, and country breakdown.

## Tech Stack

- ASP.NET Core 10 Web API (Controllers)
- MongoDB.Driver (no Entity Framework)
- Serilog (structured logging)
- FluentValidation
- UAParser (device detection)
- Testcontainers.MongoDB (integration tests)
- xUnit + FluentAssertions

## Setup

### Prerequisites

- .NET 10 SDK (for local dev)
- Docker + Docker Compose

---

## Running with Docker (recommended)

Builds the API image and starts both API + MongoDB in one command:

```bash
docker-compose up --build
```

API available at `http://localhost:8080`.  
MongoDB available at `localhost:27017`.

To run in background:

```bash
docker-compose up --build -d
```

To stop:

```bash
docker-compose down
```

To stop and wipe MongoDB data:

```bash
docker-compose down -v
```

### Build the image manually

```bash
docker build -t urlshortener-api .
```

Run against a local MongoDB:

```bash
docker run -p 8080:8080 \
  -e MongoDB__ConnectionString=mongodb://host.docker.internal:27017 \
  -e MongoDB__DatabaseName=urlshortener \
  urlshortener-api
```

---

## Running Locally (without Docker)

### Start MongoDB

```bash
docker-compose up -d mongodb
```

### Run the API

```bash
dotnet run --project src/UrlShortener.Api/UrlShortener.Api.csproj
```

API available at `http://localhost:5000`.

---

## Running Tests

```bash
dotnet test
```

Integration tests spin up a real MongoDB instance via Testcontainers — no local MongoDB needed.

---

## curl Examples

> Replace `localhost:8080` with `localhost:5000` if running locally without Docker.

### Health check

```bash
curl http://localhost:8080/health
```

### Shorten a URL (auto-generated code)

```bash
curl -X POST http://localhost:8080/api/v1/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://www.example.com/very/long/path"}'
```

Response:

```json
{
  "shortCode": "aB3xY9z",
  "shortUrl": "http://localhost:8080/aB3xY9z",
  "originalUrl": "https://www.example.com/very/long/path"
}
```

### Shorten a URL (custom code)

```bash
curl -X POST http://localhost:8080/api/v1/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://github.com/dotnet/aspnetcore", "customCode": "dotnet"}'
```

Returns `409 Conflict` if `customCode` already exists.

### Redirect

```bash
curl -L http://localhost:8080/dotnet
```

Follows the 302 redirect to the original URL.

### Get stats

```bash
curl http://localhost:8080/api/v1/stats/dotnet
```

Response:

```json
{
  "totalClicks": 3,
  "clicksByDay": [
    { "date": "2026-05-02", "count": 3 }
  ],
  "topReferrers": [
    { "referrer": "", "count": 3 }
  ],
  "countryBreakdown": [
    { "country": "Unknown", "count": 3 }
  ],
  "deviceBreakdown": [
    { "device": "Other", "count": 3 }
  ]
}
```

`clicksByDay` covers last 30 days. Uses MongoDB aggregation pipeline.

### Soft delete a link

```bash
curl -X DELETE http://localhost:8080/api/v1/links/dotnet
```

Returns `204 No Content`. Link excluded from redirects; click data retained for analytics.

### Validation error (expect 400 ProblemDetails)

```bash
curl -X POST http://localhost:8080/api/v1/shorten \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "not-a-url"}'
```

---

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/v1/shorten` | Create short URL |
| `GET` | `/{shortCode}` | Redirect to original URL (302) |
| `GET` | `/api/v1/stats/{shortCode}` | Get click analytics |
| `DELETE` | `/api/v1/links/{shortCode}` | Soft delete link |
| `GET` | `/health` | MongoDB health check |

---

## Data Model

### `links` collection

| Field | Type | Notes |
|---|---|---|
| `_id` | ObjectId | |
| `shortCode` | string | Unique index |
| `originalUrl` | string | |
| `createdAt` | DateTime | UTC |
| `createdBy` | string? | Nullable, reserved for auth |
| `isDeleted` | bool | Soft delete flag |

### `clicks` collection

| Field | Type | Notes |
|---|---|---|
| `_id` | ObjectId | |
| `shortCode` | string | Indexed |
| `clickedAt` | DateTime | Compound index with shortCode |
| `ipAddress` | string | |
| `country` | string | Stubbed (see note) |
| `userAgent` | string | Raw user-agent string |
| `referrer` | string | |
| `device` | string | `Desktop` / `Mobile` / `Tablet` via UAParser |

**Indexes:**
- Unique index on `links.shortCode`
- Compound index on `clicks`: `{ shortCode: 1, clickedAt: -1 }`

---

## Notes

### GeoIP

Country resolution is currently stubbed — returns `"Unknown"` for all clicks. To enable real geolocation, integrate [MaxMind GeoLite2](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data) and replace the stub in `RedirectController`.

### Authentication

No authentication is implemented. The `createdBy` field in `links` is reserved for it. Add JWT middleware at the controller level and populate `createdBy` from claims when ready.

### Error Format

All errors return [RFC 7807 ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "OriginalUrl": ["'Original Url' must not be empty."]
  }
}
```
