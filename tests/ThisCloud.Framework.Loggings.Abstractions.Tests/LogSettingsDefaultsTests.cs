using Xunit;
using FluentAssertions;

namespace ThisCloud.Framework.Loggings.Abstractions.Tests;

public sealed class LogSettingsDefaultsTests
{
    [Fact]
    public void LogSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new LogSettings();

        // Assert
        settings.IsEnabled.Should().BeTrue();
        settings.MinimumLevel.Should().Be(LogLevel.Information);
        settings.Overrides.Should().BeNull();
        settings.Console.Should().NotBeNull();
        settings.File.Should().NotBeNull();
        settings.Retention.Should().NotBeNull();
        settings.Redaction.Should().NotBeNull();
        settings.Correlation.Should().NotBeNull();
    }

    [Fact]
    public void ConsoleSinkSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new ConsoleSinkSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
    }

    [Fact]
    public void FileSinkSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new FileSinkSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.Path.Should().Be("logs/log-.ndjson");
        settings.RollingFileSizeMb.Should().Be(10);
        settings.RetainedFileCountLimit.Should().Be(30);
        settings.UseCompactJson.Should().BeTrue();
    }

    [Fact]
    public void RetentionSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new RetentionSettings();

        // Assert
        settings.Days.Should().Be(30);
    }

    [Fact]
    public void RedactionSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new RedactionSettings();

        // Assert
        settings.Enabled.Should().BeTrue();
        settings.AdditionalPatterns.Should().BeNull();
    }

    [Fact]
    public void CorrelationSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new CorrelationSettings();

        // Assert
        settings.HeaderName.Should().Be("X-Correlation-Id");
        settings.GenerateIfMissing.Should().BeTrue();
    }

    [Fact]
    public void LogSettings_ShouldAllowSettingProperties()
    {
        // Arrange
        var settings = new LogSettings
        {
            IsEnabled = false,
            MinimumLevel = LogLevel.Warning,
            Overrides = new Dictionary<string, LogLevel>
            {
                { "MyApp.Database", LogLevel.Debug }
            }
        };

        // Assert
        settings.IsEnabled.Should().BeFalse();
        settings.MinimumLevel.Should().Be(LogLevel.Warning);
        settings.Overrides.Should().ContainKey("MyApp.Database");
        settings.Overrides!["MyApp.Database"].Should().Be(LogLevel.Debug);
    }
}
