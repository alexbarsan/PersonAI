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
    }
}
