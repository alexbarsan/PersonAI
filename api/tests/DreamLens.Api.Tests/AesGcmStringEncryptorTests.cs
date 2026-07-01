using System.Text;
using DreamLens.Api.Infrastructure.Security;

namespace DreamLens.Api.Tests;

public sealed class AesGcmStringEncryptorTests
{
    [Fact]
    public void EncryptProducesCiphertextThatDoesNotExposePlaintext()
    {
        var encryptor = new AesGcmStringEncryptor(CreateOptions());

        var ciphertext = encryptor.Encrypt("spiders, peanuts, new job");

        Assert.DoesNotContain("spiders", ciphertext);
        Assert.DoesNotContain("peanuts", ciphertext);
        Assert.NotEqual("spiders, peanuts, new job", ciphertext);
    }

    [Fact]
    public void EncryptUsesUniqueNonceAndDecryptsBackToPlaintext()
    {
        var encryptor = new AesGcmStringEncryptor(CreateOptions());

        var first = encryptor.Encrypt("sensitive profile traits");
        var second = encryptor.Encrypt("sensitive profile traits");

        Assert.NotEqual(first, second);
        Assert.Equal("sensitive profile traits", encryptor.Decrypt(first));
        Assert.Equal("sensitive profile traits", encryptor.Decrypt(second));
    }

    private static EncryptionOptions CreateOptions()
    {
        var key = Convert.ToBase64String(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));

        return new EncryptionOptions
        {
            LocalKeyBase64 = key
        };
    }
}
