using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Pandora.Core.Features.Encryption;

public sealed class AesGcmEncryptionService : IEncryptionService
{
    private const int StreamBufferSize = 128 * 1024;
    private readonly EncryptionOptions _options;

    public AesGcmEncryptionService(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    public byte[] EncryptBytes(ReadOnlySpan<byte> data)
    {
        var nonce = ArrayPool<byte>.Shared.Rent(_options.NonceSize);
        var cipher = ArrayPool<byte>.Shared.Rent(data.Length);
        var tag = ArrayPool<byte>.Shared.Rent(_options.TagSize);

        try
        {
            RandomNumberGenerator.Fill(nonce.AsSpan(0, _options.NonceSize));

            using var aes = new AesGcm(_options.Key, _options.TagSize);
            aes.Encrypt(nonce.AsSpan(0, _options.NonceSize), data, cipher.AsSpan(0, data.Length), tag.AsSpan(0, _options.TagSize));

            var result = new byte[_options.NonceSize + data.Length + _options.TagSize];
            nonce.AsSpan(0, _options.NonceSize).CopyTo(result);
            cipher.AsSpan(0, data.Length).CopyTo(result.AsSpan(_options.NonceSize));
            tag.AsSpan(0, _options.TagSize).CopyTo(result.AsSpan(_options.NonceSize + data.Length));

            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nonce);
            ArrayPool<byte>.Shared.Return(cipher, clearArray: true);
            ArrayPool<byte>.Shared.Return(tag, clearArray: true);
        }
    }

    public byte[] DecryptBytes(ReadOnlySpan<byte> encryptedData)
    {
        var minimum = _options.NonceSize + _options.TagSize;
        if (encryptedData.Length < minimum)
        {
            throw new CryptographicException("Encrypted payload is too small.");
        }

        var cipherLength = encryptedData.Length - minimum;
        var plaintext = new byte[cipherLength];

        using var aes = new AesGcm(_options.Key, _options.TagSize);
        aes.Decrypt(encryptedData[.._options.NonceSize], encryptedData.Slice(_options.NonceSize, cipherLength), encryptedData.Slice(encryptedData.Length - _options.TagSize), plaintext);

        return plaintext;
    }

    public string EncryptString(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = EncryptBytes(data);
        return Convert.ToBase64String(encrypted);
    }

    public string DecryptString(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        var buffer = Convert.FromBase64String(cipherText);
        var decrypted = DecryptBytes(buffer);
        return Encoding.UTF8.GetString(decrypted);
    }

    public string EncryptNumber<T>(T value) where T : struct, ISpanFormattable
    {
        Span<char> span = stackalloc char[64];
        if (value.TryFormat(span, out var written, default, CultureInfo.InvariantCulture))
        {
            return EncryptString(span[..written].ToString());
        }

        return EncryptString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    public T DecryptNumber<T>(string cipherText) where T : struct, ISpanParsable<T>
    {
        var plain = DecryptString(cipherText);
        if (T.TryParse(plain, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new FormatException("Unable to parse decrypted value to the target type.");
    }

    public Task EncryptFileAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        ValidateFileArguments(inputPath, outputPath);
        return ProcessFileAsync(inputPath, outputPath, EncryptStreamAsync, cancellationToken);
    }

    public Task DecryptFileAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
    {
        ValidateFileArguments(inputPath, outputPath);
        return ProcessFileAsync(inputPath, outputPath, DecryptStreamAsync, cancellationToken);
    }

    private static void ValidateFileArguments(string inputPath, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Input and output files must be different to prevent data loss.");
        }

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file was not found.", inputPath);
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private async Task ProcessFileAsync(string inputPath, string outputPath, Func<Stream, Stream, CancellationToken, Task> pipeline, CancellationToken cancellationToken)
    {
        await using var input = File.Open(inputPath, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        await using var output = File.Open(outputPath, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });

        await pipeline(input, output, cancellationToken).ConfigureAwait(false);
    }

    private async Task EncryptStreamAsync(Stream input, Stream output, CancellationToken cancellationToken)
    {
        var plainBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var cipherBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var nonce = ArrayPool<byte>.Shared.Rent(_options.NonceSize);
        var tag = ArrayPool<byte>.Shared.Rent(_options.TagSize);
        var lengthPrefix = new byte[sizeof(int)];

        try
        {
            using var aes = new AesGcm(_options.Key, _options.TagSize);

            while (true)
            {
                var read = await input.ReadAsync(plainBuffer.AsMemory(0, plainBuffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                RandomNumberGenerator.Fill(nonce.AsSpan(0, _options.NonceSize));
                BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, read);

                aes.Encrypt(nonce.AsSpan(0, _options.NonceSize), plainBuffer.AsSpan(0, read), cipherBuffer.AsSpan(0, read), tag.AsSpan(0, _options.TagSize), lengthPrefix);

                await output.WriteAsync(lengthPrefix, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(nonce.AsMemory(0, _options.NonceSize), cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(tag.AsMemory(0, _options.TagSize), cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(cipherBuffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plainBuffer, clearArray: true);
            ArrayPool<byte>.Shared.Return(cipherBuffer, clearArray: true);
            ArrayPool<byte>.Shared.Return(nonce, clearArray: true);
            ArrayPool<byte>.Shared.Return(tag, clearArray: true);
        }
    }

    private async Task DecryptStreamAsync(Stream input, Stream output, CancellationToken cancellationToken)
    {
        var plainBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var cipherBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        var nonce = ArrayPool<byte>.Shared.Rent(_options.NonceSize);
        var tag = ArrayPool<byte>.Shared.Rent(_options.TagSize);
        var lengthPrefix = new byte[sizeof(int)];

        try
        {
            using var aes = new AesGcm(_options.Key, _options.TagSize);

            while (true)
            {
                if (!await TryFillBufferAsync(input, lengthPrefix, cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                var chunkLength = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);
                if (chunkLength < 0)
                {
                    throw new CryptographicException("Invalid encrypted chunk length.");
                }

                if (chunkLength > cipherBuffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(cipherBuffer, clearArray: true);
                    ArrayPool<byte>.Shared.Return(plainBuffer, clearArray: true);
                    cipherBuffer = ArrayPool<byte>.Shared.Rent(chunkLength);
                    plainBuffer = ArrayPool<byte>.Shared.Rent(chunkLength);
                }

                await ReadExactlyAsync(input, nonce.AsMemory(0, _options.NonceSize), cancellationToken).ConfigureAwait(false);
                await ReadExactlyAsync(input, tag.AsMemory(0, _options.TagSize), cancellationToken).ConfigureAwait(false);
                await ReadExactlyAsync(input, cipherBuffer.AsMemory(0, chunkLength), cancellationToken).ConfigureAwait(false);

                aes.Decrypt(nonce.AsSpan(0, _options.NonceSize), cipherBuffer.AsSpan(0, chunkLength), tag.AsSpan(0, _options.TagSize), plainBuffer.AsSpan(0, chunkLength), lengthPrefix);

                await output.WriteAsync(plainBuffer.AsMemory(0, chunkLength), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plainBuffer, clearArray: true);
            ArrayPool<byte>.Shared.Return(cipherBuffer, clearArray: true);
            ArrayPool<byte>.Shared.Return(nonce, clearArray: true);
            ArrayPool<byte>.Shared.Return(tag, clearArray: true);
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.Slice(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new CryptographicException("Encrypted stream is truncated.");
            }

            total += read;
        }
    }

    private static async Task<bool> TryFillBufferAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.Slice(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return total != 0 ? throw new CryptographicException("Encrypted stream is truncated.") : false;
            }

            total += read;
        }

        return true;
    }
}
