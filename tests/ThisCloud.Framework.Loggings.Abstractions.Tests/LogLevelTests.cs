using Xunit;
using FluentAssertions;

namespace ThisCloud.Framework.Loggings.Abstractions.Tests;

public sealed class LogLevelTests
{
    [Fact]
    public void LogLevel_ShouldHaveVerboseAsZero()
    {
        // Arrange & Act
        var level = LogLevel.Verbose;

        // Assert
        ((int)level).Should().Be(0);
    }

    [Fact]
    public void LogLevel_ShouldHaveDebugAsOne()
    {
        // Arrange & Act
        var level = LogLevel.Debug;

        // Assert
        ((int)level).Should().Be(1);
    }

    [Fact]
    public void LogLevel_ShouldHaveInformationAsTwo()
    {
        // Arrange & Act
        var level = LogLevel.Information;

        // Assert
        ((int)level).Should().Be(2);
    }

    [Fact]
    public void LogLevel_ShouldHaveWarningAsThree()
    {
        // Arrange & Act
        var level = LogLevel.Warning;

        // Assert
        ((int)level).Should().Be(3);
    }

    [Fact]
    public void LogLevel_ShouldHaveErrorAsFour()
    {
        // Arrange & Act
        var level = LogLevel.Error;

        // Assert
        ((int)level).Should().Be(4);
    }

    [Fact]
    public void LogLevel_ShouldHaveCriticalAsFive()
    {
        // Arrange & Act
        var level = LogLevel.Critical;

        // Assert
        ((int)level).Should().Be(5);
    }

    [Fact]
    public void LogLevel_ShouldHaveSixValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<LogLevel>();

        // Assert
        values.Should().HaveCount(6);
    }

    [Fact]
    public void LogLevel_ShouldBeInCorrectOrder()
    {
        // Arrange & Act
        var values = Enum.GetValues<LogLevel>();

        // Assert
        values.Should().ContainInOrder(
            LogLevel.Verbose,
            LogLevel.Debug,
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical);
    }
}
