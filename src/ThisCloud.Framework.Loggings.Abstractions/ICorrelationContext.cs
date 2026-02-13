namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Provides access to the current correlation context for distributed tracing and diagnostics.
/// </summary>
public interface ICorrelationContext
{
    /// <summary>
    /// Gets the current correlation ID (GUID).
    /// </summary>
    Guid? CorrelationId { get; }

    /// <summary>
    /// Gets the current request ID (GUID), if applicable.
    /// </summary>
    Guid? RequestId { get; }

    /// <summary>
    /// Gets the current W3C Trace ID (from Activity/traceparent), if available.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// Gets the current user ID, if authenticated.
    /// </summary>
    string? UserId { get; }
}
