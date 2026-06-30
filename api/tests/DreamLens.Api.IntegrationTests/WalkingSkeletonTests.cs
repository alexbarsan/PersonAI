using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DreamLens.Api.IntegrationTests;

public sealed class WalkingSkeletonTests
{
    [Fact]
    public async Task LiveHealthEndpointReturnsOk()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyHealthEndpointReturnsOk()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocumentIsExposedInDevelopment()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"openapi\"", body);
        Assert.Contains("\"/health/live\"", body);
        Assert.Contains("\"/health/ready\"", body);
    }

    private static HttpClient CreateDevelopmentClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

        return factory.CreateClient();
    }
}
