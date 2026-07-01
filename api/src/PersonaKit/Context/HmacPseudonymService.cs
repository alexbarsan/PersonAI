using System.Security.Cryptography;
using System.Text;

namespace PersonaKit.Context;

public sealed class HmacPseudonymService(PseudonymOptions options) : IPseudonymService
{
    private readonly byte[] _secret = DecodeSecret(options.SecretBase64);

    public string CreatePseudonym(string internalUserId)
    {
        if (string.IsNullOrWhiteSpace(internalUserId))
        {
            throw new ArgumentException("Internal user id is required.", nameof(internalUserId));
        }

        using var hmac = new HMACSHA256(_secret);
        var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(internalUserId));
        return $"usr_{Base64UrlEncode(digest)[..16]}";
    }

    private static byte[] DecodeSecret(string secretBase64)
    {
        if (string.IsNullOrWhiteSpace(secretBase64))
        {
            throw new InvalidOperationException("Pseudonym:SecretBase64 must be configured.");
        }

        var secret = Convert.FromBase64String(secretBase64);
        if (secret.Length < 32)
        {
            throw new InvalidOperationException("Pseudonym:SecretBase64 must decode to at least 32 bytes.");
        }

        return secret;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
