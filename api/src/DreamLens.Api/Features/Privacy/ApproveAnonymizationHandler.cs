using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;

namespace DreamLens.Api.Features.Privacy;

public sealed class ApproveAnonymizationHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IPseudonymService pseudonymService,
    IPrivateAssetStore assetStore)
{
    public async Task<AnonymizationRequestResponse?> HandleAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await dbContext.AnonymizationRequests.SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);
        if (request is null)
        {
            return null;
        }

        if (request.Status == AnonymizationRequestStatuses.Approved)
        {
            return PrivacyMapper.Map(request);
        }

        var subject = request.RequestingUserSubject;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("The anonymization request has no active user subject.");
        }

        var imageKeys = await dbContext.DreamImages
            .Where(image => image.UserSubject == subject && image.AssetKey != null)
            .Select(image => image.AssetKey!)
            .ToArrayAsync(cancellationToken);
        var voiceKeys = await dbContext.VoiceCaptures
            .Where(capture => capture.UserSubject == subject && capture.RetainRecording)
            .Select(capture => capture.SourceAssetKey)
            .ToArrayAsync(cancellationToken);
        foreach (var key in imageKeys.Concat(voiceKeys))
        {
            await assetStore.DeleteAsync(key, cancellationToken);
        }

        var profiles = await dbContext.UserProfiles.Where(profile => profile.UserSubject == subject).ToArrayAsync(cancellationToken);
        var dreams = await dbContext.Dreams.Where(dream => dream.UserSubject == subject).ToArrayAsync(cancellationToken);
        var facts = await dbContext.DreamFacts.Where(fact => fact.UserSubject == subject).ToArrayAsync(cancellationToken);
        var embeddings = await dbContext.DreamEmbeddings.Where(embedding => embedding.UserSubject == subject).ToArrayAsync(cancellationToken);
        var images = await dbContext.DreamImages.Where(image => image.UserSubject == subject).ToArrayAsync(cancellationToken);
        var voiceCaptures = await dbContext.VoiceCaptures.Where(capture => capture.UserSubject == subject).ToArrayAsync(cancellationToken);
        var jobs = await dbContext.AsyncJobs.Where(job => job.UserSubject == subject).ToArrayAsync(cancellationToken);
        var ledgerRows = await dbContext.AiCostLedger.Where(row => row.UserSubject == subject).ToArrayAsync(cancellationToken);

        dbContext.UserProfiles.RemoveRange(profiles);
        dbContext.Dreams.RemoveRange(dreams);
        dbContext.DreamFacts.RemoveRange(facts);
        dbContext.DreamEmbeddings.RemoveRange(embeddings);
        dbContext.DreamImages.RemoveRange(images);
        dbContext.VoiceCaptures.RemoveRange(voiceCaptures);
        dbContext.AsyncJobs.RemoveRange(jobs);
        var anonymizedLedgerSubject = $"anon_{request.Id:N}";
        foreach (var row in ledgerRows)
        {
            row.UserSubject = anonymizedLedgerSubject;
            row.DreamId = null;
        }

        dbContext.AnonymizedUserTombstones.Add(new AnonymizedUserTombstone
        {
            SubjectPseudonym = pseudonymService.CreatePseudonym(subject)
        });
        request.RequestingUserSubject = null;
        request.Status = AnonymizationRequestStatuses.Approved;
        request.ReviewedBySubject = currentUser.Subject;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.CompletedAt = request.ReviewedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return PrivacyMapper.Map(request);
    }
}
