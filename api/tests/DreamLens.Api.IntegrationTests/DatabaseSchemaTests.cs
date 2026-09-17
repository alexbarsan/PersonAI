using DreamLens.Api.Features.Content;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace DreamLens.Api.IntegrationTests;

public sealed class DatabaseSchemaTests
{
    [Fact]
    public void DbContextExposesInitialCreateMigration()
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=dreamlens;Username=postgres;Password=postgres",
                npgsql => npgsql.UseVector())
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
            .UseNpgsql(postgres.ConnectionString, npgsql => npgsql.UseVector())
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

    [DockerAvailableFact]
    public async Task DailyDreamContentSeedingIsSafeAcrossConcurrentInstances()
    {
        await using var postgres = new PostgresContainerFixture();
        await postgres.InitializeAsync();

        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql(postgres.ConnectionString, npgsql => npgsql.UseVector())
            .Options;

        await using (var migrationDb = new DreamLensDbContext(options))
        {
            await migrationDb.Database.MigrateAsync();
        }

        await using var firstDb = new DreamLensDbContext(options);
        await using var secondDb = new DreamLensDbContext(options);
        var requestedDate = new DateOnly(2026, 9, 18);

        await Task.WhenAll(
            new DailyDreamContentSeeder(firstDb).EnsureCoverageAsync(requestedDate, CancellationToken.None),
            new DailyDreamContentSeeder(secondDb).EnsureCoverageAsync(requestedDate, CancellationToken.None));

        await using var verificationDb = new DreamLensDbContext(options);
        var rows = await verificationDb.DailyDreamContent.CountAsync();
        var distinctDates = await verificationDb.DailyDreamContent
            .Select(content => content.ContentDate)
            .Distinct()
            .CountAsync();

        Assert.Equal(426, rows);
        Assert.Equal(rows, distinctDates);
    }
}
