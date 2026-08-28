using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContext(DbContextOptions<DreamLensDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<DreamRecord> Dreams => Set<DreamRecord>();

    public DbSet<AiCostLedgerRecord> AiCostLedger => Set<AiCostLedgerRecord>();

    public DbSet<DreamEmbedding> DreamEmbeddings => Set<DreamEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<SchemaMarker>(entity =>
        {
            entity.ToTable("SchemaMarkers");
            entity.HasKey(marker => marker.Id);
            entity.Property(marker => marker.Name)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(marker => marker.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserSubject)
                .IsUnique();

            entity.Property(profile => profile.UserSubject)
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(profile => profile.Sex)
                .HasMaxLength(64);
            entity.Property(profile => profile.GenderIdentity)
                .HasMaxLength(128);
            entity.Property(profile => profile.Language)
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(profile => profile.Timezone)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(profile => profile.EncryptedTraitsJson)
                .IsRequired();
            entity.Property(profile => profile.CreatedAt)
                .IsRequired();
            entity.Property(profile => profile.UpdatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<DreamRecord>(entity =>
        {
            entity.ToTable("Dreams");
            entity.HasKey(dream => dream.Id);
            entity.HasIndex(dream => new { dream.UserSubject, dream.CreatedAt });

            entity.Property(dream => dream.UserSubject)
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(dream => dream.Text)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(dream => dream.Mood)
                .HasMaxLength(64);
            entity.Property(dream => dream.TagsJson)
                .IsRequired();
            entity.Property(dream => dream.OccurredAt)
                .HasMaxLength(32);
            entity.Property(dream => dream.Status)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(dream => dream.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<AiCostLedgerRecord>(entity =>
        {
            entity.ToTable("AiCostLedger");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => new { row.UserSubject, row.CreatedAt });
            entity.HasIndex(row => row.DreamId);

            entity.Property(row => row.UserSubject)
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(row => row.Provider)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(row => row.Model)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(row => row.PersonaId)
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(row => row.Status)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(row => row.FailureKind)
                .HasMaxLength(64);
            entity.Property(row => row.EstimatedCostUsd)
                .HasPrecision(18, 9);
            entity.Property(row => row.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<DreamEmbedding>(entity =>
        {
            entity.ToTable("DreamEmbeddings");
            entity.HasKey(embedding => embedding.Id);
            entity.HasIndex(embedding => embedding.DreamId).IsUnique();
            entity.HasIndex(embedding => new { embedding.UserSubject, embedding.CreatedAt });
            entity.Property(embedding => embedding.UserSubject).HasMaxLength(256).IsRequired();
            var embeddingProperty = entity.Property(embedding => embedding.Embedding).IsRequired();
            if (Database.IsNpgsql())
            {
                embeddingProperty.HasColumnType("vector(1024)");
            }
            else
            {
                embeddingProperty.HasConversion(
                    value => SerializeVector(value),
                    value => DeserializeVector(value));
            }
            entity.Property(embedding => embedding.Provider).HasMaxLength(64).IsRequired();
            entity.Property(embedding => embedding.Model).HasMaxLength(128).IsRequired();
            entity.Property(embedding => embedding.Version).HasMaxLength(32).IsRequired();
            entity.Property(embedding => embedding.CreatedAt).IsRequired();
        });
    }

    private static string SerializeVector(Pgvector.Vector value)
    {
        return JsonSerializer.Serialize(value.ToArray(), new JsonSerializerOptions());
    }

    private static Pgvector.Vector DeserializeVector(string value)
    {
        return new Pgvector.Vector(JsonSerializer.Deserialize<float[]>(value, new JsonSerializerOptions()) ?? Array.Empty<float>());
    }
}
