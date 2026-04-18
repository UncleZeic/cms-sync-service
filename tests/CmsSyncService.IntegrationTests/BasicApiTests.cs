using System.Net;
using System.Threading.Tasks;
using CmsSyncService.Api.Tests.TestFixtures;
using Xunit;

namespace CmsSyncService.Api.Tests;

public class BasicApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BasicApiTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", payload);
    }
}
