using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DreamLensDbContext))]
public sealed class DreamLensDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.12")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("DreamLens.Api.Infrastructure.Persistence.AiCostLedgerRecord", entity =>
        {
            entity.Property<Guid>("Id")
                .HasColumnType("uuid");

            entity.Property<int>("AttemptCount")
                .HasColumnType("integer");

            entity.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            entity.Property<Guid?>("DreamId")
                .HasColumnType("uuid");

            entity.Property<decimal>("EstimatedCostUsd")
                .HasPrecision(18, 9)
                .HasColumnType("numeric(18,9)");

            entity.Property<string>("FailureKind")
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<int?>("InputTokens")
                .HasColumnType("integer");

            entity.Property<long>("LatencyMilliseconds")
                .HasColumnType("bigint");

            entity.Property<string>("Model")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entity.Property<int?>("OutputTokens")
                .HasColumnType("integer");

            entity.Property<string>("PersonaId")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entity.Property<string>("Provider")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.Property<int?>("TotalTokens")
                .HasColumnType("integer");

            entity.Property<string>("UserSubject")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entity.HasKey("Id");

            entity.HasIndex("DreamId");

            entity.HasIndex("UserSubject", "CreatedAt");

            entity.ToTable("AiCostLedger");
        });

        modelBuilder.Entity("DreamLens.Api.Infrastructure.Persistence.DreamRecord", entity =>
        {
            entity.Property<Guid>("Id")
                .HasColumnType("uuid");

            entity.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("ErrorMessage")
                .HasColumnType("text");

            entity.Property<string>("Mood")
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<string>("OccurredAt")
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.Property<string>("ResultJson")
                .HasColumnType("text");

            entity.Property<int?>("SleepQuality")
                .HasColumnType("integer");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            entity.Property<string>("TagsJson")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<string>("Text")
                .IsRequired()
                .HasMaxLength(4000)
                .HasColumnType("character varying(4000)");

            entity.Property<string>("UserSubject")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entity.HasKey("Id");

            entity.HasIndex("UserSubject", "CreatedAt");

            entity.ToTable("Dreams");
        });

        modelBuilder.Entity("DreamLens.Api.Infrastructure.Persistence.SchemaMarker", entity =>
        {
            entity.Property<Guid>("Id")
                .HasColumnType("uuid");

            entity.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entity.HasKey("Id");

            entity.ToTable("SchemaMarkers");
        });

        modelBuilder.Entity("DreamLens.Api.Infrastructure.Persistence.UserProfile", entity =>
        {
            entity.Property<Guid>("Id")
                .HasColumnType("uuid");

            entity.Property<int?>("Age")
                .HasColumnType("integer");

            entity.Property<bool>("ConsentAiProcessing")
                .HasColumnType("boolean");

            entity.Property<bool>("ConsentHistoryUse")
                .HasColumnType("boolean");

            entity.Property<bool>("ConsentSensitiveTraits")
                .HasColumnType("boolean");

            entity.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("EncryptedTraitsJson")
                .IsRequired()
                .HasColumnType("text");

            entity.Property<string>("GenderIdentity")
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entity.Property<string>("Language")
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnType("character varying(16)");

            entity.Property<string>("Sex")
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entity.Property<string>("Timezone")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entity.Property<DateTimeOffset>("UpdatedAt")
                .HasColumnType("timestamp with time zone");

            entity.Property<string>("UserSubject")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entity.HasKey("Id");

            entity.HasIndex("UserSubject")
                .IsUnique();

            entity.ToTable("UserProfiles");
        });
    }
}
