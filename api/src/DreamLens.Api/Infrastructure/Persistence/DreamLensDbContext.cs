using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContext(DbContextOptions<DreamLensDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

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
    }
}
