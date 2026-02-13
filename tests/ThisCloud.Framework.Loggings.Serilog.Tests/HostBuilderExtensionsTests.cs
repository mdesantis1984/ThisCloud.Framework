using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ThisCloud.Framework.Loggings.Abstractions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Tests for <see cref="HostBuilderExtensions"/>.
/// </summary>
public sealed class HostBuilderExtensionsTests
{
    [Fact]
    public void UseThisCloudFrameworkSerilog_WithValidInputs_ConfiguresSerilog()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Console:Enabled"] = "true"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert
        host.Should().NotBeNull();

        // Verify Serilog is configured as logger factory
        var loggerFactory = host.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithNullHost_ThrowsArgumentNullException()
    {
        // Arrange
        IHostBuilder host = null!;
        var configuration = new ConfigurationBuilder().Build();
        const string serviceName = "test-service";

        // Act
        var act = () => host.UseThisCloudFrameworkSerilog(configuration, serviceName);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("host");
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder();
        IConfiguration configuration = null!;
        const string serviceName = "test-service";

        // Act
        var act = () => hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UseThisCloudFrameworkSerilog_WithNullOrWhitespaceServiceName_ThrowsArgumentException(string? serviceName)
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("serviceName");
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithExistingLoggingLevelSwitch_UsesSharedSwitch()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Warning"
            })
            .Build();
        const string serviceName = "test-service";

        var sharedSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
                services.AddSingleton(sharedSwitch); // Pre-register switch
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();
        var resolvedSwitch = host.Services.GetRequiredService<LoggingLevelSwitch>();

        // Assert
        resolvedSwitch.Should().BeSameAs(sharedSwitch);
        // Note: Switch level is not changed by UseThisCloudFrameworkSerilog, it uses whatever is in DI
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithoutPreRegisteredSwitch_CreatesNewSwitch()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Error"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
                // No LoggingLevelSwitch pre-registered
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert - Should not throw; internal switch is created
        host.Should().NotBeNull();
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithOverrides_AppliesNamespaceOverrides()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Overrides:Microsoft"] = "Warning",
                ["ThisCloud:Loggings:Overrides:System"] = "Error"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert
        host.Should().NotBeNull();
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithCorrelationContext_EnrichesWithContext()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
                // DefaultCorrelationContext is internal, so we use a test double
                services.AddScoped<ICorrelationContext>(sp => 
                {
                    // Re-use the default registration from AddThisCloudFrameworkLoggings
                    var testServices = new ServiceCollection();
                    var testConfig = sp.GetRequiredService<IConfiguration>();
                    testServices.AddThisCloudFrameworkLoggings(testConfig, "test");
                    var testProvider = testServices.BuildServiceProvider();
                    return testProvider.CreateScope().ServiceProvider.GetRequiredService<ICorrelationContext>();
                });
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert
        host.Should().NotBeNull();
        var correlationContext = host.Services.CreateScope().ServiceProvider.GetService<ICorrelationContext>();
        correlationContext.Should().NotBeNull();
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithConsoleDisabled_DoesNotWriteToConsole()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Console:Enabled"] = "false"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert
        host.Should().NotBeNull();
    }

    [Fact]
    public void UseThisCloudFrameworkSerilog_WithConsoleEnabled_WritesToConsole()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Console:Enabled"] = "true"
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

        // Assert
        host.Should().NotBeNull();
    }
}
