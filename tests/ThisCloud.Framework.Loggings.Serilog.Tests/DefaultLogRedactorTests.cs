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
}
