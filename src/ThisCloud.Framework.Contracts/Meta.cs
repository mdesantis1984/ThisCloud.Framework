using System;

namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Envelope meta information.
/// </summary>
public record Meta(
    string Service,
    string Version,
    DateTime TimestampUtc,
    Guid CorrelationId,
    Guid RequestId,
    string? TraceId
);
