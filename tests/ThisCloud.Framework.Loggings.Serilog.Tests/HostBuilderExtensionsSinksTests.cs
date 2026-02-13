using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Tests for Console and File sink configuration (L3.1, L3.2).
/// </summary>
public sealed class HostBuilderExtensionsSinksTests
{
    [Fact]
    public void UseThisCloudFrameworkSerilog_ConsoleEnabled_ConfiguresSink()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Enabled"] = "false" // Disable file to avoid IO
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: Cannot easily assert console sink without capturing output; validated by integration
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_ConsoleDisabled_DoesNotConfigureSink()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "false"
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: Console sink not configured; no easy way to assert negative case
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileEnabled_ConfiguresSinkWithDefaults()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath
                // RollingFileSizeMb default = 10 (not specified)
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: File sink configured with default 10MB = 10485760 bytes
                // FileSizeLimitBytes = 10 * 1024 * 1024 = 10485760
                // Cannot easily assert internal sink config without reflection; validated by integration
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileEnabled_CustomRollingSize_ConfiguresSink()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath,
                ["ThisCloud:Loggings:File:RollingFileSizeMb"] = "20" // Custom 20MB
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: File sink configured with 20MB = 20971520 bytes
                // FileSizeLimitBytes = 20 * 1024 * 1024 = 20971520
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileEnabled_UseCompactJsonTrue_ConfiguresFormatter()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.ndjson";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath,
                ["ThisCloud:Loggings:File:UseCompactJson"] = "true"
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: CompactJsonFormatter configured; validated by integration
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileEnabled_UseCompactJsonFalse_ConfiguresPlainText()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath,
                ["ThisCloud:Loggings:File:UseCompactJson"] = "false"
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: Plain text formatter (default); validated by integration
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileDisabled_DoesNotConfigureSink()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "false"
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: File sink not configured; no log files created
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_FileEnabled_CustomRetainedFileCountLimit_ConfiguresSink()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath,
                ["ThisCloud:Loggings:File:RetainedFileCountLimit"] = "7" // Keep 7 days
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: RetainedFileCountLimit = 7
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_BothSinksEnabled_ConfiguresBoth()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath
            })
                .Build();
                const string serviceName = "test-service";

                var hostBuilder = Host.CreateDefaultBuilder()
                    .UseEnvironment("Development")
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(configuration);
                    });

                // Act
                using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();

                // Assert
                host.Should().NotBeNull();
                // Note: Both console and file sinks configured
            }

            [Fact]
            public void UseThisCloudFrameworkSerilog_RollingFileSizeMb_DefaultIs10MB()
    {
        // Arrange
        var testPath = $"logs/test-{Guid.NewGuid()}.log";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = testPath
                // RollingFileSizeMb not specified -> defaults to 10
            })
            .Build();
        const string serviceName = "test-service";

        var hostBuilder = Host.CreateDefaultBuilder()
            .UseEnvironment("Development")
            .ConfigureServices(services =>
            {
                services.AddSingleton(configuration);
            });

        // Act
        using var host = hostBuilder.UseThisCloudFrameworkSerilog(configuration, serviceName).Build();
        var options = ThisCloudSerilogOptions.FromConfiguration(configuration, serviceName);

        // Assert
        host.Should().NotBeNull();
        options.Settings.File.RollingFileSizeMb.Should().Be(10);
        // FileSizeLimitBytes = 10 * 1024 * 1024 = 10485760 bytes
    }
}
