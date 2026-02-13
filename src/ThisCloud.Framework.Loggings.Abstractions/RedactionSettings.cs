namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Configuration settings for log data redaction (sanitization of sensitive information).
/// </summary>
public sealed class RedactionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether log redaction is enabled.
    /// </summary>
    /// <remarks>
    /// Default: <c>true</c>.
    /// When enabled, sensitive data (secrets, PII) is automatically redacted from logs.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets additional redaction patterns (regular expressions or keywords).
    /// </summary>
    /// <remarks>
    /// Optional. Use to extend the default redaction rules with custom patterns.
    /// </remarks>
    public string[]? AdditionalPatterns { get; set; }
}
