using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamLensDbContextFactory : IDesignTimeDbContextFactory<DreamLensDbContext>
{
    public DreamLensDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=dreamlens;Username=postgres;Password=postgres",
                npgsql => npgsql.UseVector())
            .Options;

        return new DreamLensDbContext(options);
    }
}
