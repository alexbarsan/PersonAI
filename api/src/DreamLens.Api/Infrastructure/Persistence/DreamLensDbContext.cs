using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContext(DbContextOptions<DreamLensDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
