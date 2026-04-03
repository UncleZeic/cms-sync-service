using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http.Json;
using CmsSyncService.Api.Tests.TestFixtures;
using CmsSyncService.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CmsSyncService.Api.Tests;

public class CmsEntityControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CmsEntityControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task SeedDbAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsSyncDbContext>();

        // Apply migrations to ensure schema is up to daterun
        await dbContext.Database.MigrateAsync();
        await CmsEntityDbSeeder.ClearAsync(dbContext);
        await CmsEntityDbSeeder.SeedAsync(dbContext);
    }

    private static void AddBasicAuthHeader(HttpClient client)
    {
        var credentials = "viewer:DD888324-9217-41D1-85D9-20D844090106";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
    }

    [Fact]
    public async Task GetAll()
    {
        await SeedDbAsync();
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        // ...existing code...
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entities = await response.Content.ReadFromJsonAsync<dynamic[]>();
        Assert.NotNull(entities);
        Assert.True(entities.Length >= 2);
    }

    [Fact]
    public async Task GetById()
    {
        await SeedDbAsync();
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var entity = CmsEntityDbSeeder.SeedEntities[0];
        // ...existing code...
        var response = await client.GetAsync($"/cms/entities/{entity.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CmsEntityController_RejectsUnauthorizedUser()
    {
        await SeedDbAsync();
        var client = _factory.CreateClient();
        // Use eventuser credentials (should not have EntityViewer/Admin role)
        var credentials = "eventuser:9A01D9BF-A5B5-45D4-BE41-618B0F11D6CF";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CmsEntityController_RejectsInvalidPassword()
    {
        await SeedDbAsync();
        var client = _factory.CreateClient();
        // Use wrong password for viewer
        var credentials = "viewer:wrongpassword";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
