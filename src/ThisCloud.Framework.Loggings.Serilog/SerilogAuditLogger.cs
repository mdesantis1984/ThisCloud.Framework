using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Serilog-based implementation of <see cref="IAuditLogger"/> for structured audit logging.
/// </summary>
/// <remarks>
/// Writes audit events using structured logging with correlation properties.
/// Does NOT log sensitive data (redaction is caller's responsibility).
/// </remarks>
public sealed class SerilogAuditLogger : IAuditLogger
{
    private readonly ILogger<SerilogAuditLogger> _logger;
    private readonly ILogRedactor _redactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogAuditLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="redactor">The log redactor for sanitizing details.</param>
    public SerilogAuditLogger(
        ILogger<SerilogAuditLogger> logger,
        ILogRedactor redactor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
    }

    /// <summary>
    /// Logs an audit event for a configuration change.
    /// </summary>
    /// <param name="action">The action performed (e.g., "UpdateSettings", "ResetSettings").</param>
    /// <param name="userId">The user ID who performed the action, if known.</param>
    /// <param name="correlationId">The correlation ID for the operation.</param>
    /// <param name="details">Additional details about the change (e.g., diff/patch JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LogAuditEventAsync(
        string action,
        string? userId,
        Guid? correlationId,
        string? details,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        // Redact details to ensure no secrets are logged
        var sanitizedDetails = !string.IsNullOrWhiteSpace(details)
            ? _redactor.Redact(details)
            : null;

        // Log with structured properties
        _logger.LogInformation(
            "Audit event: {AuditAction} by {UserId} (CorrelationId: {CorrelationId}). Details: {AuditDetails}",
            action,
            userId ?? "system",
            correlationId,
            sanitizedDetails ?? "(none)");

        return Task.CompletedTask;
    }
}
