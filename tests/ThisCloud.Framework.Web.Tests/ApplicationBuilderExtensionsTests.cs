using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ThisCloud.Framework.Web.Extensions;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests end-to-end para ApplicationBuilderExtensions (cobertura de UseThisCloudFrameworkWeb y UseThisCloudFrameworkSwagger).
/// </summary>
public class ApplicationBuilderExtensionsTests
{
    /// <summary>
    /// UseThisCloudFrameworkWeb con CORS habilitado aplica policy CORS correctamente.
    /// </summary>
    [Fact]
    public async Task UseThisCloudFrameworkWeb_CorsEnabled_AppliesPolicy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                ["ThisCloud:Web:Cors:AllowedOrigins:0"] = "https://example.com",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
            })
            .Build();

        using var host = await CreateTestHost(config, app =>
        {
            app.UseThisCloudFrameworkWeb();
            app.Run(context => context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "https://example.com");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // CORS aplicado: debe tener header Access-Control-Allow-Origin
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
    }

    /// <summary>
    /// UseThisCloudFrameworkWeb con CORS deshabilitado NO aplica policy.
    /// </summary>
    [Fact]
    public async Task UseThisCloudFrameworkWeb_CorsDisabled_DoesNotApplyPolicy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "false",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
            })
            .Build();

        using var host = await CreateTestHost(config, app =>
        {
            app.UseThisCloudFrameworkWeb();
            app.Run(context => context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "https://example.com");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // CORS NO aplicado: NO debe tener header Access-Control-Allow-Origin
        response.Headers.Should().NotContainKey("Access-Control-Allow-Origin");
    }

    /// <summary>
    /// UseThisCloudFrameworkWeb siempre aplica CookiePolicy (verificar que no rompe).
    /// </summary>
    [Fact]
    public async Task UseThisCloudFrameworkWeb_AlwaysAppliesCookiePolicy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "Always",
                ["ThisCloud:Web:Cookies:HttpOnly"] = "true",
                ["ThisCloud:Web:Cookies:SameSite"] = "Strict"
            })
            .Build();

        using var host = await CreateTestHost(config, app =>
        {
            app.UseThisCloudFrameworkWeb();
            app.Run(context =>
            {
                context.Response.Cookies.Append("test-cookie", "value");
                return context.Response.WriteAsync("OK");
            });
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        // CookiePolicy aplicado: debe retornar OK sin errores
        response.Should().BeSuccessful();
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("OK");
    }

    /// <summary>
    /// UseThisCloudFrameworkSwagger es placeholder vacío (no rompe).
    /// </summary>
    [Fact]
    public async Task UseThisCloudFrameworkSwagger_IsPlaceholder_DoesNotBreak()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service"
            })
            .Build();

        using var host = await CreateTestHost(config, app =>
        {
            app.UseThisCloudFrameworkSwagger();
            app.Run(context => context.Response.WriteAsync("OK"));
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/test", TestContext.Current.CancellationToken);

        // Placeholder no rompe: debe retornar OK
        response.Should().BeSuccessful();
    }

    // Helper para crear test host con configuración custom
    private static async Task<IHost> CreateTestHost(IConfiguration config, Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Configurar servicios con ThisCloud framework
        builder.Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });
        builder.Services.AddThisCloudFrameworkWeb(config, config["ThisCloud:Web:ServiceName"] ?? "test");

        var app = builder.Build();
        configure(app);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
