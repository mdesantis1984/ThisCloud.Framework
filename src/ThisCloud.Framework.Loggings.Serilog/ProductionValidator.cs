using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using ThisCloud.Framework.Loggings.Abstractions;

[assembly: InternalsVisibleTo("ThisCloud.Framework.Loggings.Serilog.Tests")]

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Internal validator for Production environment logging configuration.
/// Ensures fail-fast behavior when required settings are missing in Production.
/// </summary>
internal static class ProductionValidator
{
    /// <summary>
    /// Validates logging settings for Production environment.
    /// </summary>
    /// <param name="environment">The hosting environment.</param>
    /// <param name="settings">The logging settings to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Production environment has invalid file sink configuration.
    /// </exception>
    internal static void ValidateProductionSettings(IHostEnvironment environment, LogSettings settings)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(settings);

        var isProduction = string.Equals(environment.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);
        if (!isProduction)
        {
            return; // Validation only applies to Production
        }

        // L3.3: Strict Production validation
        if (!settings.File.Enabled)
        {
            throw new InvalidOperationException(
                "File sink must be enabled in Production environment. " +
                "Set 'ThisCloud:Loggings:File:Enabled' to true in configuration.");
        }

        if (string.IsNullOrWhiteSpace(settings.File.Path))
        {
            throw new InvalidOperationException(
                "File sink path must be configured in Production environment. " +
                "Set 'ThisCloud:Loggings:File:Path' to a valid path in configuration.");
        }
    }
}
