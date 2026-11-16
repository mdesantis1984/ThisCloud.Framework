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
/// Verifies that every Admin endpoint maps backend failures (store / control service throwing)
/// to a 500 ProblemDetails response instead of letting the exception escape.
/// </summary>
public sealed class AdminEndpointsBackendFailureTests
{
    public static TheoryData<string, string> Endpoints => new()
    {
        { "GET", "/api/admin/logging/settings" },
        { "PUT", "/api/admin/logging/settings" },
        { "PATCH", "/api/admin/logging/settings" },
        { "POST", "/api/admin/logging/enable" },
        { "POST", "/api/admin/logging/disable" },
        { "DELETE", "/api/admin/logging/settings" }
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_WhenBackendThrows_ReturnsInternalServerErrorProblemDetails(string method, string path)
    {
        // Arrange
        using var host = await CreateFailingHost();
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "PUT")
        {
            request.Content = JsonContent.Create(new UpdateLogSettingsRequest { MinimumLevel = LogLevel.Debug });
        }
        else if (method is "PATCH")
        {
            request.Content = JsonContent.Create(new PatchLogSettingsRequest { MinimumLevel = LogLevel.Debug });
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("title").GetString()));
    }

    private static async Task<IHost> CreateFailingHost()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false",
                ["ThisCloud:Loggings:MinimumLevel"] = "Information"
            })
            .Build();

        var builder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseEnvironment("Development")
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(configuration);
                        services.AddThisCloudFrameworkLoggings(configuration, "test-service");

                        // Replace the registered backends with ones that always fail
                        services.AddSingleton<ILoggingSettingsStore, FailingLoggingSettingsStore>();
                        services.AddSingleton<ILoggingControlService, FailingLoggingControlService>();

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

        return await builder.StartAsync();
    }

    private sealed class FailingLoggingSettingsStore : ILoggingSettingsStore
    {
        public Task<LogSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("store failure");

        public Task SaveSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("store failure");

        public Task<byte[]?> GetVersionAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("store failure");
    }

    private sealed class FailingLoggingControlService : ILoggingControlService
    {
        public Task EnableAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("control failure");

        public Task DisableAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("control failure");

        public Task SetSettingsAsync(LogSettings settings, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("control failure");

        public Task PatchSettingsAsync(LogSettings partialSettings, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("control failure");

        public Task ResetSettingsAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("control failure");
    }
}
