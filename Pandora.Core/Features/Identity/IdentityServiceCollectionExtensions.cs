using Microsoft.Extensions.DependencyInjection;

namespace Pandora.Core.Features.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAuth(this IServiceCollection services, Action<AuthOptions>? configure = null)
    {
        var options = new AuthOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IIdentityAuthService, InMemoryIdentityAuthService>();

        return services;
    }
}
