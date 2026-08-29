using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PersonaKit.Context;

namespace DreamLens.Api.Infrastructure.Identity;

public interface IAnonymizedUserAccessService
{
    Task<bool> IsAnonymizedAsync(string subject, CancellationToken cancellationToken);
}

public sealed class AnonymizedUserAccessService(
    DreamLensDbContext dbContext,
    IPseudonymService pseudonymService) : IAnonymizedUserAccessService
{
    public Task<bool> IsAnonymizedAsync(string subject, CancellationToken cancellationToken)
    {
        var pseudonym = pseudonymService.CreatePseudonym(subject);
        return dbContext.AnonymizedUserTombstones
            .AsNoTracking()
            .AnyAsync(tombstone => tombstone.SubjectPseudonym == pseudonym, cancellationToken);
    }
}

public static class AnonymizedUserAccessApplicationBuilderExtensions
{
    public static IApplicationBuilder UseDreamLensAnonymizedUserGuard(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true
                && context.Request.Path.StartsWithSegments("/v1")
                && context.RequestServices.GetService<IAnonymizedUserAccessService>() is { } accessService
                && context.RequestServices.GetService<ICurrentUser>() is { } currentUser
                && await accessService.IsAnonymizedAsync(currentUser.Subject, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "account_anonymized" }, context.RequestAborted);
                return;
            }

            await next(context);
        });
    }
}
