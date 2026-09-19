using DreamLens.Api.Features.Entitlements;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace DreamLens.Api.Tests;

public sealed class PremiumGrantEmailTests
{
    [Fact]
    public void WelcomeEmailUsesLifetimePremiumCopyAndEscapesRecipientName()
    {
        var email = new PremiumGrantWelcomeEmail("friend@example.com", "Mia <Dreamer>");

        Assert.Equal("Your DreamDNA Premium is yours — for life ✨", PremiumGrantWelcomeEmail.Subject);
        Assert.Contains("DreamDNA Premium for life", email.TextBody);
        Assert.Contains("no subscription, no renewals, no catches", email.TextBody);
        Assert.Contains("Mia &lt;Dreamer&gt;", email.HtmlBody);
        Assert.Contains("Alex Barsan", email.HtmlBody);
    }

    [Fact]
    public async Task GrantAsyncQueuesWelcomeEmailOnlyForNewGrant()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserProfiles.Add(new UserProfile
        {
            UserSubject = "friend-subject",
            EmailNormalized = "friend@example.com",
            PreferredName = "Mia",
            EncryptedTraitsJson = "[]"
        });
        await dbContext.SaveChangesAsync();
        var queue = new RecordingQueue();
        var handler = new PremiumGrantHandler(
            dbContext,
            new TestCurrentUser("owner-subject", "ai.ro.dodoloata@gmail.com"),
            Options.Create(new PremiumGrantOptions { AdministratorEmails = ["ai.ro.dodoloata@gmail.com"] }),
            Options.Create(new PremiumGrantEmailOptions { Enabled = true }),
            new ServiceCollection()
                .AddSingleton(new AsyncJobService(dbContext, queue))
                .BuildServiceProvider(),
            NullLogger<PremiumGrantHandler>.Instance);

        var first = await handler.GrantAsync(new PremiumGrantRequest("friend@example.com"), CancellationToken.None);
        var repeated = await handler.GrantAsync(new PremiumGrantRequest("friend@example.com"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, first.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, repeated.StatusCode);
        var message = Assert.Single(queue.Messages);
        Assert.Equal(AsyncJobTypes.PremiumGrantEmail, message.JobType);
        Assert.Equal("friend-subject", message.UserSubject);
        var job = await dbContext.AsyncJobs.SingleAsync();
        Assert.Equal(AsyncJobTypes.PremiumGrantEmail, job.JobType);
        Assert.Equal(first.Grant!.Id, job.TargetId);
    }

    [Fact]
    public async Task EmailJobSendsOnceAndRecordsProviderMessageId()
    {
        await using var dbContext = CreateDbContext();
        var grant = new PremiumGrantRecord
        {
            UserSubject = "friend-subject",
            EmailNormalized = "friend@example.com",
            GrantedBySubject = "owner-subject",
            GrantedByEmail = "ai.ro.dodoloata@gmail.com"
        };
        dbContext.UserProfiles.Add(new UserProfile
        {
            UserSubject = "friend-subject",
            PreferredName = "Mia",
            EmailNormalized = "friend@example.com",
            EncryptedTraitsJson = "[]"
        });
        dbContext.PremiumGrants.Add(grant);
        await dbContext.SaveChangesAsync();
        var sender = new RecordingSender();
        var handler = new PremiumGrantEmailJobHandler(dbContext, sender);
        var message = new AsyncJobMessage(
            Guid.NewGuid(),
            AsyncJobTypes.PremiumGrantEmail,
            "friend-subject",
            System.Text.Json.JsonSerializer.Serialize(new PremiumGrantEmailJobHandler.PremiumGrantEmailJobPayload(grant.Id)));

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        var delivered = Assert.Single(sender.Emails);
        Assert.Equal("friend@example.com", delivered.RecipientEmail);
        Assert.Equal("Mia", delivered.RecipientName);
        Assert.NotNull(grant.PremiumWelcomeEmailSentAt);
        Assert.Equal("ses-message-1", grant.PremiumWelcomeEmailProviderMessageId);
    }

    private static DreamLensDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseInMemoryDatabase($"premium-grant-email-{Guid.NewGuid():N}")
            .Options;
        return new DreamLensDbContext(options);
    }

    private sealed record TestCurrentUser(string Subject, string? Email) : ICurrentUser
    {
        public string? DisplayName => null;
        public string AuthenticationScheme => "test";
    }

    private sealed class RecordingQueue : IAsyncJobQueue
    {
        public List<AsyncJobMessage> Messages { get; } = [];

        public Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSender : IPremiumGrantEmailSender
    {
        public List<PremiumGrantWelcomeEmail> Emails { get; } = [];

        public Task<PremiumGrantEmailDelivery> SendAsync(PremiumGrantWelcomeEmail email, CancellationToken cancellationToken)
        {
            Emails.Add(email);
            return Task.FromResult(new PremiumGrantEmailDelivery("ses-message-1"));
        }
    }
}
