namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Provides persistence operations for log settings.
/// </summary>
public interface ILoggingSettingsStore
{
    /// <summary>
    /// Retrieves the current log settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the current log settings.
    /// </returns>
    Task<LogSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the specified log settings.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current settings version (e.g., RowVersion for optimistic concurrency).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the version token, or null if not applicable.
    /// </returns>
    Task<byte[]?> GetVersionAsync(CancellationToken cancellationToken = default);
}
