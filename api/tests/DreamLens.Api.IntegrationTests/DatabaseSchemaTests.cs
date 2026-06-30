using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.IntegrationTests;

public sealed class DatabaseSchemaTests
{
    [Fact]
    public void DbContextExposesInitialCreateMigration()
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql("Host=localhost;Database=dreamlens;Username=postgres;Password=postgres")
            .Options;

        using var db = new DreamLensDbContext(options);

        Assert.Contains("20260701000000_InitialCreate", db.Database.GetMigrations());
    }

    [DockerAvailableFact]
    public async Task MigrationsApplyAndSchemaMarkerCanRoundTrip()
    {
        await using var postgres = new PostgresContainerFixture();
        await postgres.InitializeAsync();

        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        await using var db = new DreamLensDbContext(options);
        await db.Database.MigrateAsync();

        var marker = new SchemaMarker
        {
            Name = "s2-persistence-smoke",
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.SchemaMarkers.Add(marker);
        await db.SaveChangesAsync();

        var saved = await db.SchemaMarkers.SingleAsync(x => x.Id == marker.Id);

        Assert.Equal("s2-persistence-smoke", saved.Name);
    }
}
