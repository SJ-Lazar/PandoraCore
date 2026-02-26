using System.Security.Cryptography;

namespace Pandora.Core.Features.Encryption;

public sealed class EncryptionOptions
{
    public const int DefaultKeySizeBytes = 32;

    public byte[] Key { get; init; } = Array.Empty<byte>();

    public int NonceSize { get; init; } = 12;

    public int TagSize { get; init; } = 16;

    public void Validate()
    {
        if (Key.Length is not 16 and not 24 and not 32)
        {
            throw new InvalidOperationException("Encryption key must be 128, 192, or 256 bits.");
        }

        if (NonceSize < 12 || NonceSize > 32)
        {
            throw new InvalidOperationException("Nonce size must be between 12 and 32 bytes for AES-GCM.");
        }

        if (TagSize is < 12 or > 16)
        {
            throw new InvalidOperationException("Authentication tag size must be between 12 and 16 bytes.");
        }
    }

    public static byte[] GenerateKey(int sizeInBytes = DefaultKeySizeBytes)
    {
        if (sizeInBytes is not 16 and not 24 and not 32)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Key size must be 16, 24, or 32 bytes.");
        }

        var key = new byte[sizeInBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
