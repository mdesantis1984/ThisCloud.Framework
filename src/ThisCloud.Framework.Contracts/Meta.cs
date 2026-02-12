using System;
using System.Text.Json.Serialization;

namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Envelope meta information.
/// </summary>
public record Meta
{
    /// <summary>
    /// Service name.
    /// </summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>
    /// API version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp UTC.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>
    /// Correlation ID.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Request ID.
    /// </summary>
    public Guid RequestId { get; init; }

    /// <summary>
    /// Trace ID (W3C format).
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public Meta()
    {
    }

    /// <summary>
    /// Creates a new Meta with service and version, other fields default.
    /// </summary>
    public Meta(string service, string version)
    {
        Service = service;
        Version = version;
        TimestampUtc = DateTimeOffset.UtcNow;
        CorrelationId = Guid.NewGuid();
        RequestId = Guid.NewGuid();
        TraceId = null;
    }

    /// <summary>
    /// Full constructor (for serialization scenarios).
    /// </summary>
    [JsonConstructor]
    public Meta(string service, string version, DateTimeOffset timestampUtc, Guid correlationId, Guid requestId, string? traceId)
    {
        Service = service;
        Version = version;
        TimestampUtc = timestampUtc;
        CorrelationId = correlationId;
        RequestId = requestId;
        TraceId = traceId;
    }
}
