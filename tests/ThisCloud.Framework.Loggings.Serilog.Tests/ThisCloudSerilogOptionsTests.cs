using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

public sealed class ThisCloudSerilogOptionsTests
{
    [Fact]
    public void FromConfiguration_WithValidConfig_ReturnsConfiguredOptions()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["ThisCloud:Loggings:IsEnabled"] = "true",
            ["ThisCloud:Loggings:MinimumLevel"] = "Debug",
            ["ThisCloud:Loggings:Console:Enabled"] = "true",
            ["ASPNETCORE_ENVIRONMENT"] = "Staging"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, "test-service");

        // Assert
        options.Should().NotBeNull();
        options.ServiceName.Should().Be("test-service");
        options.Environment.Should().Be("Staging");
        options.Settings.IsEnabled.Should().BeTrue();
        options.Settings.MinimumLevel.Should().Be(LogLevel.Debug);
        options.Settings.Console.Enabled.Should().BeTrue();
    }

    [Fact]
    public void FromConfiguration_WithoutEnvironmentVariable_DefaultsToProduction()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, "prod-service");

        // Assert
        options.Environment.Should().Be("Production");
    }

    [Fact]
    public void FromConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => ThisCloudSerilogOptions.FromConfiguration(null!, "test-service");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void FromConfiguration_WithNullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        var act = () => ThisCloudSerilogOptions.FromConfiguration(configuration, null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*serviceName*");
    }

    [Fact]
    public void FromConfiguration_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        var act = () => ThisCloudSerilogOptions.FromConfiguration(configuration, "   ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*serviceName*");
    }

    [Fact]
    public void FromConfiguration_WithMissingSection_UsesDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, "default-service");

        // Assert
        options.Settings.Should().NotBeNull();
        options.Settings.IsEnabled.Should().BeTrue();
        options.Settings.MinimumLevel.Should().Be(LogLevel.Information);
    }
}
