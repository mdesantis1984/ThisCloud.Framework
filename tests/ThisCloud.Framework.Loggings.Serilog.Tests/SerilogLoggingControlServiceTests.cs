using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

public sealed class SerilogLoggingControlServiceTests
{
    [Fact]
    public async Task EnableAsync_SetsIsEnabledToTrue()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        // Act
        await service.EnableAsync();

        // Assert
        options.Settings.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task DisableAsync_SetsIsEnabledToFalseAndLevelToFatal()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        // Act
        await service.DisableAsync();

        // Assert
        options.Settings.IsEnabled.Should().BeFalse();
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Fatal);
    }

    [Fact]
    public async Task SetSettingsAsync_UpdatesLevelSwitch()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        var newSettings = new LogSettings
        {
            IsEnabled = true,
            MinimumLevel = LogLevel.Debug
        };

        // Act
        await service.SetSettingsAsync(newSettings);

        // Assert
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
    }

    [Theory]
    [InlineData(LogLevel.Verbose, LogEventLevel.Verbose)]
    [InlineData(LogLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LogLevel.Information, LogEventLevel.Information)]
    [InlineData(LogLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LogLevel.Error, LogEventLevel.Error)]
    [InlineData(LogLevel.Critical, LogEventLevel.Fatal)]
    public async Task SetSettingsAsync_WithDifferentLevels_MapsCorrectly(LogLevel input, LogEventLevel expected)
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        var newSettings = new LogSettings { MinimumLevel = input };

        // Act
        await service.SetSettingsAsync(newSettings);

        // Assert
        levelSwitch.MinimumLevel.Should().Be(expected);
    }

    [Fact]
    public async Task PatchSettingsAsync_MergesPartialSettings()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        var partialSettings = new LogSettings
        {
            MinimumLevel = LogLevel.Warning
        };

        // Act
        await service.PatchSettingsAsync(partialSettings);

        // Assert
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public async Task ResetSettingsAsync_RestoresDefaults()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
        var options = CreateOptions(LogLevel.Debug);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        // Act
        await service.ResetSettingsAsync();

        // Assert
        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information); // LogSettings default
    }

    [Fact]
    public async Task SetSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        // Act & Assert
        var act = async () => await service.SetSettingsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task PatchSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        // Act & Assert
        var act = async () => await service.PatchSettingsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("partialSettings");
    }

    [Fact]
    public void Constructor_WithNullLevelSwitch_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateOptions(LogLevel.Information);

        // Act & Assert
        var act = () => new SerilogLoggingControlService(null!, options);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("globalLevelSwitch");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        // Act & Assert
        var act = () => new SerilogLoggingControlService(levelSwitch, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    private static ThisCloudSerilogOptions CreateOptions(LogLevel minimumLevel)
    {
        return new ThisCloudSerilogOptions
        {
            ServiceName = "test-service",
            Environment = "Test",
            Settings = new LogSettings
            {
                IsEnabled = true,
                MinimumLevel = minimumLevel
            }
        };
    }
}
