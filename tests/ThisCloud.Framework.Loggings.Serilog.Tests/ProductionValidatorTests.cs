using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ThisCloud.Framework.Loggings.Abstractions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Unit tests for ProductionValidator (L3.3 - Fail-fast validation).
/// Tests use stub IHostEnvironment to avoid Host.CreateDefaultBuilder complexity.
/// </summary>
public sealed class ProductionValidatorTests
{
    [Fact]
    public void ValidateProductionSettings_Production_FileEnabledFalse_ThrowsInvalidOperationException()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        var settings = new LogSettings
        {
            File = { Enabled = false }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File sink must be enabled in Production*");
    }

    [Fact]
    public void ValidateProductionSettings_Production_FilePathEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        var settings = new LogSettings
        {
            File = { Enabled = true, Path = "" }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File sink path must be configured in Production*");
    }

    [Fact]
    public void ValidateProductionSettings_Production_FilePathWhitespace_ThrowsInvalidOperationException()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        var settings = new LogSettings
        {
            File = { Enabled = true, Path = "   " }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File sink path must be configured in Production*");
    }

    [Fact]
    public void ValidateProductionSettings_Production_FilePathNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        var settings = new LogSettings
        {
            File = { Enabled = true, Path = null! }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File sink path must be configured in Production*");
    }

    [Fact]
    public void ValidateProductionSettings_Production_ValidFileConfig_DoesNotThrow()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        var settings = new LogSettings
        {
            File = { Enabled = true, Path = "logs/test.log" }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProductionSettings_Development_FileEnabledFalse_DoesNotThrow()
    {
        // Arrange
        var environment = CreateStubEnvironment("Development");
        var settings = new LogSettings
        {
            File = { Enabled = false }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProductionSettings_Staging_FileEnabledFalse_DoesNotThrow()
    {
        // Arrange
        var environment = CreateStubEnvironment("Staging");
        var settings = new LogSettings
        {
            File = { Enabled = false }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateProductionSettings_ProductionCaseInsensitive_FileEnabledFalse_ThrowsInvalidOperationException()
    {
        // Arrange
        var environment = CreateStubEnvironment("PRODUCTION"); // Uppercase
        var settings = new LogSettings
        {
            File = { Enabled = false }
        };

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*File sink must be enabled in Production*");
    }

    [Fact]
    public void ValidateProductionSettings_NullEnvironment_ThrowsArgumentNullException()
    {
        // Arrange
        IHostEnvironment environment = null!;
        var settings = new LogSettings();

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("environment");
    }

    [Fact]
    public void ValidateProductionSettings_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var environment = CreateStubEnvironment("Production");
        LogSettings settings = null!;

        // Act
        var act = () => ProductionValidator.ValidateProductionSettings(environment, settings);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }

    /// <summary>
    /// Creates a stub IHostEnvironment for testing without full host builder setup.
    /// </summary>
    private static IHostEnvironment CreateStubEnvironment(string environmentName)
    {
        return new StubHostEnvironment { EnvironmentName = environmentName };
    }

    /// <summary>
    /// Minimal IHostEnvironment stub for unit tests.
    /// </summary>
    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
