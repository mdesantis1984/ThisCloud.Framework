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
    public List<ErrorItem> Errors { get; init; } = new();

    /// <summary>
    /// Creates an empty envelope with default meta.
    /// </summary>
    public ApiEnvelope()
    {
        Meta = new Meta("unknown", "0.0");
        Data = default;
        Errors = new List<ErrorItem>();
    }
}
