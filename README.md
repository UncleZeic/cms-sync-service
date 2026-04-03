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

## Test User Passwords (for local development and CI only)

These are the plaintext passwords for the users defined in appsettings.json. Do not use these in production or commit them to public repositories.

| Username    | Password (pre-hashed GUID)                | Roles           |
|-------------|-------------------------------------------|-----------------|
| admin       | 7FDD33AD-3FD3-41B8-AC05-5A9122ABC086      | Admin           |
| eventuser   | 9A01D9BF-A5B5-45D4-BE41-618B0F11D6CF      | EventUpdater    |
| viewer      | DD888324-9217-41D1-85D9-20D844090106      | EntityViewer    |

- These passwords are only for use in local development and automated tests.
- Passwords are GUIDs and are hashed in appsettings.json.
- If you regenerate users or hashes, update this section accordingly.

## Tech stack
- .NET 10
- ASP.NET Core Web API
- PostgreSQL (via Docker)
- Entity Framework Core (Npgsql)