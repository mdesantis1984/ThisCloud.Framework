using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;
using Serilog.Events;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Extension methods for registering ThisCloud.Framework Serilog logging services in DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ThisCloud.Framework logging services (control, redaction, correlation, audit) in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <param name="serviceName">Service name for enrichment and diagnostics.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> is null or whitespace.</exception>
    public static IServiceCollection AddThisCloudFrameworkLoggings(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, serviceName);

        // Register options as singleton
        services.TryAddSingleton(options);

        // Register global logging level switch (L2.5 - shared with UseThisCloudFrameworkSerilog)
        services.TryAddSingleton(sp => new LoggingLevelSwitch(MapToSerilogLevel(options.Settings.MinimumLevel)));

        // Register redactor (L2.3)
        services.TryAddSingleton<ILogRedactor>(sp =>
        {
            var additionalPatterns = options.Settings.Redaction.AdditionalPatterns?.ToArray();
            return new DefaultLogRedactor(additionalPatterns);
        });

        // Register audit logger (L2.4)
        services.TryAddSingleton<IAuditLogger, SerilogAuditLogger>();

        // Register control service (L2.5)
        services.TryAddSingleton<ILoggingControlService>(sp =>
        {
            var levelSwitch = sp.GetRequiredService<LoggingLevelSwitch>();
            return new SerilogLoggingControlService(levelSwitch, options);
        });

        // Placeholder: register settings store (will be implemented in Phase 4/6)
        // services.TryAddSingleton<ILoggingSettingsStore, InMemoryLoggingSettingsStore>();

        // Correlation context: Singleton lifetime (resolved during Serilog bootstrap from root scope).
        // For HTTP apps, use ThisCloud.Framework.Web middleware to populate context per-request.
        // For console apps, provides default no-op values (TraceId from Activity.Current).
        services.TryAddSingleton<ICorrelationContext, DefaultCorrelationContext>();

        return services;
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

/// <summary>
/// Default no-op correlation context for non-HTTP scenarios.
/// </summary>
internal sealed class DefaultCorrelationContext : ICorrelationContext
{
    public Guid? CorrelationId => null;
    public Guid? RequestId => null;
    public string? TraceId => System.Diagnostics.Activity.Current?.TraceId.ToString();
    public string? UserId => null;
}
