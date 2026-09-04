using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Jobs;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContext(DbContextOptions<DreamLensDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<DreamRecord> Dreams => Set<DreamRecord>();

    public DbSet<DreamInterpretationFeedback> DreamInterpretationFeedback => Set<DreamInterpretationFeedback>();

    public DbSet<DreamFactRecord> DreamFacts => Set<DreamFactRecord>();

    public DbSet<DreamImageRecord> DreamImages => Set<DreamImageRecord>();

    public DbSet<VoiceCaptureRecord> VoiceCaptures => Set<VoiceCaptureRecord>();

    public DbSet<AiCostLedgerRecord> AiCostLedger => Set<AiCostLedgerRecord>();

    public DbSet<DreamEmbedding> DreamEmbeddings => Set<DreamEmbedding>();

    public DbSet<AsyncJobRecord> AsyncJobs => Set<AsyncJobRecord>();

    public DbSet<AnonymizationRequest> AnonymizationRequests => Set<AnonymizationRequest>();

    public DbSet<AnonymizedUserTombstone> AnonymizedUserTombstones => Set<AnonymizedUserTombstone>();

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
            entity.Property(dream => dream.JournalNote)
                .HasMaxLength(2000);
            entity.Property(dream => dream.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<DreamInterpretationFeedback>(entity =>
        {
            entity.ToTable("DreamInterpretationFeedback");
            entity.HasKey(feedback => feedback.Id);
            entity.HasIndex(feedback => feedback.DreamId).IsUnique();
            entity.HasIndex(feedback => new { feedback.UserSubject, feedback.UpdatedAt });
            entity.Property(feedback => feedback.UserSubject).HasMaxLength(256).IsRequired();
            entity.Property(feedback => feedback.Rating).HasMaxLength(16).IsRequired();
            entity.Property(feedback => feedback.ReasonsJson).IsRequired();
            entity.Property(feedback => feedback.Details).HasMaxLength(1000);
            entity.Property(feedback => feedback.CreatedAt).IsRequired();
            entity.Property(feedback => feedback.UpdatedAt).IsRequired();
            entity.HasOne<DreamRecord>()
                .WithOne()
                .HasForeignKey<DreamInterpretationFeedback>(feedback => feedback.DreamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DreamFactRecord>(entity =>
        {
            entity.ToTable("DreamFacts");
            entity.HasKey(fact => fact.Id);
            entity.HasIndex(fact => new { fact.DreamId, fact.FactType, fact.NormalizedValue })
                .IsUnique();
            entity.HasIndex(fact => new { fact.UserSubject, fact.FactType, fact.NormalizedValue });
            entity.HasIndex(fact => new { fact.UserSubject, fact.CreatedAt });

            entity.Property(fact => fact.UserSubject).HasMaxLength(256).IsRequired();
            entity.Property(fact => fact.FactType).HasMaxLength(64).IsRequired();
            entity.Property(fact => fact.NormalizedValue).HasMaxLength(256).IsRequired();
            entity.Property(fact => fact.DisplayValue).HasMaxLength(256).IsRequired();
            entity.Property(fact => fact.Score).HasPrecision(5, 4);
            entity.Property(fact => fact.ExtractionConfidence).HasPrecision(5, 4);
            entity.Property(fact => fact.SourceSchemaVersion).HasMaxLength(16).IsRequired();
            entity.Property(fact => fact.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<DreamImageRecord>(entity =>
        {
            entity.ToTable("DreamImages");
            entity.HasKey(image => image.Id);
            entity.HasIndex(image => new { image.DreamId, image.UserSubject, image.CreatedAt });
            entity.HasIndex(image => new { image.UserSubject, image.CreatedAt });
            entity.Property(image => image.UserSubject).HasMaxLength(256).IsRequired();
            entity.Property(image => image.Status).HasMaxLength(32).IsRequired();
            entity.Property(image => image.Style).HasMaxLength(64).IsRequired();
            entity.Property(image => image.AssetKey).HasMaxLength(512);
            entity.Property(image => image.ErrorMessage).HasMaxLength(2000);
            entity.Property(image => image.CreatedAt).IsRequired();
            entity.Property(image => image.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<VoiceCaptureRecord>(entity =>
        {
            entity.ToTable("VoiceCaptures");
            entity.HasKey(capture => capture.Id);
            entity.HasIndex(capture => new { capture.UserSubject, capture.CreatedAt });
            entity.HasIndex(capture => new { capture.UserSubject, capture.Status, capture.CreatedAt });
            entity.Property(capture => capture.UserSubject).HasMaxLength(256).IsRequired();
            entity.Property(capture => capture.Status).HasMaxLength(32).IsRequired();
            entity.Property(capture => capture.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(capture => capture.Language).HasMaxLength(32);
            entity.Property(capture => capture.SourceAssetKey).HasMaxLength(512).IsRequired();
            entity.Property(capture => capture.Transcript).HasMaxLength(8000);
            entity.Property(capture => capture.ErrorMessage).HasMaxLength(2000);
            entity.Property(capture => capture.CreatedAt).IsRequired();
            entity.Property(capture => capture.UpdatedAt).IsRequired();
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
            entity.Property(row => row.OperationType)
                .HasMaxLength(64)
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

        modelBuilder.Entity<AsyncJobRecord>(entity =>
        {
            entity.ToTable("AsyncJobs");
            entity.HasKey(job => job.Id);
            entity.HasIndex(job => job.IdempotencyKey).IsUnique();
            entity.HasIndex(job => new { job.Status, job.AvailableAt });
            entity.HasIndex(job => new { job.UserSubject, job.CreatedAt });
            entity.HasIndex(job => new { job.JobType, job.TargetId });
            entity.Property(job => job.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(job => job.JobType).HasMaxLength(64).IsRequired();
            entity.Property(job => job.UserSubject).HasMaxLength(256).IsRequired();
            entity.Property(job => job.PayloadJson).IsRequired();
            entity.Property(job => job.Status).HasMaxLength(32).IsRequired();
            entity.Property(job => job.LastError).HasMaxLength(2000);
            entity.Property(job => job.CreatedAt).IsRequired();
            entity.Property(job => job.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<AnonymizationRequest>(entity =>
        {
            entity.ToTable("AnonymizationRequests");
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.RequestingUserSubject, request.Status });
            entity.HasIndex(request => new { request.Status, request.RequestedAt });
            entity.Property(request => request.RequestingUserSubject).HasMaxLength(256);
            entity.Property(request => request.RequesterPseudonym).HasMaxLength(64).IsRequired();
            entity.Property(request => request.Status).HasMaxLength(32).IsRequired();
            entity.Property(request => request.ReviewedBySubject).HasMaxLength(256);
            entity.Property(request => request.RequestedAt).IsRequired();
        });

        modelBuilder.Entity<AnonymizedUserTombstone>(entity =>
        {
            entity.ToTable("AnonymizedUserTombstones");
            entity.HasKey(tombstone => tombstone.Id);
            entity.HasIndex(tombstone => tombstone.SubjectPseudonym).IsUnique();
            entity.Property(tombstone => tombstone.SubjectPseudonym).HasMaxLength(64).IsRequired();
            entity.Property(tombstone => tombstone.AnonymizedAt).IsRequired();
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
