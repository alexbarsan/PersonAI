using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class UpdateDreamFeedbackHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<UpdateDreamFeedbackResult> HandleAsync(
        Guid dreamId,
        UpdateDreamFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var rating = request.Rating?.Trim().ToLowerInvariant();
        if (rating is not DreamFeedbackRatings.Like and not DreamFeedbackRatings.Dislike)
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status400BadRequest, "rating", "Rating must be like or dislike.");
        }

        var reasons = (request.Reasons ?? [])
            .Select(reason => reason.Trim().ToLowerInvariant())
            .Where(reason => reason.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (reasons.Length > 6 || reasons.Any(reason => !DreamFeedbackReasons.Allowed.Contains(reason)))
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status400BadRequest, "reasons", "One or more feedback reasons are invalid.");
        }

        var details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim();
        if (details?.Length > 1000)
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status400BadRequest, "details", "Feedback details must be 1000 characters or fewer.");
        }

        if (rating == DreamFeedbackRatings.Dislike && reasons.Length == 0)
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status400BadRequest, "reasons", "Select at least one reason for the dislike.");
        }

        if (rating == DreamFeedbackRatings.Like && (reasons.Length > 0 || details is not null))
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status400BadRequest, "feedback", "Reasons and details are only accepted with a dislike.");
        }

        var dreamExists = await dbContext.Dreams.AsNoTracking().AnyAsync(
            dream => dream.Id == dreamId
                && dream.UserSubject == currentUser.Subject
                && dream.Status == "completed"
                && dream.ResultJson != null,
            cancellationToken);
        if (!dreamExists)
        {
            return UpdateDreamFeedbackResult.Failure(StatusCodes.Status404NotFound, "dream", "Dream interpretation was not found.");
        }

        var feedback = await dbContext.DreamInterpretationFeedback.SingleOrDefaultAsync(
            row => row.DreamId == dreamId && row.UserSubject == currentUser.Subject,
            cancellationToken);
        if (feedback is null)
        {
            feedback = new DreamInterpretationFeedback
            {
                DreamId = dreamId,
                UserSubject = currentUser.Subject,
                Rating = rating
            };
            dbContext.DreamInterpretationFeedback.Add(feedback);
        }

        feedback.Rating = rating;
        feedback.ReasonsJson = JsonSerializer.Serialize(reasons);
        feedback.Details = details;
        feedback.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return UpdateDreamFeedbackResult.Success(GetDreamFeedbackHandler.Map(feedback));
    }
}
