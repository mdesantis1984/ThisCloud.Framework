using System.Text.RegularExpressions;
using FluentAssertions;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

public sealed class DefaultLogRedactorTests
{
    [Fact]
    public void Redact_WithAuthorizationBearer_RedactsToken()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var message = "Request headers: Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void Redact_WithJwtToken_RedactsJwt()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var message = "Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED_JWT]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Theory]
    [InlineData("apiKey=secret123")]
    [InlineData("password=mypass")]
    [InlineData("token: value123")]
    [InlineData("secret: mysecret")]
    public void Redact_WithKeyValueSecrets_RedactsValues(string input)
    {
        // Arrange
        var redactor = new DefaultLogRedactor();

        // Act
        var result = redactor.Redact(input);

        // Assert
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void Redact_WithEmail_RedactsPII()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var message = "User email: test@example.com reported an issue";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED_PII]");
        result.Should().NotContain("test@example.com");
    }

    [Fact]
    public void Redact_WithPhoneNumber_RedactsPII()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var message = "Contact: +34-123-456-789";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED_PII]");
    }

    [Fact]
    public void Redact_WithDniNie_RedactsPII()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var message = "DNI: 12345678A";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED_PII]");
        result.Should().NotContain("12345678A");
    }

    [Fact]
    public void Redact_WithNullMessage_ReturnsEmptyString()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();

        // Act
        var result = redactor.Redact(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Redact_WithEmptyMessage_ReturnsEmptyString()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();

        // Act
        var result = redactor.Redact("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Redact_WithAdditionalPatterns_AppliesCustomRedaction()
    {
        // Arrange
        var additionalPatterns = new[] { @"CustomSecret:\s*\S+" };
        var redactor = new DefaultLogRedactor(additionalPatterns);
        var message = "Config: CustomSecret: MyValue123";

        // Act
        var result = redactor.Redact(message);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("MyValue123");
    }

    [Fact]
    public void RedactProperties_WithSensitiveKeys_RedactsValues()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var properties = new Dictionary<string, object?>
        {
            ["username"] = "john",
            ["password"] = "secret123",
            ["apiKey"] = "key-abc-xyz",
            ["message"] = "normal text"
        };

        // Act
        var result = redactor.RedactProperties(properties);

        // Assert
        result["username"].Should().Be("john");
        result["password"].Should().Be("[REDACTED]");
        result["apiKey"].Should().Be("[REDACTED]");
        result["message"].Should().Be("normal text");
    }

    [Fact]
    public void RedactProperties_WithStringValues_AppliesRedaction()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var properties = new Dictionary<string, object?>
        {
            ["header"] = "Authorization: Bearer token123"
        };

        // Act
        var result = redactor.RedactProperties(properties);

        // Assert
        result["header"].Should().NotBeNull();
        result["header"]!.ToString().Should().Contain("[REDACTED]");
    }

    [Fact]
    public void RedactProperties_WithNullProperties_ReturnsEmptyDictionary()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();

        // Act
        var result = redactor.RedactProperties(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RedactProperties_WithEmptyProperties_ReturnsEmptyDictionary()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();
        var properties = new Dictionary<string, object?>();

        // Act
        var result = redactor.RedactProperties(properties);

        // Assert
        result.Should().BeEmpty();
    }

    // ---------- S3: quoted JSON keys/values ----------

    [Theory]
    [InlineData("{\"password\":\"x\"}", "{\"password\":\"[REDACTED]\"}")]
    [InlineData("{\"apiKey\": \"x\"}", "{\"apiKey\": \"[REDACTED]\"}")]
    [InlineData("{\"authorization\":\"Bearer x\"}", "{\"authorization\":\"Bearer [REDACTED]\"}")]
    [InlineData("{\"Authorization\": \"Bearer abc.def-ghi\"}", "{\"Authorization\": \"Bearer [REDACTED]\"}")]
    [InlineData("{\"user\":\"bob\",\"token\":\"abc123\",\"n\":1}", "{\"user\":\"bob\",\"token\":\"[REDACTED]\",\"n\":1}")]
    [InlineData("{\"client_secret\":\"two words\"}", "{\"client_secret\":\"[REDACTED]\"}")]
    [InlineData("{\"Password\" : \"p@ss, word\"}", "{\"Password\" : \"[REDACTED]\"}")]
    public void Redact_WithQuotedJsonSecrets_RedactsValueAndKeepsJsonShape(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    [Fact]
    public void Redact_WithQuotedJsonSecretsAmongOtherFields_LeavesOtherFieldsUntouched()
    {
        var redactor = new DefaultLogRedactor();

        var result = redactor.Redact("{\"name\":\"alice\",\"password\":\"hunter2\",\"role\":\"admin\"}");

        result.Should().Be("{\"name\":\"alice\",\"password\":\"[REDACTED]\",\"role\":\"admin\"}");
    }

    // ---------- S3: phone pattern must not be greedy ----------

    [Theory]
    [InlineData("order 12345 shipped")]
    [InlineData("user id 987654 created")]
    [InlineData("at 2026-11-16T10:30:45Z done")]
    [InlineData("at 2026-11-16 10:30:45 done")]
    [InlineData("date 2026-11-16 only")]
    [InlineData("guid 3f2504e0-4f89-11d3-9a0c-0305e82c3301 end")]
    [InlineData("guid 12345678-1234-1234-1234-123456789012 end")]
    [InlineData("epoch 1763289045123 end")]
    public void Redact_WithNonPhoneNumbers_LeavesTextUntouched(string input)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(input);
    }

    [Theory]
    [InlineData("Contact: +34-123-456-789", "Contact: [REDACTED_PII]")]
    [InlineData("Contact: +34 600 123 456 now", "Contact: [REDACTED_PII] now")]
    [InlineData("Call (555) 123-4567.", "Call [REDACTED_PII].")]
    [InlineData("Call 555-123-4567 today", "Call [REDACTED_PII] today")]
    [InlineData("Call 600123456 today", "Call [REDACTED_PII] today")]
    [InlineData("DNI: 12345678A", "DNI: [REDACTED_PII]")]
    public void Redact_WithPhoneNumber_RedactsOnlyThePhone(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    [Fact]
    public void Redact_WithPhoneNextToIdsAndTimestamps_RedactsOnlyThePhone()
    {
        var redactor = new DefaultLogRedactor();

        var result = redactor.Redact("order 12345 at 2026-11-16 10:30:45 id 3f2504e0-4f89-11d3-9a0c-0305e82c3301 phone +34-123-456-789 ok");

        result.Should().Be("order 12345 at 2026-11-16 10:30:45 id 3f2504e0-4f89-11d3-9a0c-0305e82c3301 phone [REDACTED_PII] ok");
    }

    // ---------- PRESERVE: redactions that already worked keep their exact output ----------

    [Theory]
    [InlineData("apiKey=secret123", "apiKey[REDACTED]")]
    [InlineData("password=mypass", "password[REDACTED]")]
    [InlineData("token: value123 next", "token[REDACTED] next")]
    [InlineData("Request headers: Authorization: Bearer abc.def", "Request headers: Authorization: Bearer [REDACTED]")]
    [InlineData("User email: test@example.com reported", "User email: [REDACTED_PII] reported")]
    [InlineData("password=[REDACTED]", "password=[REDACTED]")]
    public void Redact_ExistingPlainTextPatterns_KeepExactOutput(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    // ---------- S3: escaped JSON inside strings (value replaced, escaping kept valid) ----------

    [Theory]
    [InlineData("""{\"password\":\"x\"}""", """{\"password\":\"[REDACTED]\"}""")]
    [InlineData("""{\"apiKey\": \"x\"}""", """{\"apiKey\": \"[REDACTED]\"}""")]
    [InlineData("""{\"authorization\":\"Bearer x\"}""", """{\"authorization\":\"Bearer [REDACTED]\"}""")]
    [InlineData("""{\"Authorization\": \"Bearer abc.def-ghi\"}""", """{\"Authorization\": \"Bearer [REDACTED]\"}""")]
    [InlineData("""{\"client_secret\":\"two words\"}""", """{\"client_secret\":\"[REDACTED]\"}""")]
    [InlineData("""{\"user\":\"bob\",\"token\":\"abc123\",\"n\":1}""", """{\"user\":\"bob\",\"token\":\"[REDACTED]\",\"n\":1}""")]
    [InlineData("""{\"password\":\"\"}""", """{\"password\":\"[REDACTED]\"}""")]
    public void Redact_WithEscapedJsonSecrets_RedactsValueAndKeepsEscaping(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    [Fact]
    public void Redact_WithEscapedJsonSecrets_KeepsTheStringLiteralAndInnerJsonValid()
    {
        var redactor = new DefaultLogRedactor();
        var logged = """{\"user\":\"bob\",\"password\":\"hunter2\",\"authorization\":\"Bearer abc\"}""";

        var redacted = redactor.Redact(logged);

        // The escaped text is the body of a JSON string literal: it must still unescape and parse.
        var inner = System.Text.Json.JsonSerializer.Deserialize<string>("\"" + redacted + "\"");
        using var json = System.Text.Json.JsonDocument.Parse(inner!);
        json.RootElement.GetProperty("user").GetString().Should().Be("bob");
        json.RootElement.GetProperty("password").GetString().Should().Be("[REDACTED]");
        json.RootElement.GetProperty("authorization").GetString().Should().Be("Bearer [REDACTED]");
    }

    // ---------- S3: single-quoted keys/values ----------

    [Theory]
    [InlineData("{'password':'x'}", "{'password':'[REDACTED]'}")]
    [InlineData("{'apiKey': 'x'}", "{'apiKey': '[REDACTED]'}")]
    [InlineData("{'authorization':'Bearer x'}", "{'authorization':'Bearer [REDACTED]'}")]
    [InlineData("{'client_secret':'two words', 'n': 1}", "{'client_secret':'[REDACTED]', 'n': 1}")]
    [InlineData("{'user': 'bob', 'token': 'abc123'}", "{'user': 'bob', 'token': '[REDACTED]'}")]
    [InlineData("{'password':''}", "{'password':'[REDACTED]'}")]
    [InlineData("{'password': 'it\\'s secret'}", "{'password': '[REDACTED]'}")]
    public void Redact_WithSingleQuotedSecrets_RedactsValueAndKeepsQuotes(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    // ---------- S3: IPv4 addresses are not phones ----------

    [Theory]
    [InlineData("from 192.168.100.200 ok")]
    [InlineData("from 10.0.0.1 ok")]
    [InlineData("from 255.255.255.255 ok")]
    [InlineData("from 172.16.254.100 ok")]
    [InlineData("peer 192.168.100.200:8080 up")]
    [InlineData("net 192.168.100.200/24 up")]
    [InlineData("client=192.168.100.200, server=10.20.30.40.")]
    [InlineData("{\"ip\":\"192.168.100.200\"}")]
    public void Redact_WithIpv4Address_LeavesTextUntouched(string input)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(input);
    }

    [Fact]
    public void Redact_WithIpv4NextToAPhone_RedactsOnlyThePhone()
    {
        var redactor = new DefaultLogRedactor();

        var result = redactor.Redact("from 192.168.100.200 phone +34-123-456-789 and 555-123-4567 via 10.0.0.1");

        result.Should().Be("from 192.168.100.200 phone [REDACTED_PII] and [REDACTED_PII] via 10.0.0.1");
    }

    // ---------- A1: built-in patterns are linear (no ReDoS) and Redact never throws on a regex timeout ----------

    [Theory]
    [InlineData("quote-token")]
    [InlineData("single-quote-secret")]
    [InlineData("quote-password-dash")]
    [InlineData("escaped-quote-token")]
    [InlineData("quote-apikey-dot")]
    [InlineData("authorization-bearer-spaces")]
    [InlineData("authorization-bearer-open-value")]
    [InlineData("escaped-authorization-repeated")]
    [InlineData("single-authorization-repeated")]
    [InlineData("header-bearer-spaces")]
    [InlineData("header-authorization-repeated")]
    [InlineData("jwt-prefix-repeated")]
    [InlineData("jwt-two-parts")]
    [InlineData("email-dotted-local-part")]
    [InlineData("email-at-signs")]
    [InlineData("key-value-spaces")]
    [InlineData("key-value-repeated")]
    [InlineData("json-key-spaces")]
    [InlineData("json-value-open-double")]
    [InlineData("json-value-open-single")]
    [InlineData("json-value-backslashes")]
    [InlineData("phone-digit-groups")]
    [InlineData("phone-plus-groups")]
    [InlineData("phone-ipv4-lookalike")]
    [InlineData("phone-parentheses")]
    [InlineData("dni-digits")]
    [InlineData("quotes-only")]
    public void Redact_WithAdversarialInput_DoesNotThrowAndIsLinear(string kind)
    {
        var input = BuildAdversarialInput(kind);
        input.Length.Should().BeGreaterThanOrEqualTo(30_000);
        var redactor = new DefaultLogRedactor();
        redactor.Redact(WarmUpSample); // JIT / regex construction must not count against the budget

        string? result = null;
        var full = BestOf(3, () => result = redactor.Redact(input));
        var half = BestOf(3, () => redactor.Redact(BuildAdversarialInput(kind, 80_000)));

        result.Should().NotBeNull();
        // Wall-clock budgets alone are flaky on shared CI runners, so linearity is checked by growth:
        // doubling the input roughly doubles the time when linear and quadruples it when quadratic.
        full.Should().BeLessThan(TimeSpan.FromSeconds(2), "built-in patterns must stay fast on {0}", kind);
        if (full > TimeSpan.FromMilliseconds(20))
        {
            (full.TotalMilliseconds / Math.Max(half.TotalMilliseconds, 1)).Should()
                .BeLessThan(3, "doubling the input must not quadruple the time on {0} (half {1}, full {2})", kind, half, full);
        }
    }

    private static TimeSpan BestOf(int runs, Action action) =>
        Enumerable.Range(0, runs).Select(_ => Time(action)).Min();

    [Fact]
    public void Redact_WhenABuiltInPatternTimesOut_ReturnsAFullyRedactedPlaceholderInsteadOfThrowing()
    {
        var redactor = new DefaultLogRedactor(null, _ => throw new RegexMatchTimeoutException());

        var act = () => redactor.Redact("some message with user@example.com");

        act.Should().NotThrow();
        redactor.Redact("some message with user@example.com").Should().Be("[REDACTED]");
    }

    [Fact]
    public void RedactProperties_WhenABuiltInPatternTimesOut_RedactsTheValueInsteadOfThrowing()
    {
        var redactor = new DefaultLogRedactor(null, _ => throw new RegexMatchTimeoutException());

        var result = redactor.RedactProperties(new Dictionary<string, object?> { ["msg"] = "hello", ["n"] = 5 });

        result["msg"].Should().Be("[REDACTED]");
        result["n"].Should().Be(5);
    }

    // ---------- extra probes ----------

    [Theory]
    [InlineData("{\"password\":123}", "{\"password\":\"[REDACTED]\"}")]
    [InlineData("{\"msg\":\"a\",\"password\":\"a,b}c\"}", "{\"msg\":\"a\",\"password\":\"[REDACTED]\"}")]
    [InlineData("ts 2026-11-16 10:20:30.1234567 +01:00", "ts 2026-11-16 10:20:30.1234567 +01:00")]
    [InlineData("{\"msg\":\"{\\\"password\\\":\\\"x\\\"}\"}", "{\"msg\":\"{\\\"password\\\":\\\"[REDACTED]\\\"}\"}")]
    public void Redact_ExtraProbes_KeepExactOutput(string input, string expected)
    {
        var redactor = new DefaultLogRedactor();

        redactor.Redact(input).Should().Be(expected);
    }

    private const string WarmUpSample =
        "Authorization: Bearer abc {\"password\":\"x\"} {\"authorization\":\"Bearer x\"} {'token':'x'} " +
        "{\\\"secret\\\":\\\"x\\\"} apiKey=1 a@b.com +34-123-456-789 192.168.1.1 12345678A " +
        "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.abc";

    private static TimeSpan Time(Action action)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        action();
        return watch.Elapsed;
    }

    private static string Rep(string unit, int length) =>
        string.Concat(Enumerable.Repeat(unit, (length / unit.Length) + 1));

    private static string BuildAdversarialInput(string kind, int length = 160_000) => kind switch
    {
        "quote-token" => "\"" + Rep("token", length),
        "single-quote-secret" => "'" + Rep("secret", length),
        "quote-password-dash" => "\"" + Rep("password-", length),
        "escaped-quote-token" => "\\\"" + Rep("token", length),
        "quote-apikey-dot" => "\"" + Rep("apiKey.", length),
        "authorization-bearer-spaces" => "\"authorization\":\"Bearer" + new string(' ', length),
        "authorization-bearer-open-value" => "\"authorization\":\"Bearer " + new string('a', length),
        "escaped-authorization-repeated" => Rep("\\\"authorization\\\":\\\"Bearer a ", length),
        "single-authorization-repeated" => Rep("'authorization':'Bearer a ", length),
        "header-bearer-spaces" => "Authorization: Bearer" + new string(' ', length),
        "header-authorization-repeated" => Rep("Authorization: ", length),
        "jwt-prefix-repeated" => Rep("eyJ", length),
        "jwt-two-parts" => Rep("eyJa.eyJb", length),
        "email-dotted-local-part" => Rep("a.", length),
        "email-at-signs" => Rep("a@", length),
        "key-value-spaces" => "token" + new string(' ', length),
        "key-value-repeated" => Rep("password ", length),
        "json-key-spaces" => "\"token\"" + new string(' ', length),
        "json-value-open-double" => "\"password\":\"" + new string('a', length),
        "json-value-open-single" => "'password':'" + new string('a', length),
        "json-value-backslashes" => "\"password\":\"" + new string('\\', length),
        "phone-digit-groups" => Rep("12 ", length),
        "phone-plus-groups" => Rep("+1 2 ", length),
        "phone-ipv4-lookalike" => Rep("192.168.1.", length),
        "phone-parentheses" => Rep("(12)", length),
        "dni-digits" => Rep("12345678", length),
        "quotes-only" => Rep("\"'", length),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
