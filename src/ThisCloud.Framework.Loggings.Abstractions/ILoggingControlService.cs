namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Provides runtime control operations for the logging system.
/// </summary>
public interface ILoggingControlService
{
    /// <summary>
    /// Enables logging system-wide.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables logging system-wide.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the current log settings with the specified settings.
    /// </summary>
    /// <param name="settings">The new settings to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a partial update to the current log settings.
    /// </summary>
    /// <param name="partialSettings">The partial settings to merge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PatchSettingsAsync(LogSettings partialSettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the log settings to their default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetSettingsAsync(CancellationToken cancellationToken = default);
}
