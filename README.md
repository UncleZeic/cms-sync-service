# CMS Sync Service (.NET 10)

## Description
A .NET 10 Web API that ingests CMS webhook events, persists the latest known entity state, and exposes a protected REST API for consumers. Now uses PostgreSQL for persistence and Docker for local development.

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine)

## Setup & Run

1. **Start PostgreSQL with Docker Compose:**
	```sh
	docker-compose up -d
	```

2. **Apply EF Core migrations (if any):**
	```sh
	dotnet ef database update --project src/CmsSyncService.Infrastructure --startup-project src/CmsSyncService.Api
	```

3. **Run the API:**
	```sh
	dotnet run --project src/CmsSyncService.Api
	```

The API will connect to PostgreSQL at `localhost:5432` using credentials from `appsettings.json`.


## Development Credentials
Test credentials are defined in `src/CmsSyncService.Api/appsettings.json`.
Users: admin, cms-event-user, viewer
Passwords are SHA256-hashed GUIDs — see the README table below for plaintext values.

See [CREDENTIALS.md](CREDENTIALS.md) for local development and CI credentials plaintext values, for test purposes only.

## Tech stack
- .NET 10
- ASP.NET Core Web API
- PostgreSQL (via Docker)
- Entity Framework Core (Npgsql)