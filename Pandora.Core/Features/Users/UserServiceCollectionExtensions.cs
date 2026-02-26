using Microsoft.Extensions.DependencyInjection;

namespace Pandora.Core.Features.Users;

public static class UserServiceCollectionExtensions
{
    public static IServiceCollection AddUsers(this IServiceCollection services, Action<UsersOptions>? configure = null)
    {
        var options = new UsersOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddSingleton<IUserService, UserService>();

        return services;
    }
}
