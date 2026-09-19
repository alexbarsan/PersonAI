using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Entitlements;

public sealed class SesPremiumGrantEmailSender(
    IAmazonSimpleEmailServiceV2 ses,
    IOptions<PremiumGrantEmailOptions> options) : IPremiumGrantEmailSender
{
    public async Task<PremiumGrantEmailDelivery> SendAsync(PremiumGrantWelcomeEmail email, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.FromAddress))
        {
            throw new InvalidOperationException("Premium grant email delivery requires a sender address.");
        }

        var response = await ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = $"{settings.FromName} <{settings.FromAddress}>",
            Destination = new Destination { ToAddresses = [email.RecipientEmail] },
            ReplyToAddresses = string.IsNullOrWhiteSpace(settings.ReplyToAddress) ? [] : [settings.ReplyToAddress],
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Amazon.SimpleEmailV2.Model.Content { Charset = "UTF-8", Data = PremiumGrantWelcomeEmail.Subject },
                    Body = new Body
                    {
                        Text = new Amazon.SimpleEmailV2.Model.Content { Charset = "UTF-8", Data = email.TextBody },
                        Html = new Amazon.SimpleEmailV2.Model.Content { Charset = "UTF-8", Data = email.HtmlBody }
                    }
                }
            }
        }, cancellationToken);

        return new PremiumGrantEmailDelivery(response.MessageId);
    }
}
