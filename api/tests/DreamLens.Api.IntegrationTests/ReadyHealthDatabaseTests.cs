using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DreamLens.Api.IntegrationTests;

public sealed class ReadyHealthDatabaseTests
{
    [Fact]
    public async Task ReadyHealthEndpointReturnsUnavailableWhenConfiguredDatabaseCannotConnect()
    {
        using var client = CreateClientWithConnectionString(
            "Host=127.0.0.1;Port=1;Database=dreamlens;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task ReadyHealthEndpointReturnsOkWhenConfiguredDatabaseCanConnect()
    {
        await using var postgres = new PostgresContainerFixture();
        await postgres.InitializeAsync();

        using var client = CreateClientWithConnectionString(postgres.ConnectionString);

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient CreateClientWithConnectionString(string connectionString)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DreamLensDb"] = connectionString
                    });
                });
            });

        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        return client;
    }
}
