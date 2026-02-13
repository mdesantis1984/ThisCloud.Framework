using Serilog.Core;
using Serilog.Events;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Serilog-based implementation of <see cref="ILoggingControlService"/> that provides runtime logging control.
/// </summary>
/// <remarks>
/// Uses Serilog's <see cref="LoggingLevelSwitch"/> for dynamic level changes at runtime.
/// Settings changes are applied in-memory; persistence is the responsibility of Admin layer or host.
/// </remarks>
public sealed class SerilogLoggingControlService : ILoggingControlService
{
    private readonly LoggingLevelSwitch _globalLevelSwitch;
    private readonly ThisCloudSerilogOptions _options;
    private LogSettings _currentSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogLoggingControlService"/> class.
    /// </summary>
    /// <param name="globalLevelSwitch">The global logging level switch.</param>
    /// <param name="options">The Serilog options containing current settings.</param>
    public SerilogLoggingControlService(
        LoggingLevelSwitch globalLevelSwitch,
        ThisCloudSerilogOptions options)
    {
        _globalLevelSwitch = globalLevelSwitch ?? throw new ArgumentNullException(nameof(globalLevelSwitch));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _currentSettings = options.Settings;
    }

    /// <summary>
    /// Enables logging system-wide.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        _currentSettings.IsEnabled = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disables logging system-wide.
    /// </summary>
    /// <remarks>
    /// WARNING: Disabling logging can make diagnostics impossible.
    /// This sets the minimum level to Fatal to suppress most logs (Serilog doesn't support full disable).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        _currentSettings.IsEnabled = false;
        _globalLevelSwitch.MinimumLevel = LogEventLevel.Fatal;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces the current log settings with the specified settings.
    /// </summary>
    /// <param name="settings">The new settings to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _currentSettings = settings;
        ApplySettingsToSwitch(settings);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies a partial update to the current log settings.
    /// </summary>
    /// <param name="partialSettings">The partial settings to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PatchSettingsAsync(LogSettings partialSettings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partialSettings);

        // Merge partial settings into current (simple shallow merge for Phase 2)
        // Note: Full deep merge logic can be enhanced in Admin layer (Phase 4)
        _currentSettings.IsEnabled = partialSettings.IsEnabled;
        _currentSettings.MinimumLevel = partialSettings.MinimumLevel;

        if (partialSettings.Overrides != null)
        {
            _currentSettings.Overrides = partialSettings.Overrides;
        }

        ApplySettingsToSwitch(_currentSettings);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the log settings to their default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ResetSettingsAsync(CancellationToken cancellationToken = default)
    {
        _currentSettings = new LogSettings();
        ApplySettingsToSwitch(_currentSettings);
        return Task.CompletedTask;
    }

    private void ApplySettingsToSwitch(LogSettings settings)
    {
        var minimumLevel = MapToSerilogLevel(settings.MinimumLevel);
        _globalLevelSwitch.MinimumLevel = minimumLevel;

        // Note: Overrides cannot be applied dynamically to Serilog after initial configuration
        // (requires full logger reconfiguration). This is a known Serilog limitation.
        // For Phase 2, we only support global MinimumLevel changes at runtime.
        // Full override reconfiguration will be addressed in Phase 4 Admin layer if needed.
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
