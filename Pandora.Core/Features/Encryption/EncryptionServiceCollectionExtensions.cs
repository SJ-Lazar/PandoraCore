using Microsoft.Extensions.DependencyInjection;

namespace Pandora.Core.Features.Encryption;

public static class EncryptionServiceCollectionExtensions
{
    public static IServiceCollection AddEncryption(this IServiceCollection services, Action<EncryptionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncryptionOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();

        return services;
    }
}
