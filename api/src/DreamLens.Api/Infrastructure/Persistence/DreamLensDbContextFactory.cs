using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContextFactory : IDesignTimeDbContextFactory<DreamLensDbContext>
{
    public DreamLensDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=dreamlens;Username=postgres;Password=postgres")
            .Options;

        return new DreamLensDbContext(options);
    }
}
