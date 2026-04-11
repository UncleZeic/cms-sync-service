FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CmsSyncService.slnx ./

COPY src/CmsSyncService.Api/CmsSyncService.Api.csproj src/CmsSyncService.Api/
COPY src/CmsSyncService.Application/CmsSyncService.Application.csproj src/CmsSyncService.Application/
COPY src/CmsSyncService.Domain/CmsSyncService.Domain.csproj src/CmsSyncService.Domain/
COPY src/CmsSyncService.Infrastructure/CmsSyncService.Infrastructure.csproj src/CmsSyncService.Infrastructure/

COPY tests/CmsSyncService.UnitTests/CmsSyncService.UnitTests.csproj tests/CmsSyncService.UnitTests/
COPY tests/CmsSyncService.IntegrationTests/CmsSyncService.IntegrationTests.csproj tests/CmsSyncService.IntegrationTests/

RUN dotnet restore src/CmsSyncService.Api/CmsSyncService.Api.csproj

COPY . .

RUN dotnet publish src/CmsSyncService.Api/CmsSyncService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CmsSyncService.Api.dll"]
