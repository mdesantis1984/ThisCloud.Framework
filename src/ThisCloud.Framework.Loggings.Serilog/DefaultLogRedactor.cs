using System.Text.RegularExpressions;
using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Serilog;

/// <summary>
/// Default implementation of <see cref="ILogRedactor"/> that redacts sensitive information from log messages.
/// </summary>
/// <remarks>
/// Redacts the following patterns (per LoggingsContracts v1):
/// - Authorization: Bearer tokens → [REDACTED]
/// - JWT tokens (eyJ...) → [REDACTED_JWT]
/// - apiKey|token|secret|password in key=value or key: value → [REDACTED]
/// - Emails / phones / DNI/NIE (best-effort) → [REDACTED_PII]
/// </remarks>
public sealed class DefaultLogRedactor : ILogRedactor
{
    private readonly string[] _additionalPatterns;

    // Pre-compiled regex patterns for performance
    private static readonly Regex s_authorizationBearerRegex = new(
        @"Authorization:\s*Bearer\s+[\w\-\.]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_jwtTokenRegex = new(
        @"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_secretKeyValueRegex = new(
        @"(apiKey|token|secret|password)\s*[:=]\s*[^\s,}\]]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_emailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_phoneRegex = new(
        @"\+?\d{1,3}[-.\s]?\(?\d{1,4}\)?[-.\s]?\d{1,4}[-.\s]?\d{1,9}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_dniNieRegex = new(
        @"\b\d{8}[A-Z]\b",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLogRedactor"/> class.
    /// </summary>
    /// <param name="additionalPatterns">Optional additional regex patterns to redact.</param>
    public DefaultLogRedactor(string[]? additionalPatterns = null)
    {
        _additionalPatterns = additionalPatterns ?? Array.Empty<string>();
    }

    /// <summary>
    /// Redacts sensitive information from a log message string.
    /// </summary>
    /// <param name="message">The message to redact.</param>
    /// <returns>The redacted message, or the original if null/empty.</returns>
    public string Redact(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message ?? string.Empty;
        }

        var redacted = message;

        // Authorization: Bearer token
        redacted = s_authorizationBearerRegex.Replace(redacted, "Authorization: Bearer [REDACTED]");

        // JWT tokens (eyJ...)
        redacted = s_jwtTokenRegex.Replace(redacted, "[REDACTED_JWT]");

        // apiKey|token|secret|password in key=value or key: value
        redacted = s_secretKeyValueRegex.Replace(redacted, "$1[REDACTED]");

        // Emails (best-effort)
        redacted = s_emailRegex.Replace(redacted, "[REDACTED_PII]");

        // Phone numbers (international format, best-effort)
        redacted = s_phoneRegex.Replace(redacted, "[REDACTED_PII]");

        // DNI/NIE (Spanish ID, best-effort: 8 digits + letter)
        redacted = s_dniNieRegex.Replace(redacted, "[REDACTED_PII]");

        // Apply additional patterns if any
        foreach (var pattern in _additionalPatterns)
        {
            try
            {
                redacted = Regex.Replace(redacted, pattern, "[REDACTED]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip pattern if it times out
            }
        }

        return redacted;
    }

    /// <summary>
    /// Redacts sensitive properties from a dictionary of log properties.
    /// </summary>
    /// <param name="properties">The properties dictionary to redact.</param>
    /// <returns>A new dictionary with redacted values.</returns>
    public IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?>? properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return new Dictionary<string, object?>();
        }

        var redacted = new Dictionary<string, object?>(properties.Count);

        foreach (var (key, value) in properties)
        {
            // Redact known sensitive keys
            if (IsSensitiveKey(key))
            {
                redacted[key] = "[REDACTED]";
            }
            else if (value is string stringValue)
            {
                redacted[key] = Redact(stringValue);
            }
            else
            {
                redacted[key] = value;
            }
        }

        return redacted;
    }

    private static bool IsSensitiveKey(string key)
    {
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("password")
            || lowerKey.Contains("secret")
            || lowerKey.Contains("apikey")
            || lowerKey.Contains("token")
            || lowerKey.Contains("authorization")
            || lowerKey.Contains("connectionstring");
    }
}
