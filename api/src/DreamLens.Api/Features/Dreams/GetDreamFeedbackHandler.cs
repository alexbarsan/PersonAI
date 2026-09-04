using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetDreamFeedbackHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<(bool DreamExists, DreamFeedbackResponse Feedback)> HandleAsync(
        Guid dreamId,
        CancellationToken cancellationToken)
    {
        var dreamExists = await dbContext.Dreams.AsNoTracking().AnyAsync(
            dream => dream.Id == dreamId
                && dream.UserSubject == currentUser.Subject
                && dream.Status == "completed"
                && dream.ResultJson != null,
            cancellationToken);
        if (!dreamExists)
        {
            return (false, Empty());
        }

        var feedback = await dbContext.DreamInterpretationFeedback.AsNoTracking().SingleOrDefaultAsync(
            row => row.DreamId == dreamId && row.UserSubject == currentUser.Subject,
            cancellationToken);
        return (true, feedback is null ? Empty() : Map(feedback));
    }

    internal static DreamFeedbackResponse Map(DreamInterpretationFeedback feedback) => new(
        feedback.Rating,
        JsonSerializer.Deserialize<string[]>(feedback.ReasonsJson) ?? [],
        feedback.Details,
        feedback.UpdatedAt);

    private static DreamFeedbackResponse Empty() => new(null, [], null, null);
}
