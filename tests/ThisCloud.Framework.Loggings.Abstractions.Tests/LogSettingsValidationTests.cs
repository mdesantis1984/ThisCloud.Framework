using Xunit;
using FluentAssertions;

namespace ThisCloud.Framework.Loggings.Abstractions.Tests;

public sealed class LogSettingsValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public void FileSinkSettings_RollingFileSizeMb_ShouldThrowForInvalidValues(int invalidValue)
    {
        // Arrange
        var settings = new FileSinkSettings();

        // Act
        var act = () => settings.RollingFileSizeMb = invalidValue;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(FileSinkSettings.RollingFileSizeMb))
            .WithMessage("*must be between 1 and 100*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void FileSinkSettings_RollingFileSizeMb_ShouldAcceptValidValues(int validValue)
    {
        // Arrange
        var settings = new FileSinkSettings();

        // Act
        settings.RollingFileSizeMb = validValue;

        // Assert
        settings.RollingFileSizeMb.Should().Be(validValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(1000)]
    public void FileSinkSettings_RetainedFileCountLimit_ShouldThrowForInvalidValues(int invalidValue)
    {
        // Arrange
        var settings = new FileSinkSettings();

        // Act
        var act = () => settings.RetainedFileCountLimit = invalidValue;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(FileSinkSettings.RetainedFileCountLimit))
            .WithMessage("*must be between 1 and 365*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(180)]
    [InlineData(365)]
    public void FileSinkSettings_RetainedFileCountLimit_ShouldAcceptValidValues(int validValue)
    {
        // Arrange
        var settings = new FileSinkSettings();

        // Act
        settings.RetainedFileCountLimit = validValue;

        // Assert
        settings.RetainedFileCountLimit.Should().Be(validValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    [InlineData(10000)]
    public void RetentionSettings_Days_ShouldThrowForInvalidValues(int invalidValue)
    {
        // Arrange
        var settings = new RetentionSettings();

        // Act
        var act = () => settings.Days = invalidValue;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(RetentionSettings.Days))
            .WithMessage("*must be between 1 and 3650*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    [InlineData(3650)]
    public void RetentionSettings_Days_ShouldAcceptValidValues(int validValue)
    {
        // Arrange
        var settings = new RetentionSettings();

        // Act
        settings.Days = validValue;

        // Assert
        settings.Days.Should().Be(validValue);
    }

    [Fact]
    public void FileSinkSettings_ShouldAllowModifyingOtherPropertiesWithoutValidation()
    {
        // Arrange
        var settings = new FileSinkSettings
        {
            Enabled = false,
            Path = "custom/path.log",
            UseCompactJson = false
        };

        // Assert
        settings.Enabled.Should().BeFalse();
        settings.Path.Should().Be("custom/path.log");
        settings.UseCompactJson.Should().BeFalse();
    }
}
