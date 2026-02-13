namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Configuration settings for correlation tracking.
/// </summary>
public sealed class CorrelationSettings
{
    /// <summary>
    /// Gets or sets the HTTP header name used for correlation ID.
    /// </summary>
    /// <remarks>
    /// Default: <c>"X-Correlation-Id"</c>.
    /// </remarks>
    public string HeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Gets or sets a value indicating whether to generate a correlation ID if missing.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// When enabled, a new GUID is generated if the correlation header is absent.
    /// </remarks>
    public bool GenerateIfMissing { get; set; } = true;
}
