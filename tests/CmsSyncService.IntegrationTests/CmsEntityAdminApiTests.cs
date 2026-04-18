using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Api.Tests.TestFixtures;


namespace CmsSyncService.Api.Tests;

[Collection("DbWriteTests")]
public class CmsEntityAdminApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CmsEntityAdminApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsSyncService.Infrastructure.Persistence.CmsSyncDbContext>();
        CmsEntityDbSeeder.ClearAsync(dbContext).GetAwaiter().GetResult();
        CmsEntityDbSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
    }

    private async Task<(HttpClient client, string entityId)> CreateClientAndGetEntityIdAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var credentials = $"{username}:{password}";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        // Use seeded entity for test
        var entityId = CmsEntityDbSeeder.SeedEntities[0].Id;
        return (client, entityId);
    }

    [Fact]
    public async Task Admin_Can_Set_AdminDisabled_Flag()
    {
        var (client, entityId) = await CreateClientAndGetEntityIdAsync("admin", "7FDD33AD-3FD3-41B8-AC05-5A9122ABC086");
        var patchResponse = await client.PatchAsJsonAsync($"/cms/entities/{entityId}", new AdminDisabledDto { AdminDisabled = true });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var getByIdResponse = await client.GetAsync($"/cms/entities/{entityId}");
        getByIdResponse.EnsureSuccessStatusCode();
        var raw = await getByIdResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"RESPONSE BODY: {raw}");
        var entity = await getByIdResponse.Content.ReadFromJsonAsync<CmsEntityAdminDto>();
        Assert.NotNull(entity);
        Assert.True(entity.AdminDisabled, "Admin Disabled flag should be true after patching");
    }

    [Fact]
    public async Task NonAdmin_Cannot_Set_AdminDisabled_Flag()
    {
        var (client, entityId) = await CreateClientAndGetEntityIdAsync("viewer", "DD888324-9217-41D1-85D9-20D844090106");
        var patchResponse = await client.PatchAsJsonAsync($"/cms/entities/{entityId}", new AdminDisabledDto { AdminDisabled = true });
        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
    }
}

[CollectionDefinition("DbWriteTests", DisableParallelization = true)]
public class DbWriteTestsCollection : ICollectionFixture<object> { }
