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
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Admin.DTOs;
using ThisCloud.Framework.Loggings.Serilog;
using Xunit;
using LogLevel = ThisCloud.Framework.Loggings.Abstractions.LogLevel;

namespace ThisCloud.Framework.Loggings.Admin.Tests;

/// <summary>
/// Hosts built with <c>UseThisCloudFrameworkSerilog</c> publish their logger through the static <c>Log.Logger</c>,
/// so tests that read what a host wrote must not run in parallel with each other.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SerilogHostCollection
{
    public const string Name = "SerilogHost";
}

/// <summary>
/// End-to-end tests of what the Serilog pipeline actually writes after the Admin endpoints change the log
/// settings at runtime: per-namespace <c>Overrides</c> (S4) and <c>/enable</c> after <c>/disable</c> (S5).
/// Each scenario runs against a real host with a file sink; assertions read the events that reached the file.
/// </summary>
[Collection(SerilogHostCollection.Name)]
public sealed class AdminRuntimeLevelTests
{
    private const string BasePath = "/api/admin/logging";
    private const string SettingsPath = BasePath + "/settings";

    // ---------- S4: runtime overrides filter events ----------

    [Fact]
    public async Task PutOverrides_RaisingANamespace_SilencesLowerEventsOfThatNamespaceOnly()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest
            {
                MinimumLevel = LogLevel.Information,
                Overrides = new() { ["MyApp.Noisy"] = LogLevel.Warning }
            });

            loggers.Create("MyApp.Noisy").LogInformation("noisy-info-marker");
            loggers.Create("MyApp.Noisy").LogWarning("noisy-warning-marker");
            loggers.Create("MyApp.Other").LogInformation("other-info-marker");
        });

        Assert.DoesNotContain("noisy-info-marker", logs);
        Assert.Contains("noisy-warning-marker", logs);
        Assert.Contains("other-info-marker", logs);
    }

    [Fact]
    public async Task PutOverrides_LoweringANamespace_LetsLowerEventsThroughForThatNamespaceOnly()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest
            {
                MinimumLevel = LogLevel.Information,
                Overrides = new() { ["MyApp.Debuggy"] = LogLevel.Debug }
            });

            loggers.Create("MyApp.Debuggy").LogDebug("debuggy-debug-marker");
            loggers.Create("MyApp.Debuggy.Child").LogDebug("child-debug-marker");
            loggers.Create("MyApp.DebuggyOther").LogDebug("prefix-lookalike-debug-marker");
            loggers.Create("MyApp.Other").LogDebug("other-debug-marker");
        });

        Assert.Contains("debuggy-debug-marker", logs);
        Assert.Contains("child-debug-marker", logs);
        Assert.DoesNotContain("prefix-lookalike-debug-marker", logs);
        Assert.DoesNotContain("other-debug-marker", logs);
    }

    [Fact]
    public async Task PutOverrides_MostSpecificNamespaceWins()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest
            {
                Overrides = new()
                {
                    ["MyApp"] = LogLevel.Error,
                    ["MyApp.Database"] = LogLevel.Debug
                }
            });

            loggers.Create("MyApp.Database.Queries").LogDebug("specific-debug-marker");
            loggers.Create("MyApp.Web").LogWarning("broad-warning-marker");
            loggers.Create("MyApp.Web").LogError("broad-error-marker");
        });

        Assert.Contains("specific-debug-marker", logs);
        Assert.DoesNotContain("broad-warning-marker", logs);
        Assert.Contains("broad-error-marker", logs);
    }

    [Fact]
    public async Task PatchOverrides_AreMergedAndBothTakeEffect()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PatchAsync(client, new PatchLogSettingsRequest { Overrides = new() { ["MyApp.A"] = LogLevel.Error } });
            await PatchAsync(client, new PatchLogSettingsRequest { Overrides = new() { ["MyApp.B"] = LogLevel.Debug } });

            loggers.Create("MyApp.A").LogWarning("a-warning-marker");
            loggers.Create("MyApp.B").LogDebug("b-debug-marker");
            loggers.Create("MyApp.C").LogInformation("c-info-marker");
        });

        Assert.DoesNotContain("a-warning-marker", logs);
        Assert.Contains("b-debug-marker", logs);
        Assert.Contains("c-info-marker", logs);
    }

    [Fact]
    public async Task PutOverrides_ReplacingOrClearingAnOverride_StopsFilteringWithIt()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Noisy"] = LogLevel.Error } });
            loggers.Create("MyApp.Noisy").LogInformation("while-error-marker");

            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Noisy"] = LogLevel.Information } });
            loggers.Create("MyApp.Noisy").LogInformation("after-replace-marker");

            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Noisy"] = LogLevel.Error } });
            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() });
            loggers.Create("MyApp.Noisy").LogInformation("after-clear-marker");
        });

        Assert.DoesNotContain("while-error-marker", logs);
        Assert.Contains("after-replace-marker", logs);
        Assert.Contains("after-clear-marker", logs);
    }

    [Fact]
    public async Task DeleteSettings_ClearsRuntimeOverrides()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Noisy"] = LogLevel.Error } });
            using var reset = await client.DeleteAsync(SettingsPath, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

            loggers.Create("MyApp.Noisy").LogInformation("after-reset-marker");
        });

        Assert.Contains("after-reset-marker", logs);
    }

    [Fact]
    public async Task DeleteSettings_ReturnsToTheConfiguredStartupOverrides_AndClearsRuntimeOnes()
    {
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                await PutAsync(client, new UpdateLogSettingsRequest
                {
                    Overrides = new() { ["MyApp.Quiet"] = LogLevel.Information, ["MyApp.Runtime"] = LogLevel.Error }
                });
                using var reset = await client.DeleteAsync(SettingsPath, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

                loggers.Create("MyApp.Quiet").LogWarning("quiet-after-delete-marker");
                loggers.Create("MyApp.Quiet").LogError("quiet-error-after-delete-marker");
                loggers.Create("MyApp.Runtime").LogWarning("runtime-after-delete-marker");
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        Assert.DoesNotContain("quiet-after-delete-marker", logs);
        Assert.Contains("quiet-error-after-delete-marker", logs);
        Assert.Contains("runtime-after-delete-marker", logs);
    }

    [Fact]
    public async Task StartupOverridesFromConfiguration_StillFilterEvents()
    {
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                loggers.Create("MyApp.Quiet").LogWarning("quiet-warning-marker");
                loggers.Create("MyApp.Quiet").LogError("quiet-error-marker");
                loggers.Create("MyApp.Loud").LogInformation("loud-info-marker");
                await Task.CompletedTask;
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        Assert.DoesNotContain("quiet-warning-marker", logs);
        Assert.Contains("quiet-error-marker", logs);
        Assert.Contains("loud-info-marker", logs);
    }

    [Fact]
    public async Task StartupOverridesFromConfiguration_CanBeReplacedAtRuntime()
    {
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                loggers.Create("MyApp.Quiet").LogWarning("before-marker");
                await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Quiet"] = LogLevel.Information } });
                loggers.Create("MyApp.Quiet").LogWarning("after-marker");
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        Assert.DoesNotContain("before-marker", logs);
        Assert.Contains("after-marker", logs);
    }

    [Fact]
    public async Task StartupOverridesFromConfiguration_SurviveAPutThatDoesNotMentionOverrides()
    {
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                await PutAsync(client, new UpdateLogSettingsRequest { MinimumLevel = LogLevel.Information });
                loggers.Create("MyApp.Quiet").LogWarning("quiet-warning-marker");
                loggers.Create("MyApp.Loud").LogInformation("loud-info-marker");
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        Assert.DoesNotContain("quiet-warning-marker", logs);
        Assert.Contains("loud-info-marker", logs);
    }

    [Fact]
    public async Task PutOverrides_ResponseStillEchoesOverrides()
    {
        await RunScenario(async (client, _) =>
        {
            using var response = await client.PutAsJsonAsync(
                SettingsPath,
                new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Noisy"] = LogLevel.Warning } },
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<LogSettingsDto>(TestContext.Current.CancellationToken);
            Assert.Equal(LogLevel.Warning, dto!.Overrides["MyApp.Noisy"]);
        });
    }

    // ---------- S4: audit precedence over runtime overrides ----------

    [Theory]
    [InlineData("ThisCloud")]
    [InlineData("ThisCloud.Framework.Loggings.Serilog")]
    [InlineData("ThisCloud.Framework.Loggings.Serilog.SerilogAuditLogger")]
    public async Task RuntimeCriticalOverrides_NeverSilenceAuditEvents(string overrideKey)
    {
        var logs = await RunScenario(async (client, _) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { [overrideKey] = LogLevel.Critical } });
            using var disable = await client.PostAsync(BasePath + "/disable", content: null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        });

        Assert.Contains("\"AuditAction\":\"PutSettings\"", logs);
        Assert.Contains("\"AuditAction\":\"DisableLogging\"", logs);
    }

    // ---------- S5: /enable restores the level active before /disable ----------

    [Theory]
    [InlineData("Warning")]
    [InlineData("Debug")]
    [InlineData("Error")]
    public async Task EnableAfterDisable_RestoresTheMinimumLevelActiveBeforeDisable(string configuredLevel)
    {
        var level = Enum.Parse<LogLevel>(configuredLevel);
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                using var disable = await client.PostAsync(BasePath + "/disable", content: null, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
                loggers.Create("MyApp.Any").LogError("while-disabled-marker");

                using var enable = await client.PostAsync(BasePath + "/enable", content: null, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, enable.StatusCode);

                loggers.Create("MyApp.Any").LogDebug("restored-debug-marker");
                loggers.Create("MyApp.Any").LogInformation("restored-info-marker");
                loggers.Create("MyApp.Any").LogWarning("restored-warning-marker");
                loggers.Create("MyApp.Any").LogError("restored-error-marker");
            },
            ("ThisCloud:Loggings:MinimumLevel", configuredLevel));

        Assert.DoesNotContain("while-disabled-marker", logs);
        Assert.Equal(level <= LogLevel.Debug, logs.Contains("restored-debug-marker"));
        Assert.Equal(level <= LogLevel.Information, logs.Contains("restored-info-marker"));
        Assert.Equal(level <= LogLevel.Warning, logs.Contains("restored-warning-marker"));
        Assert.Contains("restored-error-marker", logs);
    }

    [Fact]
    public async Task EnableAfterDisable_RestoresTheLevelSetByAPreviousPut()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest { MinimumLevel = LogLevel.Debug });
            using var disable = await client.PostAsync(BasePath + "/disable", content: null, TestContext.Current.CancellationToken);
            using var enable = await client.PostAsync(BasePath + "/enable", content: null, TestContext.Current.CancellationToken);

            loggers.Create("MyApp.Any").LogDebug("debug-after-enable-marker");
        });

        Assert.Contains("debug-after-enable-marker", logs);
    }

    [Fact]
    public async Task WhileDisabled_OverridesDoNotReopenLogging_AndEnableBringsThemBack()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            await PutAsync(client, new UpdateLogSettingsRequest { Overrides = new() { ["MyApp.Debuggy"] = LogLevel.Debug } });
            using var disable = await client.PostAsync(BasePath + "/disable", content: null, TestContext.Current.CancellationToken);
            loggers.Create("MyApp.Debuggy").LogDebug("disabled-debug-marker");

            using var enable = await client.PostAsync(BasePath + "/enable", content: null, TestContext.Current.CancellationToken);
            loggers.Create("MyApp.Debuggy").LogDebug("enabled-debug-marker");
        });

        Assert.DoesNotContain("disabled-debug-marker", logs);
        Assert.Contains("enabled-debug-marker", logs);
    }

    [Fact]
    public async Task EnableWithoutPriorDisable_LeavesTheMinimumLevelUntouched()
    {
        var logs = await RunScenario(
            async (client, loggers) =>
            {
                using var enable = await client.PostAsync(BasePath + "/enable", content: null, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, enable.StatusCode);

                loggers.Create("MyApp.Any").LogInformation("info-marker");
                loggers.Create("MyApp.Any").LogWarning("warning-marker");
            },
            ("ThisCloud:Loggings:MinimumLevel", "Warning"));

        Assert.DoesNotContain("info-marker", logs);
        Assert.Contains("warning-marker", logs);
    }

    [Fact]
    public async Task Disable_StillSilencesNonAuditEvents()
    {
        var logs = await RunScenario(async (client, loggers) =>
        {
            using var disable = await client.PostAsync(BasePath + "/disable", content: null, TestContext.Current.CancellationToken);
            loggers.Create("MyApp.Any").LogError("error-while-disabled-marker");
        });

        Assert.DoesNotContain("error-while-disabled-marker", logs);
        Assert.Contains("\"AuditAction\":\"DisableLogging\"", logs);
    }

    // ---------- helpers ----------

    private static async Task PutAsync(HttpClient client, UpdateLogSettingsRequest request)
    {
        using var response = await client.PutAsJsonAsync(SettingsPath, request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task PatchAsync(HttpClient client, PatchLogSettingsRequest request)
    {
        using var response = await client.PatchAsJsonAsync(SettingsPath, request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class Loggers(ILoggerFactory factory)
    {
        public ILogger Create(string category) => factory.CreateLogger(category);
    }

    /// <summary>
    /// Runs <paramref name="scenario"/> against a started host (Admin endpoints + Serilog file sink, Information by
    /// default), disposes the host to flush the sink and returns everything written to the log files.
    /// </summary>
    private static async Task<string> RunScenario(
        Func<HttpClient, Loggers, Task> scenario,
        params (string Key, string Value)[] extraConfig)
    {
        var dir = Directory.CreateTempSubdirectory("tc-runtime-level-").FullName;
        try
        {
            var data = new System.Collections.Generic.Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false",
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = Path.Combine(dir, "runtime-.ndjson"),
                ["ThisCloud:Loggings:File:RollingFileSizeMb"] = "10"
            };
            foreach (var (key, value) in extraConfig)
            {
                data[key] = value;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = await CreateHost(configuration))
            using (var client = host.GetTestClient())
            {
                await scenario(client, new Loggers(host.Services.GetRequiredService<ILoggerFactory>()));
            }

            return string.Concat(Directory.GetFiles(dir).Select(f =>
            {
                using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static async Task<IHost> CreateHost(IConfiguration configuration)
    {
        var builder = new HostBuilder();
        builder.UseThisCloudFrameworkSerilog(configuration, "test-service");
        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseEnvironment("Development")
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(configuration);
                    services.AddThisCloudFrameworkLoggings(configuration, "test-service");

                    var initialSettings = AdminEndpointsTests.BuildLogSettingsFromConfiguration(configuration);
                    services.AddSingleton<ILoggingSettingsStore>(_ =>
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
                    app.UseEndpoints(endpoints => endpoints.MapThisCloudFrameworkLoggingsAdmin(configuration));
                });
        });

        return await builder.StartAsync();
    }
}
