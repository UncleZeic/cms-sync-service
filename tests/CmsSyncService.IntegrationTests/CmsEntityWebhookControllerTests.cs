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

    [Fact]
    public async Task GetAll()
    {
        await SeedDbAsync();
        var client = _factory.CreateClient();
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
        var entity = CmsEntityDbSeeder.SeedEntities[0];
        var response = await client.GetAsync($"/cms/entities/{entity.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
