namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Configuration settings for the console sink.
/// </summary>
public sealed class ConsoleSinkSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the console sink is enabled.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// In production, it is recommended to disable console logging for performance.
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
