using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pandora.Core.Features.Logging;

public static class LoggingHostBuilderExtensions
{
    /// <summary>
    /// Applies a centralized Serilog configuration with sensible defaults.
    /// </summary>
    public static void UseCoreSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console());
    }
}
