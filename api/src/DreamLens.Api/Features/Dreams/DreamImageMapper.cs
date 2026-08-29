using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Dreams;

internal static class DreamImageMapper
{
    public static DreamImageResponse Map(DreamImageRecord image, Guid? jobId, string? downloadUrl)
    {
        return new DreamImageResponse(
            image.Id,
            image.DreamId,
            image.Status,
            image.Style,
            jobId,
            downloadUrl,
            image.ErrorMessage,
            image.CreatedAt);
    }
}
