using Microsoft.Extensions.DependencyInjection;

namespace Pandora.Core.Features.Export;

public static class ExportServiceCollectionExtensions
{
    public static IServiceCollection AddExport(this IServiceCollection services, Action<ExportOptions>? configure = null)
    {
        var options = new ExportOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IExportService, ExportService>();

        return services;
    }
}
