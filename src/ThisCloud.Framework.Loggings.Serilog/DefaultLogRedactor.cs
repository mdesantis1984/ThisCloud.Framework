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
/// - Quoted JSON keys/values ("password":"x", "authorization":"Bearer x"), also single-quoted ('password':'x') or
///   escaped inside a string (\"password\":\"x\") → value replaced by [REDACTED], quoting kept
/// - Emails / phones / DNI/NIE (best-effort) → [REDACTED_PII]
/// Phone detection is deliberately conservative: it needs a "+" international prefix, separators between digit
/// groups, or exactly nine consecutive digits, and 9–15 digits in total, so ids, dates, timestamps, GUIDs and IPv4 addresses are kept.
/// </remarks>
public sealed class DefaultLogRedactor : ILogRedactor
{
    private readonly string[] _additionalPatterns;
    private readonly Func<string, string> _builtInRedaction;

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

    // JSON-like form of the Authorization header: "authorization": "Bearer xyz". The key and the value may be
    // double-quoted, single-quoted ('authorization': 'Bearer xyz') or escaped inside a string
    // (\"authorization\": \"Bearer xyz\"); the value closes with the quote that opened it, which is kept.
    private static readonly Regex s_authorizationJsonRegex = new(
        "((?<kq>\\\\\"|\"|')authorization\\k<kq>\\s*:\\s*(?<vq>\\\\\"|\"|'))Bearer(?>\\s+)(?:(?!\\k<vq>).)+\\k<vq>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    // JSON-like key: "key": | 'key': | \"key\": (the closing quote matches the opening one). Only the key and the
    // colon are matched here; whether the key names a secret and where the value ends are decided in linear code
    // (see RedactJsonSecrets). Putting the keyword search inside the pattern ([\w.\-]*(?:token)[\w.\-]*) made it
    // backtrack quadratically on inputs such as "tokentokentoken...
    private static readonly Regex s_jsonKeyRegex = new(
        "(?<kq>\\\\\"|\"|')(?<key>[\\w.\\-]+)\\k<kq>\\s*:\\s*",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly string[] s_secretKeyWords = ["apiKey", "token", "secret", "password"];

    // NonBacktracking: the local part [A-Za-z0-9._%+-]+ overlaps with the word boundary, so the backtracking engine
    // is quadratic on inputs like "a.a.a.a..." (every letter is a candidate start). Same matches, linear time.
    private static readonly Regex s_emailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.NonBacktracking,
        TimeSpan.FromSeconds(1));

    // Phone candidates (never starts inside a word/number/date/GUID, never ends in one):
    //  - international: +CC [(area)] groups
    //  - national with separators: 2–4 digit groups separated by space, dot or hyphen (area in parentheses allowed)
    //  - nine bare digits (e.g. Spanish national numbers)
    // Candidates are only redacted when they hold 9–15 digits, which keeps ISO dates (8 digits) and short ids.
    // A candidate that starts a dotted-quad IPv4 address (192.168.100.200, also with :port or /prefix) is skipped.
    private static readonly Regex s_phoneRegex = new(
        @"(?<![\w\-.+:/])(?!(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}(?!\d))(?:\+\d{1,3}[-.\s]?(?:\(\d{1,4}\)|\d{1,4})(?:[-.\s]?\d{2,4}){1,3}|(?:\(\d{2,4}\)|\d{2,4})(?:[-.\s]\d{2,4}){2,3}|\d{9})(?![\w\-:/])",
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
        : this(additionalPatterns, RedactBuiltIn)
    {
    }

    /// <summary>
    /// Test seam: lets tests replace the built-in redaction stage (for example to simulate a regex timeout).
    /// </summary>
    internal DefaultLogRedactor(string[]? additionalPatterns, Func<string, string> builtInRedaction)
    {
        _additionalPatterns = additionalPatterns ?? Array.Empty<string>();
        _builtInRedaction = builtInRedaction;
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

        string redacted;
        try
        {
            redacted = _builtInRedaction(message);
        }
        catch (RegexMatchTimeoutException)
        {
            // Fail safe: an unredacted message must never be written because a pattern timed out, and audit
            // logging must not break either, so the whole message is replaced by the placeholder.
            return "[REDACTED]";
        }

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

    private static string RedactBuiltIn(string message)
    {
        var redacted = message;

        // Authorization: Bearer token
        redacted = s_authorizationBearerRegex.Replace(redacted, "Authorization: Bearer [REDACTED]");

        // "authorization": "Bearer token" (JSON)
        redacted = s_authorizationJsonRegex.Replace(redacted, "$1Bearer [REDACTED]${vq}");

        // JWT tokens (eyJ...)
        redacted = s_jwtTokenRegex.Replace(redacted, "[REDACTED_JWT]");

        // apiKey|token|secret|password in key=value or key: value
        redacted = s_secretKeyValueRegex.Replace(redacted, "$1[REDACTED]");

        // "password": "value" (JSON, quoted key)
        redacted = RedactJsonSecrets(redacted);

        // Emails (best-effort)
        redacted = s_emailRegex.Replace(redacted, "[REDACTED_PII]");

        // Phone numbers (international format, best-effort)
        redacted = s_phoneRegex.Replace(redacted, RedactPhoneCandidate);

        // DNI/NIE (Spanish ID, best-effort: 8 digits + letter)
        redacted = s_dniNieRegex.Replace(redacted, "[REDACTED_PII]");

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

    private static bool IsSecretKeyName(ReadOnlySpan<char> key)
    {
        foreach (var word in s_secretKeyWords)
        {
            if (key.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Replaces the value of every JSON-like <c>"key": value</c> whose key names a secret. Single pass, linear in the
    /// input: the key regex is linear and each value is scanned once, never re-scanned.
    /// </summary>
    private static string RedactJsonSecrets(string input)
    {
        System.Text.StringBuilder? output = null;
        var copied = 0;
        var position = 0;

        while (position < input.Length)
        {
            var match = s_jsonKeyRegex.Match(input, position);
            if (!match.Success)
            {
                break;
            }

            var valueStart = match.Index + match.Length;
            if (!IsSecretKeyName(match.Groups["key"].ValueSpan)
                || !TryFindValueEnd(input, valueStart, out var valueEnd, out var quoteStyle))
            {
                position = match.Index + 1;
                continue;
            }

            output ??= new System.Text.StringBuilder(input.Length);
            output.Append(input, copied, valueStart - copied);
            output.Append(quoteStyle switch
            {
                QuoteStyle.Escaped => "\\\"[REDACTED]\\\"",
                QuoteStyle.Single => "'[REDACTED]'",
                _ => "\"[REDACTED]\""
            });

            copied = valueEnd;
            position = valueEnd;
        }

        if (output == null)
        {
            return input;
        }

        output.Append(input, copied, input.Length - copied);
        return output.ToString();
    }

    private enum QuoteStyle
    {
        Double,
        Single,
        Escaped,
        Bare
    }

    /// <summary>
    /// Finds the end of the value that starts at <paramref name="start"/>: an escaped (<c>\"x\"</c>), double-quoted or
    /// single-quoted string (with backslash escapes), or a bare token that stops at whitespace, ',', '}', ']' or '"'.
    /// </summary>
    private static bool TryFindValueEnd(string input, int start, out int end, out QuoteStyle style)
    {
        end = start;
        style = QuoteStyle.Bare;
        if (start >= input.Length)
        {
            return false;
        }

        var span = input.AsSpan(start);
        if (span.StartsWith("\\\"", StringComparison.Ordinal))
        {
            // \"value\": runs to the next \" on the same line.
            for (var i = start + 2; i + 1 < input.Length; i++)
            {
                if (input[i] == '\n')
                {
                    break;
                }

                if (input[i] == '\\' && input[i + 1] == '"')
                {
                    end = i + 2;
                    style = QuoteStyle.Escaped;
                    return true;
                }
            }
        }
        else if (span[0] is '"' or '\'')
        {
            var quote = span[0];
            for (var i = start + 1; i < input.Length; i++)
            {
                if (input[i] == '\\')
                {
                    i++; // skip the escaped character
                }
                else if (input[i] == quote)
                {
                    end = i + 1;
                    style = quote == '"' ? QuoteStyle.Double : QuoteStyle.Single;
                    return true;
                }
            }

            return false; // unterminated quoted value: leave it as is
        }

        // Bare value (also the fallback of an unterminated \" value, like the previous pattern).
        var bare = start;
        while (bare < input.Length && !char.IsWhiteSpace(input[bare]) && input[bare] is not (',' or '}' or ']' or '"'))
        {
            bare++;
        }

        end = bare;
        style = QuoteStyle.Bare;
        return bare > start;
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
