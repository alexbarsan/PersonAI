using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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

    [Fact]
    public async Task SwaggerUiIsExposedInDevelopment()
    {
        using var client = CreateDevelopmentClient();

        var response = await client.GetAsync("/swagger");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("DreamLens API Swagger", body);
        Assert.Contains("/openapi/v1.json", body);
        Assert.Contains("swagger-ui", body);
    }

    [Fact]
    public async Task CorsPreflightAllowsConfiguredOrigins()
    {
        using var client = CreateDevelopmentClient(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://dev.dreamdna.world"
        });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/v1/me");
        request.Headers.Add("Origin", "https://dev.dreamdna.world");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://dev.dreamdna.world", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    private static HttpClient CreateDevelopmentClient(Dictionary<string, string?>? configurationValues = null)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                if (configurationValues is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(configurationValues));
                }
            });

        return factory.CreateClient();
    }
}
