using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using ThisCloud.Framework.Loggings.Abstractions;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddThisCloudFrameworkLoggings_WithValidInputs_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for ILogger<T> dependency
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Information"
            })
            .Build();
        const string serviceName = "test-service";

        // Act
        services.AddThisCloudFrameworkLoggings(configuration, serviceName);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<ThisCloudSerilogOptions>().Should().NotBeNull();
        provider.GetService<LoggingLevelSwitch>().Should().NotBeNull();
        provider.GetService<ILogRedactor>().Should().NotBeNull().And.BeOfType<DefaultLogRedactor>();
        provider.GetService<IAuditLogger>().Should().NotBeNull().And.BeOfType<SerilogAuditLogger>();
        provider.GetService<ILoggingControlService>().Should().NotBeNull().And.BeOfType<SerilogLoggingControlService>();

        // ICorrelationContext is scoped, need scope
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<ICorrelationContext>().Should().NotBeNull();
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var configuration = new ConfigurationBuilder().Build();
        const string serviceName = "test-service";

        // Act
        var act = () => services.AddThisCloudFrameworkLoggings(configuration, serviceName);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = null!;
        const string serviceName = "test-service";

        // Act
        var act = () => services.AddThisCloudFrameworkLoggings(configuration, serviceName);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddThisCloudFrameworkLoggings_WithNullOrWhitespaceServiceName_ThrowsArgumentException(string? serviceName)
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddThisCloudFrameworkLoggings(configuration, serviceName!);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("serviceName");
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_RegistersLogRedactorWithAdditionalPatterns()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Redaction:AdditionalPatterns:0"] = "custom-secret-\\d+"
            })
            .Build();
        const string serviceName = "test-service";

        // Act
        services.AddThisCloudFrameworkLoggings(configuration, serviceName);
        var provider = services.BuildServiceProvider();
        var redactor = provider.GetRequiredService<ILogRedactor>();

        // Assert
        redactor.Should().BeOfType<DefaultLogRedactor>();
        var result = redactor.Redact("Message with custom-secret-12345");
        result.Should().Contain("[REDACTED_PII]"); // AdditionalPatterns use [REDACTED_PII]
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_RegistersLoggingLevelSwitchWithCorrectMinimumLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Warning"
            })
            .Build();
        const string serviceName = "test-service";

        // Act
        services.AddThisCloudFrameworkLoggings(configuration, serviceName);
        var provider = services.BuildServiceProvider();
        var levelSwitch = provider.GetRequiredService<LoggingLevelSwitch>();

        // Assert
        levelSwitch.MinimumLevel.Should().Be(global::Serilog.Events.LogEventLevel.Warning);
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_RegistersControlServiceWithSharedLevelSwitch()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:MinimumLevel"] = "Debug"
            })
            .Build();
        const string serviceName = "test-service";

        // Act
        services.AddThisCloudFrameworkLoggings(configuration, serviceName);
        var provider = services.BuildServiceProvider();
        var controlService = provider.GetRequiredService<ILoggingControlService>();
        var levelSwitch = provider.GetRequiredService<LoggingLevelSwitch>();

        // Assert
        controlService.Should().BeOfType<SerilogLoggingControlService>();
        levelSwitch.MinimumLevel.Should().Be(global::Serilog.Events.LogEventLevel.Debug);
    }

    [Fact]
    public void AddThisCloudFrameworkLoggings_AllowsMultipleCalls_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        const string serviceName = "test-service";

        // Act
        services.AddThisCloudFrameworkLoggings(configuration, serviceName);
        services.AddThisCloudFrameworkLoggings(configuration, serviceName); // Second call
        var provider = services.BuildServiceProvider();

        // Assert - Should not throw, TryAdd prevents duplicates
        var options = provider.GetServices<ThisCloudSerilogOptions>().ToList();
        options.Should().HaveCount(1); // Only one registration due to TryAddSingleton
    }
}
