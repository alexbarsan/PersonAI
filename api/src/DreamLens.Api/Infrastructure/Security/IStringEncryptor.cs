namespace DreamLens.Api.Infrastructure.Security;

public interface IStringEncryptor
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);
}
