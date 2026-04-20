using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Json;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Api.Tests.TestFixtures;
using CmsSyncService.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CmsSyncService.Api.Tests;

[Collection("DbWriteTests")]
public class CmsEntityControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly List<CmsSyncService.Domain.CmsEntity> _seedEntities;

    private sealed class EntityListResponse
    {
        public List<CmsEntityDto> Items { get; init; } = [];

        public int Total { get; init; }

        public int Skip { get; init; }

        public int Take { get; init; }
    }

    public CmsEntityControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsSyncDbContext>();
        CmsEntityDbSeeder.ClearAsync(dbContext).GetAwaiter().GetResult();
        _seedEntities = CmsEntityDbSeeder.SeedAsync(dbContext).GetAwaiter().GetResult();
    }

    private static void AddBasicAuthHeader(HttpClient client)
    {
        var credentials = "viewer:DD888324-9217-41D1-85D9-20D844090106";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
    }

    private static void AddAdminAuthHeader(HttpClient client)
    {
        var credentials = "admin:7FDD33AD-3FD3-41B8-AC05-5A9122ABC086";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
    }

    [Fact]
    public async Task GetAll()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        // Should get only the 2 published entities (not the 1 unpublished)
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Total);
        Assert.Equal(0, page.Skip);
        Assert.Equal(100, page.Take);

        // Test pagination: take=1
        response = await client.GetAsync("/cms/entities?take=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Total);
        Assert.Equal(0, page.Skip);
        Assert.Equal(1, page.Take);

        // Test pagination: skip=1, take=1
        response = await client.GetAsync("/cms/entities?skip=1&take=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Total);
        Assert.Equal(1, page.Skip);
        Assert.Equal(1, page.Take);
    }

    [Fact]
    public async Task GetAll_AsAdmin_GetsAllEntities()
    {
        var client = _factory.CreateClient();
        // Admin credentials (from README.md)
        var credentials = "admin:7FDD33AD-3FD3-41B8-AC05-5A9122ABC086";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        // Should get all 3 entities (2 published, 1 unpublished)
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(3, page.Total);

        // Test pagination: take=2
        response = await client.GetAsync("/cms/entities?take=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.Total);
        Assert.Equal(0, page.Skip);
        Assert.Equal(2, page.Take);

        // Test pagination: skip=2, take=2
        response = await client.GetAsync("/cms/entities?skip=2&take=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Skip);
        Assert.Equal(2, page.Take);
    }

    [Fact]
    public async Task GetAll_AsViewer_GetsOnlyPublishedEntities()
    {
        var client = _factory.CreateClient();
        // Viewer credentials
        var credentials = "viewer:DD888324-9217-41D1-85D9-20D844090106";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        // Should get only the 2 published entities
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Total);
    }

    [Fact]
    public async Task GetById()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var entity = _seedEntities[0];
        var response = await client.GetAsync($"/cms/entities/{entity.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AsViewer_UnpublishedEntity_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var unpublishedEntity = _seedEntities[2];

        var response = await client.GetAsync($"/cms/entities/{unpublishedEntity.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AsViewer_AdminDisabledEntity_ReturnsNotFound()
    {
        var adminClient = _factory.CreateClient();
        AddAdminAuthHeader(adminClient);
        var targetEntity = _seedEntities[0];

        var patchResponse = await adminClient.PatchAsJsonAsync($"/cms/entities/{targetEntity.Id}", new AdminDisabledDto { AdminDisabled = true });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var viewerClient = _factory.CreateClient();
        AddBasicAuthHeader(viewerClient);
        var getResponse = await viewerClient.GetAsync($"/cms/entities/{targetEntity.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAll_AsViewer_AdminDisabledPublishedEntity_IsHiddenFromListing()
    {
        var targetEntity = _seedEntities[0];

        var adminClient = _factory.CreateClient();
        AddAdminAuthHeader(adminClient);
        var patchResponse = await adminClient.PatchAsJsonAsync($"/cms/entities/{targetEntity.Id}", new AdminDisabledDto { AdminDisabled = true });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var viewerClient = _factory.CreateClient();
        AddBasicAuthHeader(viewerClient);
        var listResponse = await viewerClient.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var page = await listResponse.Content.ReadFromJsonAsync<EntityListResponse>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Total);
    }

    [Fact]
    public async Task CmsEntityController_RejectsUnauthorizedUser()
    {
        var client = _factory.CreateClient();
        // Use cms-event-user credentials (should not have EntityViewer/Admin role)
        var credentials = "cms-event-user:9A01D9BF-A5B5-45D4-BE41-618B0F11D6CF";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CmsEntityController_RejectsInvalidPassword()
    {
        var client = _factory.CreateClient();
        // Use wrong password for viewer
        var credentials = "viewer:wrongpassword";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
