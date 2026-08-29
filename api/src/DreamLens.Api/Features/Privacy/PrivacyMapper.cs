using DreamLens.Api.Infrastructure.Persistence;

namespace DreamLens.Api.Features.Privacy;

internal static class PrivacyMapper
{
    public static AnonymizationRequestResponse Map(AnonymizationRequest request) => new(
        request.Id,
        request.Status,
        request.RequestedAt,
        request.ReviewedAt,
        request.CompletedAt);
}
