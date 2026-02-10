using System;

namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Envelope meta information.
/// </summary>
public record Meta(
    string Service,
    string Version,
    DateTimeOffset TimestampUtc,
    Guid CorrelationId,
    Guid RequestId,
    string? TraceId
)
{
    /// <summary>
    /// Creates a new Meta with defaults for timestamp (UTC now).
    /// </summary>
    public Meta(string service, string version)
        : this(service, version, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), null)
    {
    }
}
