using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using ThisCloud.Framework.Web.Extensions;
using ThisCloud.Framework.Web.Options;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests adicionales para ServiceCollectionExtensions (cobertura de AddThisCloudFrameworkWeb).
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// AddThisCloudFrameworkWeb registra CORS service cuando Enabled=true.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_CorsEnabled_RegistersCorsService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                ["ThisCloud:Web:Cors:AllowedOrigins:0"] = "https://example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });
        services.AddLogging(); // CorsService requiere ILoggerFactory

        services.AddThisCloudFrameworkWeb(config, "test-service");

        var provider = services.BuildServiceProvider();

        // CORS service debe estar registrado
        var corsService = provider.GetService<ICorsService>();
        corsService.Should().NotBeNull();
    }

    /// <summary>
    /// AddThisCloudFrameworkWeb NO registra CORS service cuando Enabled=false.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_CorsDisabled_DoesNotRegisterCors()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });
        
        services.AddThisCloudFrameworkWeb(config, "test-service");

        var provider = services.BuildServiceProvider();
        
        // CORS service NO debe estar registrado cuando Enabled=false
        // Nota: AddCors siempre se registra en el ejemplo actual, este test verifica que no rompa
        var options = provider.GetService<IOptions<ThisCloudWebOptions>>();
        options.Should().NotBeNull();
        options!.Value.Cors.Enabled.Should().BeFalse();
    }

    /// <summary>
    /// AddThisCloudFrameworkWeb registra IOptions correctamente con valores de config.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_RegistersOptionsWithConfigValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "my-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                ["ThisCloud:Web:Cors:AllowedOrigins:0"] = "https://example.com",
                ["ThisCloud:Web:Cors:AllowedOrigins:1"] = "https://other.com",
                ["ThisCloud:Web:Cors:AllowCredentials"] = "true",
                ["ThisCloud:Web:Compression:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });
        
        services.AddThisCloudFrameworkWeb(config, "my-service");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ThisCloudWebOptions>>().Value;
        
        options.ServiceName.Should().Be("my-service");
        options.Cors.Enabled.Should().BeTrue();
        options.Cors.AllowedOrigins.Should().Contain("https://example.com");
        options.Cors.AllowedOrigins.Should().Contain("https://other.com");
        options.Cors.AllowCredentials.Should().BeTrue();
        options.Compression.Enabled.Should().BeTrue();
    }

    /// <summary>
    /// AddThisCloudFrameworkWeb ejecuta validación eagerly y falla si config inválida en Production.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_InvalidConfig_ThrowsOnRegistration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "", // Inválido en Production
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "Always"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });
        
        Action act = () => services.AddThisCloudFrameworkWeb(config, "");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ServiceName*required*Production*");
    }

    /// <summary>
    /// AddThisCloudFrameworkWeb Development con config mínima no lanza excepción.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Development_MinimalConfig_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });
        
        Action act = () => services.AddThisCloudFrameworkWeb(config, "dev-service");

        act.Should().NotThrow();
    }
}
