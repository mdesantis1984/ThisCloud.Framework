using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Abstractions;
using Xunit;
using LogLevel = ThisCloud.Framework.Loggings.Abstractions.LogLevel;

namespace ThisCloud.Framework.Loggings.Serilog.Tests;

/// <summary>
/// Drives <see cref="ILoggingControlService"/> on a real host and asserts which events reach the file sink
/// (per-namespace <c>Overrides</c> applied at runtime, S4).
/// </summary>
[Collection(SerilogHostCollection.Name)]
public sealed class RuntimeLevelPipelineTests
{
    [Fact]
    public async Task SetSettingsAsync_WithOverrides_FiltersPerNamespace()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings
            {
                MinimumLevel = LogLevel.Information,
                Overrides = new Dictionary<string, LogLevel>
                {
                    ["MyApp.Noisy"] = LogLevel.Error,
                    ["MyApp.Debuggy"] = LogLevel.Debug
                }
            }, TestContext.Current.CancellationToken);

            loggers.CreateLogger("MyApp.Noisy").LogWarning("noisy-warning-marker");
            loggers.CreateLogger("MyApp.Noisy").LogError("noisy-error-marker");
            loggers.CreateLogger("MyApp.Debuggy").LogDebug("debuggy-debug-marker");
            loggers.CreateLogger("MyApp.Other").LogDebug("other-debug-marker");
            loggers.CreateLogger("MyApp.Other").LogInformation("other-info-marker");
        });

        logs.Should().NotContain("noisy-warning-marker");
        logs.Should().Contain("noisy-error-marker");
        logs.Should().Contain("debuggy-debug-marker");
        logs.Should().NotContain("other-debug-marker");
        logs.Should().Contain("other-info-marker");
    }

    [Fact]
    public async Task PatchSettingsAsync_WithOverrides_AppliesThem()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.PatchSettingsAsync(new LogSettings
            {
                MinimumLevel = LogLevel.Information,
                Overrides = new Dictionary<string, LogLevel> { ["MyApp.Noisy"] = LogLevel.Error }
            }, TestContext.Current.CancellationToken);

            loggers.CreateLogger("MyApp.Noisy").LogWarning("noisy-warning-marker");
            loggers.CreateLogger("MyApp.Other").LogInformation("other-info-marker");
        });

        logs.Should().NotContain("noisy-warning-marker");
        logs.Should().Contain("other-info-marker");
    }

    [Fact]
    public async Task PatchSettingsAsync_WithoutOverrides_KeepsTheCurrentOnes()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings
            {
                Overrides = new Dictionary<string, LogLevel> { ["MyApp.Noisy"] = LogLevel.Error }
            }, TestContext.Current.CancellationToken);
            await control.PatchSettingsAsync(new LogSettings { MinimumLevel = LogLevel.Information }, TestContext.Current.CancellationToken);

            loggers.CreateLogger("MyApp.Noisy").LogWarning("noisy-warning-marker");
        });

        logs.Should().NotContain("noisy-warning-marker");
    }

    [Fact]
    public async Task SetSettingsAsync_WithoutOverrides_KeepsTheActiveOnes_AndEmptyClearsThem()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings
            {
                Overrides = new Dictionary<string, LogLevel> { ["MyApp.Noisy"] = LogLevel.Error }
            }, TestContext.Current.CancellationToken);

            await control.SetSettingsAsync(new LogSettings { MinimumLevel = LogLevel.Information }, TestContext.Current.CancellationToken);
            loggers.CreateLogger("MyApp.Noisy").LogWarning("kept-warning-marker");

            await control.SetSettingsAsync(new LogSettings { Overrides = new Dictionary<string, LogLevel>() }, TestContext.Current.CancellationToken);
            loggers.CreateLogger("MyApp.Noisy").LogWarning("cleared-warning-marker");
        });

        logs.Should().NotContain("kept-warning-marker");
        logs.Should().Contain("cleared-warning-marker");
    }

    [Fact]
    public async Task ResetSettingsAsync_RemovesOverrides()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings
            {
                Overrides = new Dictionary<string, LogLevel> { ["MyApp.Noisy"] = LogLevel.Error }
            }, TestContext.Current.CancellationToken);
            await control.ResetSettingsAsync(TestContext.Current.CancellationToken);

            loggers.CreateLogger("MyApp.Noisy").LogInformation("after-reset-marker");
        });

        logs.Should().Contain("after-reset-marker");
    }

    [Fact]
    public async Task ResetSettingsAsync_ReturnsToTheStartupOverrides_AndClearsRuntimeOnes()
    {
        var logs = await RunScenario(
            async services =>
            {
                var control = services.GetRequiredService<ILoggingControlService>();
                var loggers = services.GetRequiredService<ILoggerFactory>();
                await control.SetSettingsAsync(new LogSettings
                {
                    Overrides = new Dictionary<string, LogLevel>
                    {
                        ["MyApp.Quiet"] = LogLevel.Information,
                        ["MyApp.Runtime"] = LogLevel.Error
                    }
                }, TestContext.Current.CancellationToken);
                loggers.CreateLogger("MyApp.Quiet").LogWarning("quiet-before-reset-marker");
                loggers.CreateLogger("MyApp.Runtime").LogWarning("runtime-before-reset-marker");

                await control.ResetSettingsAsync(TestContext.Current.CancellationToken);

                loggers.CreateLogger("MyApp.Quiet").LogWarning("quiet-after-reset-marker");
                loggers.CreateLogger("MyApp.Quiet").LogError("quiet-error-after-reset-marker");
                loggers.CreateLogger("MyApp.Runtime").LogWarning("runtime-after-reset-marker");
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        logs.Should().Contain("quiet-before-reset-marker");
        logs.Should().NotContain("runtime-before-reset-marker");
        logs.Should().NotContain("quiet-after-reset-marker");
        logs.Should().Contain("quiet-error-after-reset-marker");
        logs.Should().Contain("runtime-after-reset-marker");
    }

    [Fact]
    public async Task ResetSettingsAsync_AfterAPatchOfTheStartupOverrides_StillReturnsToTheConfiguredOnes()
    {
        var logs = await RunScenario(
            async services =>
            {
                var control = services.GetRequiredService<ILoggingControlService>();
                var loggers = services.GetRequiredService<ILoggerFactory>();
                await control.PatchSettingsAsync(new LogSettings
                {
                    Overrides = new Dictionary<string, LogLevel> { ["MyApp.Quiet"] = LogLevel.Debug }
                }, TestContext.Current.CancellationToken);

                await control.ResetSettingsAsync(TestContext.Current.CancellationToken);

                loggers.CreateLogger("MyApp.Quiet").LogWarning("quiet-after-patch-reset-marker");
            },
            ("ThisCloud:Loggings:Overrides:MyApp.Quiet", "Error"));

        logs.Should().NotContain("quiet-after-patch-reset-marker");
    }

    [Fact]
    public async Task StartupOverride_IsSuspendedWhileDisabled_AndBackAfterEnable()
    {
        var logs = await RunScenario(
            async services =>
            {
                var control = services.GetRequiredService<ILoggingControlService>();
                var loggers = services.GetRequiredService<ILoggerFactory>();
                loggers.CreateLogger("Dbg").LogDebug("dbg-debug-before-disable-marker");

                await control.DisableAsync(TestContext.Current.CancellationToken);
                loggers.CreateLogger("Dbg").LogDebug("dbg-debug-while-disabled-marker");

                await control.EnableAsync(TestContext.Current.CancellationToken);
                loggers.CreateLogger("Dbg").LogDebug("dbg-debug-after-enable-marker");
                loggers.CreateLogger("Other").LogDebug("other-debug-after-enable-marker");
            },
            ("ThisCloud:Loggings:Overrides:Dbg", "Debug"));

        logs.Should().Contain("dbg-debug-before-disable-marker");
        logs.Should().NotContain("dbg-debug-while-disabled-marker");
        logs.Should().Contain("dbg-debug-after-enable-marker");
        logs.Should().NotContain("other-debug-after-enable-marker");
    }

    [Fact]
    public async Task SetSettingsAsync_WithoutOverrides_KeepsTheGlobalMinimumForEveryNamespace()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings { MinimumLevel = LogLevel.Warning }, TestContext.Current.CancellationToken);

            loggers.CreateLogger("MyApp.Any").LogInformation("info-marker");
            loggers.CreateLogger("MyApp.Any").LogWarning("warning-marker");
        });

        logs.Should().NotContain("info-marker");
        logs.Should().Contain("warning-marker");
    }

    [Fact]
    public async Task ChangingTheGlobalSwitchDirectly_IsHonoured()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            var levelSwitch = services.GetRequiredService<global::Serilog.Core.LoggingLevelSwitch>();
            levelSwitch.MinimumLevel = global::Serilog.Events.LogEventLevel.Debug;

            loggers.CreateLogger("MyApp.Any").LogDebug("debug-marker");
            await Task.CompletedTask;
        });

        logs.Should().Contain("debug-marker");
    }

    [Fact]
    public async Task AuditEvents_SurviveRuntimeOverridesAndDisable()
    {
        var logs = await RunScenario(async services =>
        {
            var control = services.GetRequiredService<ILoggingControlService>();
            var loggers = services.GetRequiredService<ILoggerFactory>();
            await control.SetSettingsAsync(new LogSettings
            {
                Overrides = new Dictionary<string, LogLevel> { ["ThisCloud"] = LogLevel.Critical }
            }, TestContext.Current.CancellationToken);
            await control.DisableAsync(TestContext.Current.CancellationToken);

            var audit = services.GetRequiredService<IAuditLogger>();
            await audit.LogAuditEventAsync("RuntimeAudit", "tester", null, "details", TestContext.Current.CancellationToken);
        });

        logs.Should().Contain("\"AuditAction\":\"RuntimeAudit\"");
    }

    // ---------- helpers ----------

    private static async Task<string> RunScenario(
        Func<IServiceProvider, Task> scenario,
        params (string Key, string Value)[] extraConfig)
    {
        var dir = Directory.CreateTempSubdirectory("tc-pipeline-").FullName;
        try
        {
            var data = new Dictionary<string, string?>
                {
                    ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                    ["ThisCloud:Loggings:Console:Enabled"] = "false",
                    ["ThisCloud:Loggings:File:Enabled"] = "true",
                    ["ThisCloud:Loggings:File:Path"] = Path.Combine(dir, "pipeline-.ndjson"),
                    ["ThisCloud:Loggings:File:RollingFileSizeMb"] = "10"
                };
            foreach (var (key, value) in extraConfig)
            {
                data[key] = value;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();

            using (var host = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureServices(services => services.AddThisCloudFrameworkLoggings(configuration, "test-service"))
                .UseThisCloudFrameworkSerilog(configuration, "test-service")
                .Build())
            {
                await scenario(host.Services);
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
}

/// <summary>
/// Hosts built with <c>UseThisCloudFrameworkSerilog</c> publish their logger through the static <c>Log.Logger</c>,
/// so tests that read what a host wrote must not run in parallel with other tests that build such a host.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SerilogHostCollection
{
    public const string Name = "SerilogHost";
}
