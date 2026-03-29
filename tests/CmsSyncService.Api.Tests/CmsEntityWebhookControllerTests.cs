using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CmsSyncService.Api.Tests;

public class CmsEntityControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CmsEntityControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/cms/entities");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetById()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.GetAsync($"/cms/entities/{id}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        
    }
}
