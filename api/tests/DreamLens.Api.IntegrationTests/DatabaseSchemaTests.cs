using DreamLens.Api.Features.Content;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;
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

    [Fact]
    public void CognitoSubjectIsUniqueWhileEmailIsOnlySearchMetadata()
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=dreamlens;Username=postgres;Password=postgres",
                npgsql => npgsql.UseVector())
            .Options;
        using var db = new DreamLensDbContext(options);
        var indexes = db.Model.FindEntityType(typeof(UserProfile))!.GetIndexes();

        Assert.True(indexes.Single(index => index.Properties.Single().Name == nameof(UserProfile.UserSubject)).IsUnique);
        Assert.False(indexes.Single(index => index.Properties.Single().Name == nameof(UserProfile.EmailNormalized)).IsUnique);
    }

    [DockerAvailableFact]
    public async Task DistinctCognitoSubjectsCanShareEmailButSameSubjectCannotDuplicate()
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

        await using (var db = new DreamLensDbContext(options))
        {
            db.UserProfiles.AddRange(
                NewProfile("google-sub", "same@example.com"),
                NewProfile("password-sub", "same@example.com"));
            await db.SaveChangesAsync();
        }

        await using var duplicateSubjectDb = new DreamLensDbContext(options);
        duplicateSubjectDb.UserProfiles.Add(NewProfile("google-sub", "different@example.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateSubjectDb.SaveChangesAsync());
    }

    private static UserProfile NewProfile(string subject, string email) => new()
    {
        UserSubject = subject,
        EmailNormalized = email,
        EncryptedTraitsJson = "{}"
    };

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
