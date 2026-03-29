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

    [Fact]
    public async Task IngestEventsPostWebhook()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/cms/events", content);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
