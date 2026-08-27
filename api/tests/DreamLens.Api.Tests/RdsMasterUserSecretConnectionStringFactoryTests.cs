using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace DreamLens.Api.Tests;

public sealed class RdsMasterUserSecretConnectionStringFactoryTests
{
    [Fact]
    public void CreateReturnsNullWhenSecretIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var connectionString = RdsMasterUserSecretConnectionStringFactory.Create(configuration);

        Assert.Null(connectionString);
    }

    [Fact]
    public void CreateBuildsConnectionStringFromRdsSecretJson()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:MasterUserJson"] = """
                {
                  "username": "dreamlens",
                  "password": "secret-password",
                  "host": "dreamlens.example.amazonaws.com",
                  "port": 5432,
                  "dbname": "dreamlens"
                }
                """
            })
            .Build();

        var connectionString = RdsMasterUserSecretConnectionStringFactory.Create(configuration);

        Assert.Contains("Host=dreamlens.example.amazonaws.com", connectionString);
        Assert.Contains("Port=5432", connectionString);
        Assert.Contains("Database=dreamlens", connectionString);
        Assert.Contains("Username=dreamlens", connectionString);
        Assert.Contains("Password=secret-password", connectionString);
    }

    [Fact]
    public void CreateFallsBackToConfiguredEndpointAndDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:MasterUserJson"] = """
                {
                  "username": "dreamlens",
                  "password": "secret-password"
                }
                """,
                ["ConnectionStrings:Host"] = "dreamlens.example.amazonaws.com:5432",
                ["ConnectionStrings:Database"] = "dreamlens"
            })
            .Build();

        var connectionString = RdsMasterUserSecretConnectionStringFactory.Create(configuration);

        Assert.Contains("Host=dreamlens.example.amazonaws.com", connectionString);
        Assert.Contains("Port=5432", connectionString);
        Assert.Contains("Database=dreamlens", connectionString);
    }
}
