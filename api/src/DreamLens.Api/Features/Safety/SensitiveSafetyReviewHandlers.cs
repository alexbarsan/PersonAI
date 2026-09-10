using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Safety;

public sealed class ListSensitiveSafetyReviewsHandler(DreamLensDbContext dbContext)
{
    public async Task<SensitiveSafetyReviewResponse[]> HandleAsync(string? status, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = dbContext.SensitiveDreamSafetyEvents.AsNoTracking()
            .Where(review => review.ExpiresAt > now);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(review => review.Status == status.Trim().ToLowerInvariant());
        }

        return await query.OrderByDescending(review => review.DetectedAt)
            .Select(review => new SensitiveSafetyReviewResponse(
                review.Id,
                review.DreamId,
                review.SubjectPseudonym,
                review.Category,
                review.Confidence,
                review.Severity,
                review.RestrictsElaboration,
                review.Status,
                review.DetectedAt,
                review.ExpiresAt))
            .ToArrayAsync(cancellationToken);
    }
}

public sealed class AcknowledgeSensitiveSafetyReviewHandler(DreamLensDbContext dbContext)
{
    public async Task<SensitiveSafetyReviewResponse?> HandleAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var review = await dbContext.SensitiveDreamSafetyEvents.SingleOrDefaultAsync(review => review.Id == eventId, cancellationToken);
        if (review is null || review.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        review.Status = SensitiveSafetyEventStatuses.Acknowledged;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SensitiveSafetyReviewResponse(
            review.Id, review.DreamId, review.SubjectPseudonym, review.Category, review.Confidence,
            review.Severity, review.RestrictsElaboration, review.Status, review.DetectedAt, review.ExpiresAt);
    }
}

public sealed class GetSensitiveSafetyReviewRawHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor)
{
    public async Task<SensitiveSafetyRawAccessResponse?> HandleAsync(
        Guid eventId,
        SensitiveSafetyRawAccessRequest request,
        CancellationToken cancellationToken)
    {
        var purpose = request.Purpose?.Trim();
        if (string.IsNullOrWhiteSpace(purpose) || purpose.Length is < 10 or > 200)
        {
            throw new ArgumentException("A review purpose between 10 and 200 characters is required.", nameof(request));
        }

        var review = await dbContext.SensitiveDreamSafetyEvents.SingleOrDefaultAsync(review => review.Id == eventId, cancellationToken);
        if (review is null || review.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        dbContext.SensitiveReviewAccessAudits.Add(new SensitiveReviewAccessAudit
        {
            SafetyEventId = review.Id,
            ReviewerSubject = currentUser.Subject,
            Purpose = purpose
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SensitiveSafetyRawAccessResponse(review.Id, review.DreamId, encryptor.Decrypt(review.EncryptedDreamText), review.ExpiresAt);
    }
}
