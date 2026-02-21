using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Extension methods for configuring ThisCloud.Framework Serilog logging on <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider for ThisCloud.Framework applications.
    /// </summary>
    /// <param name="host">The host builder to configure.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <param name="serviceName">Service name for enrichment and diagnostics.</param>
    /// <returns>The configured <see cref="IHostBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when in Production environment and file sink is not properly configured.
    /// File sink must be enabled and have a valid path in Production.
    /// </exception>
    public static IHostBuilder UseThisCloudFrameworkSerilog(
        this IHostBuilder host,
        IConfiguration configuration,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, serviceName);

        host.UseSerilog((context, services, loggerConfiguration) =>
        {
            // L3.3: Fail-fast validation in Production (before logger configuration)
            ProductionValidator.ValidateProductionSettings(context.HostingEnvironment, options.Settings);

            // Try to get or create the global level switch (shared with control service)
            var globalLevelSwitch = services.GetService<LoggingLevelSwitch>()
                ?? new LoggingLevelSwitch(MapToSerilogLevel(options.Settings.MinimumLevel));

            ConfigureSerilog(loggerConfiguration, options, context.HostingEnvironment.EnvironmentName, services, globalLevelSwitch);
        });

        return host;
    }

    private static void ConfigureSerilog(
        LoggerConfiguration loggerConfiguration,
        ThisCloudSerilogOptions options,
        string environmentName,
        IServiceProvider services,
        LoggingLevelSwitch globalLevelSwitch)
    {
        var settings = options.Settings;

        // Use the shared global level switch for runtime control
        loggerConfiguration.MinimumLevel.ControlledBy(globalLevelSwitch);

        // Apply namespace overrides
        if (settings.Overrides != null)
        {
            foreach (var (sourceContext, level) in settings.Overrides)
            {
                loggerConfiguration.MinimumLevel.Override(sourceContext, MapToSerilogLevel(level));
            }
        }

        // Enrich with standard properties
        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", options.ServiceName)
            .Enrich.WithProperty("env", environmentName)
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();

        // Enrich with ThisCloud correlation context (L2.2)
        var correlationContext = services.GetService(typeof(ICorrelationContext)) as ICorrelationContext;
        if (correlationContext != null)
        {
            loggerConfiguration.Enrich.With(new ThisCloudContextEnricher(correlationContext));
        }

        // Configure Console sink (L3.1)
        if (settings.Console.Enabled)
        {
            loggerConfiguration.WriteTo.Console();
        }

        // Configure File sink (L3.2)
        if (settings.File.Enabled)
        {
            var fileSizeLimitBytes = settings.File.RollingFileSizeMb * 1024L * 1024L;

            if (settings.File.UseCompactJson)
            {
                // Compact JSON format (NDJSON)
                loggerConfiguration.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: settings.File.Path,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: settings.File.RetainedFileCountLimit,
                    shared: false,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }
            else
            {
                // Plain text format
                loggerConfiguration.WriteTo.File(
                    path: settings.File.Path,
                    fileSizeLimitBytes: fileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: settings.File.RetainedFileCountLimit,
                    shared: false,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }
        }
    }

    private static LogEventLevel MapToSerilogLevel(LogLevel level) => level switch
    {
        LogLevel.Verbose => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}
