using System.Net.Mail;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Entitlements;

public sealed class PremiumGrantHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<PremiumGrantOptions> options,
    IOptions<PremiumGrantEmailOptions> emailOptions,
    IServiceProvider serviceProvider,
    ILogger<PremiumGrantHandler> logger)
{
    public async Task<PremiumGrantResponse[]> ListAsync(CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        return await dbContext.PremiumGrants.AsNoTracking()
            .Where(grant => grant.RevokedAt == null)
            .OrderBy(grant => grant.EmailNormalized)
            .Select(grant => new PremiumGrantResponse(
                grant.Id,
                grant.EmailNormalized,
                grant.UserSubject,
                grant.GrantedAt,
                grant.GrantedByEmail))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PremiumGrantResult> GrantAsync(PremiumGrantRequest request, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        var email = NormalizeEmail(request.Email);
        if (email is null)
        {
            return PremiumGrantResult.Invalid("Enter a valid email address.");
        }

        var profile = await dbContext.UserProfiles.SingleOrDefaultAsync(
            candidate => candidate.EmailNormalized == email,
            cancellationToken);
        if (profile is null)
        {
            return PremiumGrantResult.NotFound("This person must sign in and save their profile before receiving Premium.");
        }

        var grant = await dbContext.PremiumGrants.SingleOrDefaultAsync(
            candidate => candidate.UserSubject == profile.UserSubject,
            cancellationToken);
        var shouldSendWelcomeEmail = grant is null || grant.RevokedAt is not null;
        if (grant is null)
        {
            grant = new PremiumGrantRecord
            {
                UserSubject = profile.UserSubject,
                EmailNormalized = email,
                GrantedBySubject = currentUser.Subject,
                GrantedByEmail = CurrentAdministratorEmail(),
            };
            dbContext.PremiumGrants.Add(grant);
        }
        else
        {
            grant.EmailNormalized = email;
            grant.GrantedBySubject = currentUser.Subject;
            grant.GrantedByEmail = CurrentAdministratorEmail();
            grant.GrantedAt = DateTimeOffset.UtcNow;
            grant.RevokedAt = null;
            grant.RevokedBySubject = null;
            if (shouldSendWelcomeEmail)
            {
                grant.PremiumWelcomeEmailSentAt = null;
                grant.PremiumWelcomeEmailProviderMessageId = null;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (shouldSendWelcomeEmail && emailOptions.Value.Enabled)
        {
            try
            {
                var jobService = serviceProvider.GetService<AsyncJobService>();
                if (jobService is null)
                {
                    logger.LogWarning("Premium was granted to {UserSubject}, but asynchronous jobs are not configured for welcome email delivery.", profile.UserSubject);
                    return PremiumGrantResult.Success(Map(grant));
                }

                await jobService.EnqueueAsync(
                    $"{AsyncJobTypes.PremiumGrantEmail}:{grant.Id:N}:{grant.GrantedAt.UtcTicks}",
                    AsyncJobTypes.PremiumGrantEmail,
                    profile.UserSubject,
                    grant.Id,
                    new PremiumGrantEmailJobHandler.PremiumGrantEmailJobPayload(grant.Id),
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Premium was granted to {UserSubject}, but its welcome email could not be queued.", profile.UserSubject);
            }
        }

        return PremiumGrantResult.Success(Map(grant));
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        var grant = await dbContext.PremiumGrants.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (grant is null || grant.RevokedAt is not null)
        {
            return false;
        }

        grant.RevokedAt = DateTimeOffset.UtcNow;
        grant.RevokedBySubject = currentUser.Subject;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void EnsureAdministrator()
    {
        var email = NormalizeEmail(currentUser.Email);
        if (email is null || !options.Value.AdministratorEmails.Any(candidate => string.Equals(NormalizeEmail(candidate), email, StringComparison.Ordinal)))
        {
            throw new UnauthorizedAccessException("This account is not allowed to manage friends and family Premium access.");
        }
    }

    private string CurrentAdministratorEmail() => NormalizeEmail(currentUser.Email)!;

    private static PremiumGrantResponse Map(PremiumGrantRecord grant) => new(
        grant.Id, grant.EmailNormalized, grant.UserSubject, grant.GrantedAt, grant.GrantedByEmail);

    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320)
        {
            return null;
        }

        try
        {
            return new MailAddress(value.Trim()).Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public sealed record PremiumGrantResult(int StatusCode, PremiumGrantResponse? Grant, string? Error)
{
    public static PremiumGrantResult Success(PremiumGrantResponse grant) => new(StatusCodes.Status200OK, grant, null);
    public static PremiumGrantResult Invalid(string error) => new(StatusCodes.Status400BadRequest, null, error);
    public static PremiumGrantResult NotFound(string error) => new(StatusCodes.Status404NotFound, null, error);
}
