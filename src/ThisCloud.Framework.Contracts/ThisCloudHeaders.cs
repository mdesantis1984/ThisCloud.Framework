namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Header names used by ThisCloud Framework.
/// </summary>
public static class ThisCloudHeaders
{
    /// <summary>
    /// Correlation id header name.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// Request id header name.
    /// </summary>
    public const string RequestId = "X-Request-Id";
    
    /// <summary>
    /// Trace id header name (W3C Trace-Context / trace id).
    /// </summary>
    public const string TraceId = "Traceparent";

    /// <summary>
    /// Service name header key (optional, used in some hosts).
    /// </summary>
    public const string Service = "X-Service-Name";

    /// <summary>
    /// Service version header key (optional).
    /// </summary>
    public const string Version = "X-Service-Version";
}
