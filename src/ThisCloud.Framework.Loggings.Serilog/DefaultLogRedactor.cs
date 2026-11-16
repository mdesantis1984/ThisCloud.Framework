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
/// - Quoted JSON keys/values ("password":"x", "authorization":"Bearer x") → value replaced by [REDACTED]
/// - Emails / phones / DNI/NIE (best-effort) → [REDACTED_PII]
/// Phone detection is deliberately conservative: it needs a "+" international prefix, separators between digit
/// groups, or exactly nine consecutive digits, and 9–15 digits in total, so ids, dates, timestamps and GUIDs are kept.
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
        @"(apiKey|token|secret|password)\s*[:=]\s*(?!\[)[^\s,}\]]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // JSON form: "authorization": "Bearer xyz" (key case-insensitive, whitespace around the colon tolerated).
    private static readonly Regex s_authorizationJsonRegex = new(
        "(\"authorization\"\\s*:\\s*\")Bearer\\s+[^\"]+\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // JSON form: "<key containing apiKey|token|secret|password>": "value" | bare value.
    // The key may carry a prefix/suffix (client_secret, accessToken). The value is replaced and JSON stays valid.
    private static readonly Regex s_secretJsonRegex = new(
        "(\"[\\w.\\-]*(?:apiKey|token|secret|password)[\\w.\\-]*\"\\s*:\\s*)(?:\"(?:[^\"\\\\]|\\\\.)*\"|[^\\s,}\\]\"]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex s_emailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // Phone candidates (never starts inside a word/number/date/GUID, never ends in one):
    //  - international: +CC [(area)] groups
    //  - national with separators: 2–4 digit groups separated by space, dot or hyphen (area in parentheses allowed)
    //  - nine bare digits (e.g. Spanish national numbers)
    // Candidates are only redacted when they hold 9–15 digits, which keeps ISO dates (8 digits) and short ids.
    private static readonly Regex s_phoneRegex = new(
        @"(?<![\w\-.+:/])(?:\+\d{1,3}[-.\s]?(?:\(\d{1,4}\)|\d{1,4})(?:[-.\s]?\d{2,4}){1,3}|(?:\(\d{2,4}\)|\d{2,4})(?:[-.\s]\d{2,4}){2,3}|\d{9})(?![\w\-:/])",
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

        // "authorization": "Bearer token" (JSON)
        redacted = s_authorizationJsonRegex.Replace(redacted, "$1Bearer [REDACTED]\"");

        // JWT tokens (eyJ...)
        redacted = s_jwtTokenRegex.Replace(redacted, "[REDACTED_JWT]");

        // apiKey|token|secret|password in key=value or key: value
        redacted = s_secretKeyValueRegex.Replace(redacted, "$1[REDACTED]");

        // "password": "value" (JSON, quoted key)
        redacted = s_secretJsonRegex.Replace(redacted, "$1\"[REDACTED]\"");

        // Emails (best-effort)
        redacted = s_emailRegex.Replace(redacted, "[REDACTED_PII]");

        // Phone numbers (international format, best-effort)
        redacted = s_phoneRegex.Replace(redacted, RedactPhoneCandidate);

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

    private static string RedactPhoneCandidate(Match match)
    {
        var digits = 0;
        foreach (var c in match.ValueSpan)
        {
            if (char.IsAsciiDigit(c))
            {
                digits++;
            }
        }

        return digits is >= 9 and <= 15 ? "[REDACTED_PII]" : match.Value;
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
