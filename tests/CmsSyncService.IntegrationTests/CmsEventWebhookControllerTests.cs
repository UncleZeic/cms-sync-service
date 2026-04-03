using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CmsSyncService.Api.Tests;

[Collection("DbWriteTests")]
public class CmsEventWebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CmsEventWebhookControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static void AddBasicAuthHeader(HttpClient client)
    {
        var credentials = "admin:7FDD33AD-3FD3-41B8-AC05-5A9122ABC086";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_MissingType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var events = new[]
        {
            new {
                // type missing
                id = "test-entity-1",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        catch
        {
            Console.WriteLine($"Test failed. Response body: {body}");
            throw;
        }
    }

    [Fact]
    public async Task IngestEventsPostWebhook_InvalidType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var events = new[]
        {
            new {
                type = "invalid",
                id = "test-entity-1",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_MissingPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var events = new[]
        {
            new {
                type = "publish",
                id = "test-entity-1",
                // payload missing
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_MissingVersion_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var events = new[]
        {
            new {
                type = "publish",
                id = "test-entity-1",
                payload = new { foo = "bar" },
                // version missing
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_Publish_Succeeds()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        var events = new[]
        {
            new {
                type = "publish",
                id = "test-entity-1",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_Unpublish_Succeeds()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        // First publish
        var publishEvents = new[]
        {
            new {
                type = "publish",
                id = "test-entity-2",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var publishJson = JsonSerializer.Serialize(publishEvents);
        var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");
        var publishResponse = await client.PostAsync("/cms/events", publishContent);
        var x = await publishResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        // Then unpublish
        var unpublishEvents = new[]
        {
            new {
                type = "unpublish",
                id = "test-entity-2",
                payload = new { foo = "bar" },
                version = 2,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:05:00Z")
            }
        };
        var unpublishJson = JsonSerializer.Serialize(unpublishEvents);
        var unpublishContent = new StringContent(unpublishJson, Encoding.UTF8, "application/json");
        var unpublishResponse = await client.PostAsync("/cms/events", unpublishContent);
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_Delete_Succeeds()
    {
        var client = _factory.CreateClient();
        AddBasicAuthHeader(client);
        // First publish
        var publishEvents = new[]
        {
            new {
                type = "publish",
                id = "test-entity-3",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var publishJson = JsonSerializer.Serialize(publishEvents);
        var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");
        var publishResponse = await client.PostAsync("/cms/events", publishContent);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        // Then delete
        var deleteEvents = new[]
        {
            new {
                type = "delete",
                id = "test-entity-3",
                version = 2,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:10:00Z")
            }
        };
        var deleteJson = JsonSerializer.Serialize(deleteEvents);
        var deleteContent = new StringContent(deleteJson, Encoding.UTF8, "application/json");
        var deleteResponse = await client.PostAsync("/cms/events", deleteContent);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CmsEventWebhookController_RejectsUnauthorizedUser()
    {
        var client = _factory.CreateClient();
        // Use viewer credentials (should not have EventUpdater/Admin role)
        var credentials = "viewer:DD888324-9217-41D1-85D9-20D844090106";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var events = new[]
        {
            new {
                type = "publish",
                id = "test-entity-1",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CmsEventWebhookController_RejectsInvalidPassword()
    {
        var client = _factory.CreateClient();
        // Use wrong password for admin
        var credentials = "admin:wrongpassword";
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
        var events = new[]
        {
            new {
                type = "publish",
                id = "test-entity-1",
                payload = new { foo = "bar" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var json = JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

[CollectionDefinition("DbWriteTests", DisableParallelization = true)]
public class DbWriteTestsCollection : ICollectionFixture<object> { }