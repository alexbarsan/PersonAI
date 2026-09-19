using System.Net;

namespace DreamLens.Api.Features.Entitlements;

public sealed record PremiumGrantWelcomeEmail(string RecipientEmail, string RecipientName)
{
    public const string Subject = "Your DreamDNA Premium is yours — for life ✨";

    public string TextBody => $"""
Hi {RecipientName},

I wanted to give you a small thank-you for being one of the people close to me and DreamDNA from the beginning.

Your account has been upgraded to DreamDNA Premium for life — no subscription, no renewals, no catches. As DreamDNA grows and new Premium features are added, you’ll continue to have access to them.

I hope you enjoy exploring your dreams, patterns, and all the strange places your mind decides to visit at night. And if you ever have feedback, ideas, or something that simply feels wrong, I'd genuinely love to hear it.

Enjoy the journey. 🌙

Alex Barsan
The original dreamer
""";

    public string HtmlBody
    {
        get
        {
            var name = WebUtility.HtmlEncode(RecipientName);
            return $"""
<!doctype html>
<html lang="en">
<body style="margin:0;background:#f5f1e8;color:#192126;font-family:Arial,sans-serif;line-height:1.6;">
  <main style="max-width:620px;margin:32px auto;padding:40px;background:#fffdf8;border:1px solid #ded7c8;">
    <p>Hi {name},</p>
    <p>I wanted to give you a small thank-you for being one of the people close to me and DreamDNA from the beginning.</p>
    <p>Your account has been upgraded to <strong>DreamDNA Premium for life</strong> — no subscription, no renewals, no catches. As DreamDNA grows and new Premium features are added, you’ll continue to have access to them.</p>
    <p>I hope you enjoy exploring your dreams, patterns, and all the strange places your mind decides to visit at night. And if you ever have feedback, ideas, or something that simply feels wrong, I'd genuinely love to hear it.</p>
    <p>Enjoy the journey. 🌙</p>
    <p><strong>Alex Barsan</strong><br><em>The original dreamer</em></p>
  </main>
</body>
</html>
""";
        }
    }
}
