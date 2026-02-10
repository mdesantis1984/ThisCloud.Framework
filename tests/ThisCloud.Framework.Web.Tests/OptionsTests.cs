using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using ThisCloud.Framework.Web.Extensions;
using ThisCloud.Framework.Web.Options;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests para Options + DI (TW2.1, TW2.2).
/// </summary>
public class OptionsTests
{
    /// <summary>
    /// TW2.1: Production + config insegura (CORS sin origins) => throw.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_CorsEnabledWithEmptyOrigins_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                // No configurar AllowedOrigins para que quede array vacío
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "Always" // Necesario en Production
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "test-service");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowedOrigins is empty*");
    }

    /// <summary>
    /// TW2.1: Production + AllowCredentials=true con wildcard "*" => throw.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_CorsWithWildcardAndCredentials_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                ["ThisCloud:Web:Cors:AllowedOrigins:0"] = "*",
                ["ThisCloud:Web:Cors:AllowCredentials"] = "true",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "Always" // Necesario en Production
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "test-service");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AllowCredentials*wildcard*");
    }

    /// <summary>
    /// TW2.1: Production + Cookies.SecurePolicy no Always => throw.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_CookiesNotSecureAlways_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "test-service");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecurePolicy*Always*Production*");
    }

    /// <summary>
    /// TW2.1: Production sin ServiceName => throw.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_MissingServiceName_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, string.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ServiceName*required*Production*");
    }

    /// <summary>
    /// TW2.2: Options binding con valores por defecto correctos (Development).
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Development_DefaultValues_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "dev-service"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "dev-service");

        act.Should().NotThrow();
        
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<ThisCloudWebOptions>>();
        options.Should().NotBeNull();
        options!.Value.ServiceName.Should().Be("dev-service");
    }

    /// <summary>
    /// TW2.2: Options binding con CORS habilitado (Development) y valores válidos.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Development_CorsEnabled_BindsCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "dev-service",
                ["ThisCloud:Web:Cors:Enabled"] = "true",
                ["ThisCloud:Web:Cors:AllowedOrigins:0"] = "https://example.com",
                ["ThisCloud:Web:Cors:AllowCredentials"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "dev-service");

        act.Should().NotThrow();

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<ThisCloudWebOptions>>();
        options!.Value.Cors.Enabled.Should().BeTrue();
        options.Value.Cors.AllowedOrigins.Should().Contain("https://example.com");
        options.Value.Cors.AllowCredentials.Should().BeTrue();
    }

    /// <summary>
    /// TW2.2: Compression habilitado se enlaza correctamente.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_CompressionEnabled_BindsCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "test-service",
                ["ThisCloud:Web:Compression:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Development" });

        services.AddThisCloudFrameworkWeb(config, "test-service");

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<ThisCloudWebOptions>>();
        options!.Value.Compression.Enabled.Should().BeTrue();
    }

    /// <summary>
    /// Test adicional: ServiceCollectionExtensions con services null => throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_NullServices_ThrowsArgumentNullException()
    {
        var config = new ConfigurationBuilder().Build();

        Action act = () => ServiceCollectionExtensions.AddThisCloudFrameworkWeb(null!, config, "test");

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    /// Test adicional: ServiceCollectionExtensions con configuration null => throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_NullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddThisCloudFrameworkWeb(null!, "test");

        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    /// <summary>
    /// Test adicional: ApplicationBuilderExtensions con app null => throw ArgumentNullException.
    /// </summary>
    [Fact]
    public void UseThisCloudFrameworkWeb_NullApp_ThrowsArgumentNullException()
    {
        Action act = () => ThisCloud.Framework.Web.Extensions.ApplicationBuilderExtensions.UseThisCloudFrameworkWeb(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("app");
    }

    /// <summary>
    /// Test adicional: Production + Cookies con defaults seguros pasa validación.
    /// </summary>
    [Fact]
    public void AddThisCloudFrameworkWeb_Production_ValidConfig_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "prod-service",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "Always",
                ["ThisCloud:Web:Cookies:SameSite"] = "Strict",
                ["ThisCloud:Web:Cookies:HttpOnly"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = "Production" });

        Action act = () => services.AddThisCloudFrameworkWeb(config, "prod-service");

        act.Should().NotThrow();
    }
}

// Fake IHostEnvironment para testing
internal class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "TestApp";
    public string ContentRootPath { get; set; } = string.Empty;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}
