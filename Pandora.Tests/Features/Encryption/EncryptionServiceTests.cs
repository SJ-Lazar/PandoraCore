using Pandora.Core.Features.Encryption;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Pandora.Tests.Features.Encryption;

public sealed class EncryptionServiceTests
{
    [Test]
    public void EncryptDecrypt_String_RoundTrips()
    {
        var service = new AesGcmEncryptionService(CreateOptions());

        const string input = "hello ??";
        var cipher = service.EncryptString(input);
        var roundTrip = service.DecryptString(cipher);

        Assert.That(roundTrip, Is.EqualTo(input));
    }

    [Test]
    public void EncryptDecrypt_Number_RoundTrips()
    {
        var service = new AesGcmEncryptionService(CreateOptions());

        const decimal input = 9876543.21m;
        var cipher = service.EncryptNumber(input);
        var roundTrip = service.DecryptNumber<decimal>(cipher);

        Assert.That(roundTrip, Is.EqualTo(input));
    }

    [Test]
    public void EncryptDecrypt_EmptyBytes_Succeeds()
    {
        var service = new AesGcmEncryptionService(CreateOptions());

        var cipher = service.EncryptBytes(Array.Empty<byte>());
        var roundTrip = service.DecryptBytes(cipher);

        Assert.That(roundTrip, Is.Empty);
    }

    [Test]
    public async Task EncryptDecrypt_File_RoundTrips()
    {
        var service = new AesGcmEncryptionService(CreateOptions());

        var source = Path.GetTempFileName();
        var encrypted = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.enc");
        var decrypted = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");

        try
        {
            var content = RandomNumberGenerator.GetBytes(64 * 1024 + 257);
            await File.WriteAllBytesAsync(source, content);

            await service.EncryptFileAsync(source, encrypted);
            await service.DecryptFileAsync(encrypted, decrypted);

            var roundTrip = await File.ReadAllBytesAsync(decrypted);
            Assert.That(roundTrip, Is.EqualTo(content));
        }
        finally
        {
            SafeDelete(source);
            SafeDelete(encrypted);
            SafeDelete(decrypted);
        }
    }

    private static EncryptionOptions CreateOptions()
    {
        return new EncryptionOptions
        {
            Key = Enumerable.Range(1, EncryptionOptions.DefaultKeySizeBytes).Select(static b => (byte)b).ToArray()
        };
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Swallow cleanup failures.
        }
    }
}
