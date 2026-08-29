using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Voice;

public sealed class GetVoiceCaptureHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IPrivateAssetStore assetStore)
{
    public async Task<VoiceCaptureResponse?> HandleAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await dbContext.VoiceCaptures.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == captureId && candidate.UserSubject == currentUser.Subject,
            cancellationToken);
        if (capture is null)
        {
            return null;
        }

        var jobId = await dbContext.AsyncJobs.AsNoTracking()
            .Where(job => job.JobType == AsyncJobTypes.VoiceTranscription
                && job.TargetId == capture.Id
                && job.UserSubject == currentUser.Subject)
            .Select(job => (Guid?)job.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var recordingUrl = capture.RetainRecording && capture.Status == VoiceCaptureStatuses.Completed
            ? assetStore.CreateReadUrl(capture.SourceAssetKey)
            : null;
        return VoiceCaptureMapper.Map(capture, jobId, recordingUrl);
    }
}
