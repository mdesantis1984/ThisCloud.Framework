namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Provides audit logging for configuration changes.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event for a configuration change.
    /// </summary>
    /// <param name="action">The action performed (e.g., "UpdateSettings", "ResetSettings").</param>
    /// <param name="userId">The user ID who performed the action, if known.</param>
    /// <param name="correlationId">The correlation ID for the operation.</param>
    /// <param name="details">Additional details about the change (e.g., diff/patch JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAuditEventAsync(
        string action,
        string? userId,
        Guid? correlationId,
        string? details,
        CancellationToken cancellationToken = default);
}
