using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CmsSyncService.Api.Tests.TestFixtures;
using CmsSyncService.Infrastructure.Persistence;

namespace CmsSyncService.Api.Tests;

[
Collection("DbWriteTests")]
public class CmsEventWebhookControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CmsEventWebhookControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CmsSyncDbContext>();
        CmsEntityDbSeeder.ClearAsync(dbContext).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task IngestEventsPostWebhook_Publish_Modify_Unpublish_Scenario()
    {
        var client = _factory.CreateClient();
        AddCmsAuthHeader(client);
        // Step 1: Publish v1
        var publishEvents = new[]
        {
            new {
                type = "publish",
                id = "test-entity-scenario",
                payload = new { foo = "v1" },
                version = 1,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:00:00Z")
            }
        };
        var publishJson = JsonSerializer.Serialize(publishEvents);
        var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");
        var publishResponse = await client.PostAsync("/cms/events", publishContent);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        // Step 2: Modify to v2 in CMS (simulate by sending a publish event with new payload, but do not publish)
        // In this system, modification without publishing is not a direct event, so we simulate by updating the payload in the next unpublish event.

        // Step 3: Unpublish v2 (with new payload and version)
        var unpublishEvents = new[]
        {
            new {
                type = "unpublish",
                id = "test-entity-scenario",
                payload = new { foo = "v2-modified" },
                version = 2,
                timestamp = DateTimeOffset.Parse("2026-04-01T12:10:00Z")
            }
        };
        var unpublishJson = JsonSerializer.Serialize(unpublishEvents);
        var unpublishContent = new StringContent(unpublishJson, Encoding.UTF8, "application/json");
        var unpublishResponse = await client.PostAsync("/cms/events", unpublishContent);
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);

        // Step 4: Fetch the entity and verify it is unpublished and has the v2 payload
        AddAdminAuthHeader(client);
        var getResponse = await client.GetAsync("/cms/entities/test-entity-scenario");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var jsonString = await getResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;
        // Defensive: handle both string and object payload
        var payloadElement = root.GetProperty("payload");
        if (payloadElement.ValueKind == JsonValueKind.Object)
        {
            Assert.Equal("v2-modified", payloadElement.GetProperty("foo").GetString());
        }
        else if (payloadElement.ValueKind == JsonValueKind.String)
        {
            // If payload is a string containing JSON, parse it
            var payloadJson = payloadElement.GetString();
            Assert.False(string.IsNullOrEmpty(payloadJson));
            using var payloadDoc = JsonDocument.Parse(payloadJson!);
            var fooValue = payloadDoc.RootElement.GetProperty("foo").GetString();
            Assert.Equal("v2-modified", fooValue);
        }
        else
        {
            // Log for debugging
            throw new InvalidOperationException($"Unexpected payload type: {payloadElement.ValueKind}, value: {payloadElement}");
        }
        Assert.False(root.GetProperty("published").GetBoolean());
        Assert.Equal(2, root.GetProperty("version").GetInt32());
    }
    private static void AddCmsAuthHeader(HttpClient client)
    {
        var credentials = "cms-event-user:9A01D9BF-A5B5-45D4-BE41-618B0F11D6CF";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
    }

    private static void AddAdminAuthHeader(HttpClient client)
    {
        var credentials = "admin:7FDD33AD-3FD3-41B8-AC05-5A9122ABC086";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
    }

    [Fact]
    public async Task IngestEventsPostWebhook_MissingType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        AddCmsAuthHeader(client);
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
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
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
        // Use wrong password for the CMS user
        var credentials = "cms-event-user:wrongpassword";
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

    [Fact]
    public async Task CmsEventWebhookController_RejectsAdminUser()
    {
        var client = _factory.CreateClient();
        AddAdminAuthHeader(client);
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
}
