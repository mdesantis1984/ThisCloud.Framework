namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Standard property keys for ThisCloud.Framework structured logging.
/// </summary>
/// <remarks>
/// These keys define the canonical enrichment properties used across all ThisCloud.Framework logging sinks.
/// Keys are emitted only when their corresponding values are non-null/non-empty.
/// </remarks>
public static class ThisCloudLogKeys
{
    /// <summary>
    /// Service name (string). Identifies the application/service emitting the log.
    /// </summary>
    public const string Service = "service";

    /// <summary>
    /// Environment name (string). E.g., Development, Staging, Production.
    /// </summary>
    public const string Env = "env";

    /// <summary>
    /// Correlation ID (GUID). Tracks distributed operations across services.
    /// </summary>
    public const string CorrelationId = "correlationId";

    /// <summary>
    /// Request ID (GUID). Identifies a specific HTTP request or operation instance.
    /// </summary>
    public const string RequestId = "requestId";

    /// <summary>
    /// W3C Trace ID (string). From Activity.Current?.TraceId when available.
    /// </summary>
    public const string TraceId = "traceId";

    /// <summary>
    /// User ID (string). Identifies the authenticated user, if applicable.
    /// </summary>
    public const string UserId = "userId";

    /// <summary>
    /// Source context (string). Serilog standard property for logger category/namespace.
    /// </summary>
    public const string SourceContext = "SourceContext";
}
