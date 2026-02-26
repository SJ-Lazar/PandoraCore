using Microsoft.Extensions.DependencyInjection;

namespace Pandora.Core.Features.AuditTrail;

public static class AuditTrailServiceCollectionExtensions
{
    public static IServiceCollection AddAuditTrail(this IServiceCollection services, Action<AuditTrailOptions>? configure = null)
    {
        var options = new AuditTrailOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IAuditTrailSink, InMemoryAuditTrailSink>();
        services.AddSingleton<IAuditTrailService, AuditTrailService>();

        return services;
    }
}
