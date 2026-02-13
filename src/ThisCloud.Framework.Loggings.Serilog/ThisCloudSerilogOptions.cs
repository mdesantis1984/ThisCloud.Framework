using Microsoft.Extensions.Configuration;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Configuration options for ThisCloud.Framework.Loggings.Serilog integration.
/// </summary>
public sealed class ThisCloudSerilogOptions
{
    /// <summary>
    /// Gets or sets the service name used for enrichment.
    /// </summary>
    public string ServiceName { get; set; } = "unknown";

    /// <summary>
    /// Gets or sets the environment name (e.g., Development, Production).
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets the logging settings parsed from configuration.
    /// </summary>
    public LogSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration section root for Serilog settings.
    /// </summary>
    /// <remarks>
    /// Default: "ThisCloud:Loggings"
    /// </remarks>
    public string ConfigurationSection { get; set; } = "ThisCloud:Loggings";

    /// <summary>
    /// Creates a new instance of <see cref="ThisCloudSerilogOptions"/> from configuration.
    /// </summary>
    /// <param name="configuration">Application configuration root.</param>
    /// <param name="serviceName">Service name for enrichment.</param>
    /// <param name="environment">Environment name (defaults to ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT).</param>
    /// <returns>Configured options instance.</returns>
    public static ThisCloudSerilogOptions FromConfiguration(
        IConfiguration configuration,
        string serviceName,
        string? environment = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var options = new ThisCloudSerilogOptions
        {
            ServiceName = serviceName,
            Environment = environment
                ?? configuration["ASPNETCORE_ENVIRONMENT"]
                ?? configuration["DOTNET_ENVIRONMENT"]
                ?? "Production"
        };

        var settingsSection = configuration.GetSection(options.ConfigurationSection);
        if (settingsSection.Exists())
        {
            options.Settings = settingsSection.Get<LogSettings>() ?? new LogSettings();

            // Manual override for Path when binding doesn't replace defaults with whitespace
            var filePathValue = settingsSection["File:Path"];
            if (filePathValue != null)
            {
                options.Settings.File.Path = filePathValue;
            }
        }

        return options;
    }
}
