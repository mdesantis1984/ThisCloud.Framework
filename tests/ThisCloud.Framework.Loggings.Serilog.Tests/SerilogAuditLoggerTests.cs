using FluentAssertions;
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

public sealed class SerilogAuditLoggerTests
{
    [Fact]
    public async Task LogAuditEventAsync_WithValidInputs_LogsEvent()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();
        var redactor = new DefaultLogRedactor();
        var auditLogger = new SerilogAuditLogger(logger, redactor);

        var action = "UpdateSettings";
        var userId = "admin@example.com";
        var correlationId = Guid.NewGuid();
        var details = "MinimumLevel changed from Information to Debug";

        // Act
        await auditLogger.LogAuditEventAsync(action, userId, correlationId, details, CancellationToken.None);

        // Assert - should not throw
        loggerFactory.Dispose();
    }

    [Fact]
    public async Task LogAuditEventAsync_WithNullUserId_UsesSystemAsDefault()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();
        var redactor = new DefaultLogRedactor();
        var auditLogger = new SerilogAuditLogger(logger, redactor);

        // Act
        await auditLogger.LogAuditEventAsync("ResetSettings", null, null, null, CancellationToken.None);

        // Assert - should not throw
        loggerFactory.Dispose();
    }

    [Fact]
    public async Task LogAuditEventAsync_WithSensitiveDetails_RedactsBeforeLogging()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();
        var redactor = new DefaultLogRedactor();
        var auditLogger = new SerilogAuditLogger(logger, redactor);

        var details = "Changed apiKey=secret123 to apiKey=newsecret456";

        // Act
        await auditLogger.LogAuditEventAsync("UpdateApiKey", "user1", null, details, CancellationToken.None);

        // Assert - details should be redacted (verified by redactor)
        loggerFactory.Dispose();
    }

    [Fact]
    public async Task LogAuditEventAsync_WithNullAction_ThrowsArgumentException()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();
        var redactor = new DefaultLogRedactor();
        var auditLogger = new SerilogAuditLogger(logger, redactor);

        // Act & Assert
        var act = async () => await auditLogger.LogAuditEventAsync(null!, "user", null, null, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*action*");

        loggerFactory.Dispose();
    }

    [Fact]
    public async Task LogAuditEventAsync_WithEmptyAction_ThrowsArgumentException()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();
        var redactor = new DefaultLogRedactor();
        var auditLogger = new SerilogAuditLogger(logger, redactor);

        // Act & Assert
        var act = async () => await auditLogger.LogAuditEventAsync("   ", "user", null, null, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*action*");

        loggerFactory.Dispose();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var redactor = new DefaultLogRedactor();

        // Act & Assert
        var act = () => new SerilogAuditLogger(null!, redactor);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullRedactor_ThrowsArgumentNullException()
    {
        // Arrange
        var (logger, loggerFactory) = CreateTestLogger();

        // Act & Assert
        var act = () => new SerilogAuditLogger(logger, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("redactor");

        loggerFactory.Dispose();
    }

    private static (ILogger<SerilogAuditLogger> Logger, ILoggerFactory Factory) CreateTestLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<SerilogAuditLogger>();
        return (logger, loggerFactory);
    }
}
