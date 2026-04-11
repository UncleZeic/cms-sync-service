#!/bin/bash

echo "Clear old containers"
docker compose down -v || true

echo "Run a one time initialCreate migration"
docker compose run --rm migrate sh -c "
  dotnet tool install --global dotnet-ef --version 9.* || true &&
  dotnet restore src/CmsSyncService.Api/CmsSyncService.Api.csproj &&
  dotnet ef migrations add InitialCreate --project src/CmsSyncService.Infrastructure --startup-project src/CmsSyncService.Api
"

echo "Fire up docker compose"
docker compose up --build
