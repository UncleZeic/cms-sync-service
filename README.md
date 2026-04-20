# CMS Sync Service (.NET 10)

## Description
A .NET 10 Web API that ingests CMS webhook events, stores the latest known entity state in PostgreSQL, and exposes a protected REST API for consumers. The service supports published, unpublished, deleted, and admin-disabled entities.

## Architecture
- **Api**: Controllers, authentication, Swagger, middleware
- **Application**: Business services, DTO validation, caching
- **Infrastructure**: EF Core contexts, repositories, migrations
- **Domain**: Entity business rules for publish, unpublish, delete, and versioning

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or Docker Engine

## Setup & Run

### Option 1: Docker Compose

Start PostgreSQL, run migrations, and start the API:

```sh
docker compose up --build
```

Compose reads environment overrides from `.env`. Start from the template when you need local overrides:

macOS/Linux:

```sh
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

The API is available at:

```text
http://localhost:8080
```

Swagger UI is available in Development at:

```text
http://localhost:8080/swagger
```

### Option 2: Local API + Docker PostgreSQL

Start PostgreSQL:

```sh
docker compose up -d db
```

Apply EF Core migrations:

```sh
dotnet ef database update --project src/CmsSyncService.Infrastructure --startup-project src/CmsSyncService.Api
```

Run the API:

```sh
dotnet run --project src/CmsSyncService.Api
```

The local API uses the `DefaultConnection` value from `src/CmsSyncService.Api/appsettings.json`.

## Authentication
All CMS and entity endpoints use Basic Authentication. Development credentials are stored as SHA256 hashes in `src/CmsSyncService.Api/appsettings.json`.

Plaintext local-development credentials are listed in [CREDENTIALS.md](CREDENTIALS.md). Do not use these credentials in production.

Roles:
- `cms-event-user`: can post CMS events to `/cms/events`
- `viewer`: can read published, non-disabled entities
- `admin`: can read all entities and set the API-only admin disabled flag

## API Endpoints
- `GET /health`: public health check
- `POST /cms/events`: ingest a batch of CMS events, requires `cms-event-user`
- `GET /cms/entities`: list entities with pagination metadata, requires `viewer` or `admin`
- `GET /cms/entities/{id}`: get one visible entity, requires `viewer` or `admin`
- `PATCH /cms/entities/{id}`: set `adminDisabled`, requires `admin`

CMS event example:

```json
[
  {
    "type": "publish",
    "id": "entity-1",
    "payload": { "title": "Example" },
    "version": 1,
    "timestamp": "2024-01-01T00:00:00Z"
  }
]
```

Entity list response example:

```json
{
  "items": [
    {
      "id": "entity-1",
      "payload": "{\"title\":\"Example\"}",
      "version": 1,
      "updatedAtUtc": "2024-01-01T00:00:00Z"
    }
  ],
  "total": 1,
  "skip": 0,
  "take": 100
}
```

## Data Rules
- `publish` creates or updates an entity when the incoming version is current.
- `delete` hard-deletes the entity from persistence.
- `unpublish` keeps entity data but marks it unavailable to normal viewers.
- Admin disable is an API-side override and does not affect CMS data.
- Entity IDs may contain only letters, numbers, hyphens, and underscores.

## Testing
Unit tests and integration tests can be run with:

```sh
dotnet test tests/CmsSyncService.UnitTests/CmsSyncService.UnitTests.csproj
dotnet test tests/CmsSyncService.IntegrationTests/CmsSyncService.IntegrationTests.csproj
```

Integration tests use SQLite in-memory through `TestWebApplicationFactory`, so PostgreSQL does not need to be running for the test suite.

## Tech Stack
- .NET 10
- ASP.NET Core Web API
- Swagger / Swashbuckle
- PostgreSQL via Docker
- EF Core with Npgsql
- SQLite in-memory for integration tests
- BenchmarkDotNet

## CI/CD
GitHub Actions runs restore, build, unit tests, integration tests, and benchmarks on pushes and pull requests to `main`.

On pushes to `main`, CI also builds and publishes the API container image to GitHub Container Registry:

```text
ghcr.io/<owner>/<repo>:<commit-sha>
ghcr.io/<owner>/<repo>:main
ghcr.io/<owner>/<repo>:latest
```

Pull requests build the image without publishing it.

## Performance Benchmarks
This project uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to track cache performance over time. Benchmarks are run automatically in CI, and results are uploaded as artifacts.

Run benchmarks locally:

```sh
dotnet run -c Release --project benchmarks/benchmarks.csproj
```

Compare benchmark results:

```sh
cd benchmarks
python3 compare_benchmarks.py
```

The comparison script parses BenchmarkDotNet markdown table output and fails if any operation regresses by more than 10%.

Benchmark scenarios:
- Get cached value
- Set cached value
- Cold cache access
- Large object access
- Cache miss
- Concurrent access
- Memory and GC allocation tracking
