using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DreamLens.Api.IntegrationTests;

public sealed class MeEndpointTests
{
    [Fact]
    public async Task MeReturnsUnauthorizedWithoutAuthentication()
    {
        using var client = CreateClient("Testing");

        var response = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeReturnsCurrentUserForAuthenticatedTestToken()
    {
        using var client = CreateClient("Testing");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "test-cognito-sub");
        client.DefaultRequestHeaders.Add("X-Test-Email", "dreamer@example.test");
        client.DefaultRequestHeaders.Add("X-Test-Name", "Test Dreamer");

        var response = await client.GetAsync("/v1/me");
        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("test-cognito-sub", body.Subject);
        Assert.Equal("dreamer@example.test", body.Email);
        Assert.Equal("Test Dreamer", body.DisplayName);
        Assert.Equal("Test", body.AuthenticationScheme);
    }

    [Fact]
    public async Task TestAuthenticationHeadersAreIgnoredOutsideTesting()
    {
        using var client = CreateClient("Production");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "test-cognito-sub");

        var response = await client.GetAsync("/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient CreateClient(string environment)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));

        return factory.CreateClient();
    }

    private sealed record MeResponse(
        string Subject,
        string? Email,
        string? DisplayName,
        string AuthenticationScheme);
}
