using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CmsSyncService.Api.Tests;

public class CmsEventWebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CmsEventWebhookControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_MissingType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var json = "[" +
            "{" +
            "  'id': 'test-entity-1'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_InvalidType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var json = "[" +
            "{" +
            "  'type': 'invalid'," +
            "  'id': 'test-entity-1'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_MissingPayload_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var json = "[" +
            "{" +
            "  'type': 'publish'," +
            "  'id': 'test-entity-1'," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_MissingVersion_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var json = "[" +
            "{" +
            "  'type': 'publish'," +
            "  'id': 'test-entity-1'," +
            "  'payload': {'foo': 'bar'}," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_Publish_Succeeds()
    {
        var client = _factory.CreateClient();
        var json = "[" +
            "{" +
            "  'type': 'publish'," +
            "  'id': 'test-entity-1'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_Unpublish_Succeeds()
    {
        var client = _factory.CreateClient();
        // First publish
        var publishJson = "[" +
            "{" +
            "  'type': 'publish'," +
            "  'id': 'test-entity-2'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");
        var publishResponse = await client.PostAsync("/cms/events", publishContent);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        // Then unpublish
        var unpublishJson = "[" +
            "{" +
            "  'type': 'unpublish'," +
            "  'id': 'test-entity-2'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 2," +
            "  'timestamp': '2026-04-01T12:05:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var unpublishContent = new StringContent(unpublishJson, Encoding.UTF8, "application/json");
        var unpublishResponse = await client.PostAsync("/cms/events", unpublishContent);
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);
    }

    [Fact(Skip = "Ignored by request")]
    public async Task IngestEventsPostWebhook_Delete_Succeeds()
    {
        var client = _factory.CreateClient();
        // First publish
        var publishJson = "[" +
            "{" +
            "  'type': 'publish'," +
            "  'id': 'test-entity-3'," +
            "  'payload': {'foo': 'bar'}," +
            "  'version': 1," +
            "  'timestamp': '2026-04-01T12:00:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var publishContent = new StringContent(publishJson, Encoding.UTF8, "application/json");
        var publishResponse = await client.PostAsync("/cms/events", publishContent);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        // Then delete
        var deleteJson = "[" +
            "{" +
            "  'type': 'delete'," +
            "  'id': 'test-entity-3'," +
            "  'version': 2," +
            "  'timestamp': '2026-04-01T12:10:00Z'" +
            "}" +
            "]".Replace("'", "\"");
        var deleteContent = new StringContent(deleteJson, Encoding.UTF8, "application/json");
        var deleteResponse = await client.PostAsync("/cms/events", deleteContent);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}