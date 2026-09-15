using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetAskDreamMemoryStatusHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<EmbeddingOptions> embeddingOptions)
{
    public async Task<AskDreamMemoryStatusResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var settings = embeddingOptions.Value;
        var completedDreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dream.Status == DreamStatuses.Completed)
            .CountAsync(cancellationToken);
        var indexedDreams = settings.Enabled
            ? await dbContext.DreamEmbeddings
                .AsNoTracking()
                .Where(embedding => embedding.UserSubject == currentUser.Subject
                    && embedding.Model == settings.Model
                    && embedding.Dimensions == settings.Dimensions
                    && embedding.Version == settings.Version)
                .Select(embedding => embedding.DreamId)
                .Distinct()
                .CountAsync(cancellationToken)
            : 0;

        return new AskDreamMemoryStatusResponse(
            settings.Enabled && indexedDreams > 0,
            completedDreams,
            indexedDreams,
            Math.Max(0, completedDreams - indexedDreams),
            ReadMessage(settings.Enabled, completedDreams, indexedDreams));
    }

    private static string ReadMessage(bool embeddingsEnabled, int completedDreams, int indexedDreams)
    {
        if (!embeddingsEnabled) return "Dream memory is temporarily unavailable.";
        if (completedDreams == 0) return "Interpret a dream first, then you can ask about patterns across your journal.";
        if (indexedDreams == 0) return "Your interpreted dreams are still being indexed. This usually takes a moment.";
        return "Your dream memory is ready.";
    }
}
