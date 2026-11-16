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
    private readonly RuntimeLogLevelOverrides _runtimeLevels;
    private readonly object _sync = new();
    private LogSettings _currentSettings;
    private LogEventLevel? _levelBeforeDisable;
    private readonly IReadOnlyDictionary<string, LogLevel>? _startupOverrides;
    private IReadOnlyDictionary<string, LogLevel>? _overrides;

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
        _runtimeLevels = RuntimeLogLevelOverrides.For(_globalLevelSwitch);
        _overrides = options.Settings.Overrides;

        // Snapshot of the configured startup overrides: ResetSettingsAsync returns to them (the baseline kept them,
        // because they were baked into the Serilog logger), while overrides added at runtime are discarded.
        _startupOverrides = options.Settings.Overrides is { } startup
            ? new Dictionary<string, LogLevel>(startup)
            : null;
    }

    /// <summary>
    /// Enables logging system-wide.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task EnableAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _currentSettings.IsEnabled = true;

            // Restore the level that was active before DisableAsync (nothing to restore if never disabled).
            if (_levelBeforeDisable is { } previousLevel)
            {
                _globalLevelSwitch.MinimumLevel = previousLevel;
                _levelBeforeDisable = null;
            }

            _runtimeLevels.Update(_overrides, active: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disables logging system-wide.
    /// </summary>
    /// <remarks>
    /// WARNING: Disabling logging can make diagnostics impossible.
    /// This sets the minimum level to Fatal to suppress most logs (Serilog doesn't support full disable) and
    /// suspends the per-namespace overrides. The level that was active is remembered and restored by
    /// <see cref="EnableAsync"/>. Audit events are never silenced.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisableAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            // A second DisableAsync must not overwrite the remembered level with Fatal.
            _levelBeforeDisable ??= _globalLevelSwitch.MinimumLevel;
            _currentSettings.IsEnabled = false;
            _globalLevelSwitch.MinimumLevel = LogEventLevel.Fatal;
            _runtimeLevels.Update(_overrides, active: false);
        }

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

        lock (_sync)
        {
            _currentSettings = settings;
            ApplySettingsToSwitch(settings);
        }

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

        lock (_sync)
        {
            // Merge partial settings into current (simple shallow merge for Phase 2)
            // Note: Full deep merge logic can be enhanced in Admin layer (Phase 4)
            _currentSettings.IsEnabled = partialSettings.IsEnabled;
            _currentSettings.MinimumLevel = partialSettings.MinimumLevel;

            if (partialSettings.Overrides != null)
            {
                _currentSettings.Overrides = partialSettings.Overrides;
            }

            ApplySettingsToSwitch(_currentSettings);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the log settings to their default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ResetSettingsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _overrides = _startupOverrides;
            _currentSettings = new LogSettings { Overrides = _startupOverrides };
            ApplySettingsToSwitch(_currentSettings);
        }

        return Task.CompletedTask;
    }

    private void ApplySettingsToSwitch(LogSettings settings)
    {
        var minimumLevel = MapToSerilogLevel(settings.MinimumLevel);
        _globalLevelSwitch.MinimumLevel = minimumLevel;

        // Applying settings sets the level explicitly, so any level remembered by DisableAsync is obsolete, and the
        // per-namespace overrides (evaluated by the Serilog pipeline's runtime filter) take effect immediately.
        _levelBeforeDisable = null;

        // Like PatchSettingsAsync, null overrides mean "not specified": the active ones are kept. An explicitly
        // empty dictionary clears them, and ResetSettingsAsync drops them.
        if (settings.Overrides != null)
        {
            _overrides = settings.Overrides;
        }

        _runtimeLevels.Update(_overrides, active: true);
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
