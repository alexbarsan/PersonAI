using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Content;

public sealed class GetDailyDreamContentHandler(
    DreamLensDbContext dbContext,
    DailyDreamContentSeeder seeder)
{
    public async Task<DailyDreamContentResponse> HandleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        await seeder.EnsureCoverageAsync(date, cancellationToken);
        var content = await dbContext.DailyDreamContent
            .AsNoTracking()
            .SingleAsync(item => item.ContentDate == date, cancellationToken);

        return new DailyDreamContentResponse(
            content.ContentDate,
            content.Quote,
            content.Attribution,
            JsonSerializer.Deserialize<string[]>(content.FactsJson) ?? [],
            JsonSerializer.Deserialize<string[]>(content.CognitiveFactsJson) ?? []);
    }
}
