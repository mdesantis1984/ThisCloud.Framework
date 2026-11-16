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
/// Security hardening tests for the Admin endpoints: fail-closed gating (H1), runtime settings
/// validation (H2) and audit events that survive disabling logging (M3).
/// </summary>
public sealed class AdminSecurityHardeningTests
{
    private const string SettingsPath = "/api/admin/logging/settings";

    // ---------- H1: fail-closed gating ----------

    [Fact]
    public async Task Map_AdminEnabledOutsideDevelopmentWithoutRequireAdmin_ThrowsAtStartup()
    {
        var config = AdminConfig(("AllowedEnvironments:0", "Staging"), ("RequireAdmin", "false"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateHost(config, "Staging"));

        Assert.Contains("RequireAdmin", ex.Message);
        Assert.Contains("Staging", ex.Message);
    }

    [Fact]
    public async Task Map_AdminEnabledOutsideDevelopmentWithRequireAdminOmitted_ThrowsAtStartup()
    {
        var config = AdminConfig(("AllowedEnvironments:0", "Production"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateHost(config, "Production"));
    }

    [Fact]
    public async Task Map_AdminEnabledOutsideDevelopmentWithRequireAdmin_MapsEndpoints()
    {
        var config = AdminConfig(("AllowedEnvironments:0", "Staging"), ("RequireAdmin", "true"));

        using var host = await CreateHost(config, "Staging");
        using var client = host.GetTestClient();

        var response = await client.GetAsync(SettingsPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Map_EmptyAllowedEnvironmentsInDevelopmentWithoutRequireAdmin_MapsEndpoints()
    {
        var config = AdminConfig(("RequireAdmin", "false"));

        using var host = await CreateHost(config, "Development");
        using var client = host.GetTestClient();

        var response = await client.GetAsync(SettingsPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Map_EmptyAllowedEnvironmentsOutsideDevelopment_DoesNotMapEndpoints(string environment)
    {
        var config = AdminConfig(("RequireAdmin", "true"));

        using var host = await CreateHost(config, environment);
        using var client = host.GetTestClient();

        var response = await client.GetAsync(SettingsPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- H2: runtime settings validation ----------

    public static TheoryData<string> InvalidFilePaths => new()
    {
        "../../etc/cron.d/evil",
        "logs/../../evil.log",
        "logs/..",
        "..\\evil.log",
        "/etc/passwd",
        "\\\\server\\share\\log.txt"
    };

    [Theory]
    [MemberData(nameof(InvalidFilePaths))]
    public async Task PutSettings_WithUnsafeFilePath_ReturnsValidationProblemAndKeepsSettings(string path)
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();
        var before = await ReadSettings(client);

        var request = new UpdateLogSettingsRequest
        {
            File = new FileSinkDto { Enabled = true, Path = path, RollingFileSizeMb = 10, RetainedFileCountLimit = 5, UseCompactJson = true }
        };
        using var response = await client.PutAsJsonAsync(SettingsPath, request, TestContext.Current.CancellationToken);

        await AssertValidationProblem(response, "File.Path");
        Assert.Equal(before.File.Path, (await ReadSettings(client)).File.Path);
    }

    [Theory]
    [MemberData(nameof(InvalidFilePaths))]
    public async Task PatchSettings_WithUnsafeFilePath_ReturnsValidationProblemAndKeepsSettings(string path)
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();
        var before = await ReadSettings(client);

        using var response = await client.PatchAsJsonAsync(
            SettingsPath, new PatchLogSettingsRequest { File = new FileSinkPatchDto { Path = path } }, TestContext.Current.CancellationToken);

        await AssertValidationProblem(response, "File.Path");
        Assert.Equal(before.File.Path, (await ReadSettings(client)).File.Path);
    }

    [Fact]
    public async Task PatchSettings_WithRelativeFilePath_AppliesIt()
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();

        using var response = await client.PatchAsJsonAsync(
            SettingsPath, new PatchLogSettingsRequest { File = new FileSinkPatchDto { Path = "logs/app/custom-.ndjson" } }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("logs/app/custom-.ndjson", (await ReadSettings(client)).File.Path);
    }

    public static TheoryData<string[]> InvalidPatterns => new()
    {
        new[] { "([unclosed" },
        new[] { "valid\\d+", "(?<broken" },
        new[] { new string('a', 513) },
        new[] { "   " },
        Enumerable.Range(0, 21).Select(i => $"token{i}").ToArray()
    };

    [Theory]
    [MemberData(nameof(InvalidPatterns))]
    public async Task PutSettings_WithInvalidRedactionPatterns_ReturnsValidationProblemAndKeepsSettings(string[] patterns)
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();

        var request = new UpdateLogSettingsRequest { Redaction = new RedactionDto { Enabled = true, AdditionalPatterns = patterns } };
        using var response = await client.PutAsJsonAsync(SettingsPath, request, TestContext.Current.CancellationToken);

        await AssertValidationProblem(response, "Redaction.AdditionalPatterns");
        Assert.Empty((await ReadSettings(client)).Redaction.AdditionalPatterns);
    }

    [Theory]
    [MemberData(nameof(InvalidPatterns))]
    public async Task PatchSettings_WithInvalidRedactionPatterns_ReturnsValidationProblemAndKeepsSettings(string[] patterns)
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();

        using var response = await client.PatchAsJsonAsync(
            SettingsPath, new PatchLogSettingsRequest { Redaction = new RedactionPatchDto { AdditionalPatterns = patterns } }, TestContext.Current.CancellationToken);

        await AssertValidationProblem(response, "Redaction.AdditionalPatterns");
        Assert.Empty((await ReadSettings(client)).Redaction.AdditionalPatterns);
    }

    [Fact]
    public async Task PatchSettings_WithBoundaryRedactionPatterns_AppliesThem()
    {
        using var host = await CreateHost(DefaultConfig(), "Development");
        using var client = host.GetTestClient();
        var patterns = Enumerable.Range(0, 20).Select(i => $"token{i}\\d+").ToArray();
        patterns[0] = new string('a', 512);

        using var response = await client.PatchAsJsonAsync(
            SettingsPath, new PatchLogSettingsRequest { Redaction = new RedactionPatchDto { AdditionalPatterns = patterns } }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(patterns, (await ReadSettings(client)).Redaction.AdditionalPatterns);
    }

    // ---------- M3: audit events survive disabling logging ----------

    [Fact]
    public async Task DisableLogging_AuditEventIsStillWritten()
    {
        var dir = Directory.CreateTempSubdirectory("tc-audit-").FullName;
        try
        {
            using (var host = await CreateAuditHost(dir))
            using (var client = host.GetTestClient())
            {
                using var response = await client.PostAsync("/api/admin/logging/disable", content: null, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            Assert.Contains("\"AuditAction\":\"DisableLogging\"", ReadAllLogs(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task PutSettingsWithCriticalLevel_AuditEventIsStillWritten()
    {
        var dir = Directory.CreateTempSubdirectory("tc-audit-").FullName;
        try
        {
            using (var host = await CreateAuditHost(dir))
            using (var client = host.GetTestClient())
            {
                using var response = await client.PutAsJsonAsync(
                    SettingsPath, new UpdateLogSettingsRequest { MinimumLevel = LogLevel.Critical }, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            Assert.Contains("\"AuditAction\":\"PutSettings\"", ReadAllLogs(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ---------- helpers ----------

    private static string ReadAllLogs(string dir) =>
        string.Concat(Directory.GetFiles(dir).Select(f =>
        {
            using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }));

    private static async Task AssertValidationProblem(HttpResponseMessage response, string expectedField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(expectedField, json.RootElement.GetProperty("detail").GetString());
    }

    private static async Task<LogSettingsDto> ReadSettings(HttpClient client)
    {
        var dto = await client.GetFromJsonAsync<LogSettingsDto>(SettingsPath, TestContext.Current.CancellationToken);
        return dto!;
    }

    private static IConfiguration AdminConfig(params (string Key, string Value)[] values)
    {
        var data = new Dictionary<string, string?> { ["ThisCloud:Loggings:Admin:Enabled"] = "true" };
        foreach (var (key, value) in values)
        {
            data[$"ThisCloud:Loggings:Admin:{key}"] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static IConfiguration DefaultConfig() => AdminConfig(
        ("AllowedEnvironments:0", "Development"),
        ("RequireAdmin", "false"));

    private static Task<IHost> CreateHost(IConfiguration configuration, string environmentName) =>
        CreateHostCore(configuration, environmentName, useSerilogHost: false);

    private static Task<IHost> CreateAuditHost(string logDirectory)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Loggings:Admin:Enabled"] = "true",
                ["ThisCloud:Loggings:Admin:AllowedEnvironments:0"] = "Development",
                ["ThisCloud:Loggings:Admin:RequireAdmin"] = "false",
                ["ThisCloud:Loggings:MinimumLevel"] = "Information",
                ["ThisCloud:Loggings:Console:Enabled"] = "false",
                ["ThisCloud:Loggings:File:Enabled"] = "true",
                ["ThisCloud:Loggings:File:Path"] = Path.Combine(logDirectory, "audit-.ndjson"),
                ["ThisCloud:Loggings:File:RollingFileSizeMb"] = "10"
            })
            .Build();

        return CreateHostCore(configuration, "Development", useSerilogHost: true);
    }

    private static async Task<IHost> CreateHostCore(IConfiguration configuration, string environmentName, bool useSerilogHost)
    {
        var builder = new HostBuilder();
        if (useSerilogHost)
        {
            builder.UseThisCloudFrameworkSerilog(configuration, "test-service");
        }

        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseEnvironment(environmentName)
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
