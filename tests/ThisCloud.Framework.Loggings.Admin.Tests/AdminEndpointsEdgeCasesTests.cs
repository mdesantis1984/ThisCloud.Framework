// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
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
/// Additional tests for edge cases and coverage completion.
/// </summary>
public sealed class AdminEndpointsEdgeCasesTests
{
    [Fact]
    public async Task PutSettings_WithEmptyFilePath_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var updateRequest = new UpdateLogSettingsRequest
        {
            File = new FileSinkDto
            {
                Enabled = true,
                Path = "",  // Empty path (invalid)
                RollingFileSizeMb = 10,
                RetainedFileCountLimit = 10,
                UseCompactJson = true
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/admin/logging/settings", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutSettings_WithInvalidRetentionDays_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var updateRequest = new UpdateLogSettingsRequest
        {
            Retention = new RetentionDto
            {
                Days = 0  // Invalid (<1)
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/admin/logging/settings", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchSettings_WithEmptyFilePath_ShouldReturnBadRequest()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            File = new FileSinkPatchDto
            {
                Path = "   "  // Whitespace path (invalid)
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutSettings_WithMaxValidValues_ShouldSucceed()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var updateRequest = new UpdateLogSettingsRequest
        {
            MinimumLevel = LogLevel.Critical,
            File = new FileSinkDto
            {
                Enabled = true,
                Path = "/test/log-.ndjson",
                RollingFileSizeMb = 100,  // Max valid
                RetainedFileCountLimit = 365,  // Max valid
                UseCompactJson = true
            },
            Retention = new RetentionDto
            {
                Days = 3650  // Max valid
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/admin/logging/settings", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchSettings_WithMinValidValues_ShouldSucceed()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            File = new FileSinkPatchDto
            {
                RollingFileSizeMb = 1,  // Min valid
                RetainedFileCountLimit = 1  // Min valid
            },
            Retention = new RetentionPatchDto
            {
                Days = 1  // Min valid
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatchSettings_ChangeEnabledFlags_ShouldSucceed()
    {
        // Arrange
        var config = CreateTestConfig();
        using var host = await CreateTestHost(config);
        using var client = host.GetTestClient();

        var patchRequest = new PatchLogSettingsRequest
        {
            Console = new ConsoleSinkPatchDto
            {
                Enabled = true
            },
            File = new FileSinkPatchDto
            {
                Enabled = false
            },
            Redaction = new RedactionPatchDto
            {
                Enabled = false
            },
            Correlation = new CorrelationPatchDto
            {
                GenerateIfMissing = false
            }
        };

        // Act
        var response = await client.PatchAsync(
            "/api/admin/logging/settings",
            JsonContent.Create(patchRequest));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
                        var initialSettings = AdminEndpointsTests.BuildLogSettingsFromConfiguration(configuration);
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
}
