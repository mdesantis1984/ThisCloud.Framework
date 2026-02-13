namespace ThisCloud.Framework.Loggings.Abstractions;

/// <summary>
/// Provides redaction (sanitization) of sensitive information from log data.
/// </summary>
public interface ILogRedactor
{
    /// <summary>
    /// Redacts sensitive information from the specified text.
    /// </summary>
    /// <param name="input">The text to redact.</param>
    /// <returns>The redacted text.</returns>
    string Redact(string input);

    /// <summary>
    /// Redacts sensitive information from the specified key-value pairs.
    /// </summary>
    /// <param name="properties">The properties to redact.</param>
    /// <returns>A dictionary with redacted values.</returns>
    IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> properties);
}
