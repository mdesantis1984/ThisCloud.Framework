// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Sample.Loggings.MinimalApi.Stores;

/// <summary>
/// In-memory implementation of ILoggingSettingsStore for sample purposes.
/// </summary>
/// <remarks>
/// ⚠️ SAMPLE-ONLY: For production use, implement persistence (DB, Azure Table Storage, etc.).
/// This sample uses in-memory storage to demonstrate Admin endpoints without external dependencies.
/// Registered via TryAddSingleton to avoid overriding production implementations.
/// </remarks>
internal sealed class InMemoryLoggingSettingsStore : ILoggingSettingsStore
{
    private LogSettings _settings = new();
    private readonly object _lock = new();

    public Task<LogSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Return a copy to prevent external mutations
            return Task.FromResult(CloneSettings(_settings));
        }
    }

    public Task SaveSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_lock)
        {
            _settings = CloneSettings(settings);
        }

        return Task.CompletedTask;
    }

    public Task<byte[]?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        // Version not implemented for in-memory
        return Task.FromResult<byte[]?>(null);
    }

    private static LogSettings CloneSettings(LogSettings source)
    {
        return new LogSettings
        {
            IsEnabled = source.IsEnabled,
            MinimumLevel = source.MinimumLevel,
            Overrides = source.Overrides != null
                ? new Dictionary<string, ThisCloud.Framework.Loggings.Abstractions.LogLevel>(source.Overrides) as IReadOnlyDictionary<string, ThisCloud.Framework.Loggings.Abstractions.LogLevel>
                : null,
            Console = new ConsoleSinkSettings
            {
                Enabled = source.Console.Enabled
            },
            File = new FileSinkSettings
            {
                Enabled = source.File.Enabled,
                Path = source.File.Path,
                RollingFileSizeMb = source.File.RollingFileSizeMb,
                RetainedFileCountLimit = source.File.RetainedFileCountLimit,
                UseCompactJson = source.File.UseCompactJson
            },
            Retention = new RetentionSettings
            {
                Days = source.Retention.Days
            },
            Redaction = new RedactionSettings
            {
                Enabled = source.Redaction.Enabled,
                AdditionalPatterns = source.Redaction.AdditionalPatterns
            },
            Correlation = new CorrelationSettings
            {
                HeaderName = source.Correlation.HeaderName,
                GenerateIfMissing = source.Correlation.GenerateIfMissing
            }
        };
    }
}
