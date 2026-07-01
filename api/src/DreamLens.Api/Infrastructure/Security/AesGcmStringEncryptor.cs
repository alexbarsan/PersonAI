using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Security;

public sealed class AesGcmStringEncryptor(IOptions<EncryptionOptions> options) : IStringEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key = DecodeKey(options.Value.LocalKeyBase64);

    public AesGcmStringEncryptor(EncryptionOptions options)
        : this(Options.Create(options))
    {
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

        return $"v1.{Convert.ToBase64String(payload)}";
    }

    public string Decrypt(string ciphertext)
    {
        if (!ciphertext.StartsWith("v1.", StringComparison.Ordinal))
        {
            throw new CryptographicException("Unsupported ciphertext version.");
        }

        var payload = Convert.FromBase64String(ciphertext[3..]);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext payload is invalid.");
        }

        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var encrypted = payload[(NonceSize + TagSize)..];
        var plaintext = new byte[encrypted.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DecodeKey(string? localKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(localKeyBase64))
        {
            throw new InvalidOperationException("Encryption:LocalKeyBase64 must be configured.");
        }

        var key = Convert.FromBase64String(localKeyBase64);
        if (key.Length is not (16 or 24 or 32))
        {
            throw new InvalidOperationException("Encryption:LocalKeyBase64 must decode to a 128, 192, or 256-bit key.");
        }

        return key;
    }
}
