// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Admin.DTOs;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;

namespace ThisCloud.Framework.Loggings.Admin.Tests;

/// <summary>
/// Tests para endpoints de administración y gating.
/// </summary>
public sealed class AdminEndpointsTests
{
    [Fact]
    public async Task MapThisCloudFrameworkLoggingsAdmin_WhenAdminDisabled_ShouldNotMapEndpoints()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "false"
            })
            .Build();

        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MapThisCloudFrameworkLoggingsAdmin_WhenEnabledButEnvironmentNotAllowed_ShouldNotMapEndpoints()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        using var host = await CreateTestHost(config, "Production");
        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MapThisCloudFrameworkLoggingsAdmin_WhenEnabledAndEnvironmentAllowed_ShouldMapEndpoints()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false"
            })
            .Build();

        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSettings_ShouldReturnCurrentSettings()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false",
                ["ThisCloud:Loggings:MinimumLevel"] = "Warning"
            })
            .Build();

        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(LogLevel.Warning, dto.MinimumLevel);
    }

    [Fact]
    public async Task PutSettings_ShouldReplaceSettings()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var updateRequest = new UpdateLogSettingsRequest
        {
            MinimumLevel = LogLevel.Debug,
            File = new FileSinkDto
            {
                Enabled = true,
                Path = "/test/log-.ndjson",
                RollingFileSizeMb = 20,
                RetainedFileCountLimit = 10,
                UseCompactJson = true
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/admin/logging/settings", updateRequest, TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(LogLevel.Debug, dto.MinimumLevel);
        Assert.Equal(20, dto.File.RollingFileSizeMb);
    }

    [Fact]
    public async Task PutSettings_WithInvalidRollingFileSizeMb_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var updateRequest = new UpdateLogSettingsRequest
        {
            File = new FileSinkDto
            {
                Enabled = true,
                Path = "/test/log-.ndjson",
                RollingFileSizeMb = 200, // Invalid (>100)
                RetainedFileCountLimit = 10,
                UseCompactJson = true
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/admin/logging/settings", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchSettings_ShouldMergePartialSettings()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            MinimumLevel = LogLevel.Error
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest),
            TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(LogLevel.Error, dto.MinimumLevel);
        // Verificar que File settings NO cambiaron (merge parcial)
        Assert.Equal(10, dto.File.RollingFileSizeMb);
    }

    [Fact]
    public async Task PatchSettings_WithFileRollingFileSizeMb_ShouldUpdateOnlyThatField()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            File = new FileSinkPatchDto
            {
                RollingFileSizeMb = 50
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest),
            TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(50, dto.File.RollingFileSizeMb);
        // Verificar que otros campos de File NO cambiaron
        Assert.True(dto.File.Enabled);
        Assert.Equal("logs/log-.ndjson", dto.File.Path);
    }

    [Fact]
    public async Task PatchSettings_WithOverrides_ShouldMergeOverrides()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            Overrides = new Dictionary<string, LogLevel>
            {
                ["MyApp.Database"] = LogLevel.Debug,
                ["MyApp.Networking"] = LogLevel.Verbose
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest),
            TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.NotNull(dto.Overrides);
        Assert.Equal(LogLevel.Debug, dto.Overrides["MyApp.Database"]);
        Assert.Equal(LogLevel.Verbose, dto.Overrides["MyApp.Networking"]);
    }

    [Fact]
    public async Task PatchSettings_WithInvalidRollingFileSizeMb_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            File = new FileSinkPatchDto
            {
                RollingFileSizeMb = 0 // Invalid (<1)
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchSettings_WithInvalidRetentionDays_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            Retention = new RetentionPatchDto
            {
                Days = 5000 // Invalid (>3650)
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostEnable_ShouldEnableLogging()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        // Act
        var response = await client.PostAsync("/api/admin/logging/enable", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PostDisable_ShouldDisableLogging()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        // Act
        var response = await client.PostAsync("/api/admin/logging/disable", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSettings_ShouldResetToDefaults()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config, "Development");
        using var client = host.GetTestClient();

        // Modificar settings primero
        var patchRequest = new PatchLogSettingsRequest
        {
            MinimumLevel = LogLevel.Critical
        };
        await client.PatchAsync("/api/admin/logging/settings", JsonContent.Create(patchRequest), TestContext.Current.CancellationToken);

        // Act
        var response = await client.DeleteAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verificar que se reseteó
        var getResponse = await client.GetAsync("/api/admin/logging/settings", TestContext.Current.CancellationToken);
        var dto = await getResponse.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(LogLevel.Information, dto.MinimumLevel); // Default
    }

    // Helpers
    private static IConfiguration CreateTestConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false",
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = "logs/log-.ndjson",
                ["ThisCloud:Loggings:File:RollingFileSizeMb"] = "10"
            })
            .Build();
    }

    private static async Task<IHost> CreateTestHost(IConfiguration configuration, string environmentName = "Development")
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseEnvironment(environmentName)
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(configuration);
                        services.AddThisCloudFrameworkLoggings(configuration, "test-service");

                        // Register fake ILoggingSettingsStore initialized with config
                        var initialSettings = BuildLogSettingsFromConfiguration(configuration);
                        services.AddSingleton<ILoggingSettingsStore>(sp =>
                        {
                            var store = new InMemoryLoggingSettingsStore();
                            store.SaveSettingsAsync(initialSettings).GetAwaiter().GetResult();
                            return store;
                        });

                        services.AddRouting();
                        services.AddAuthorization(options =>
                        {
                            options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => true));
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapThisCloudFrameworkLoggingsAdmin(configuration);
                        });
                    });
            });

        var host = await builder.StartAsync();
        return host;
    }

    public static LogSettings BuildLogSettingsFromConfiguration(IConfiguration configuration)
    {
        var settings = new LogSettings();

        // Bind from configuration
        var loggingSection = configuration.GetSection("ThisCloud:Loggings");
        if (loggingSection.Exists())
        {
            var minLevelStr = loggingSection["MinimumLevel"];
            if (!string.IsNullOrEmpty(minLevelStr) && Enum.TryParse<LogLevel>(minLevelStr, out var minLevel))
            {
                settings.MinimumLevel = minLevel;
            }

            var fileSection = loggingSection.GetSection("File");
            if (fileSection.Exists())
            {
                var fileEnabled = fileSection["Enabled"];
                if (!string.IsNullOrEmpty(fileEnabled) && bool.TryParse(fileEnabled, out var enabled))
                {
                    settings.File.Enabled = enabled;
                }

                var filePath = fileSection["Path"];
                if (!string.IsNullOrEmpty(filePath))
                {
                    settings.File.Path = filePath;
                }

                var rollingSizeStr = fileSection["RollingFileSizeMb"];
                if (!string.IsNullOrEmpty(rollingSizeStr) && int.TryParse(rollingSizeStr, out var rollingSize))
                {
                    settings.File.RollingFileSizeMb = rollingSize;
                }
            }
        }

        return settings;
    }
}
