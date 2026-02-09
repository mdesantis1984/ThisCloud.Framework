using System.Collections.Generic;

namespace ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Standard API envelope.
/// </summary>
public class ApiEnvelope<T>
{
    /// <summary>
    /// Meta information.
    /// </summary>
    public Meta Meta { get; init; } = default!;

    /// <summary>
    /// Payload.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Errors list.
    /// </summary>
    public IReadOnlyList<ErrorItem> Errors { get; init; } = new List<ErrorItem>();
}
