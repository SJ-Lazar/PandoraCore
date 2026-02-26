namespace Pandora.Core.Features.Encryption;

public interface IEncryptionService
{
    byte[] EncryptBytes(ReadOnlySpan<byte> data);

    byte[] DecryptBytes(ReadOnlySpan<byte> encryptedData);

    string EncryptString(string plainText);

    string DecryptString(string cipherText);

    string EncryptNumber<T>(T value) where T : struct, ISpanFormattable;

    T DecryptNumber<T>(string cipherText) where T : struct, ISpanParsable<T>;

    Task EncryptFileAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);

    Task DecryptFileAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default);
}
