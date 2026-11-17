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
        await service.EnableAsync(TestContext.Current.CancellationToken);

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
        await service.DisableAsync(TestContext.Current.CancellationToken);

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
        await service.SetSettingsAsync(newSettings, TestContext.Current.CancellationToken);

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
        await service.SetSettingsAsync(newSettings, TestContext.Current.CancellationToken);

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
        await service.PatchSettingsAsync(partialSettings, TestContext.Current.CancellationToken);

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
        await service.ResetSettingsAsync(TestContext.Current.CancellationToken);

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

    // ---------- S5: Enable restores the level active before Disable ----------

    [Theory]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    public async Task EnableAsync_AfterDisable_RestoresTheLevelActiveBeforeDisable(LogEventLevel level)
    {
        var levelSwitch = new LoggingLevelSwitch(level);
        var options = CreateOptions(LogLevel.Information);
        var service = new SerilogLoggingControlService(levelSwitch, options);

        await service.DisableAsync(TestContext.Current.CancellationToken);
        await service.EnableAsync(TestContext.Current.CancellationToken);

        levelSwitch.MinimumLevel.Should().Be(level);
        options.Settings.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task EnableAsync_AfterTwoDisables_RestoresTheOriginalLevelNotFatal()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
        var service = new SerilogLoggingControlService(levelSwitch, CreateOptions(LogLevel.Warning));

        await service.DisableAsync(TestContext.Current.CancellationToken);
        await service.DisableAsync(TestContext.Current.CancellationToken);
        await service.EnableAsync(TestContext.Current.CancellationToken);

        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public async Task EnableAsync_AfterDisableAndSetSettings_KeepsTheExplicitlyAppliedLevel()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
        var service = new SerilogLoggingControlService(levelSwitch, CreateOptions(LogLevel.Warning));

        await service.DisableAsync(TestContext.Current.CancellationToken);
        await service.SetSettingsAsync(new LogSettings { MinimumLevel = LogLevel.Debug }, TestContext.Current.CancellationToken);
        await service.EnableAsync(TestContext.Current.CancellationToken);

        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public async Task EnableAsync_WithoutPriorDisable_DoesNotTouchTheLevel()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
        var service = new SerilogLoggingControlService(levelSwitch, CreateOptions(LogLevel.Information));

        await service.EnableAsync(TestContext.Current.CancellationToken);

        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public async Task EnableAsync_CalledTwiceAfterDisable_KeepsTheRestoredLevel()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
        var service = new SerilogLoggingControlService(levelSwitch, CreateOptions(LogLevel.Warning));

        await service.DisableAsync(TestContext.Current.CancellationToken);
        await service.EnableAsync(TestContext.Current.CancellationToken);
        levelSwitch.MinimumLevel = LogEventLevel.Debug; // changed directly by the host after the first Enable
        await service.EnableAsync(TestContext.Current.CancellationToken);

        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
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
