using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThisCloud.Framework.Web.Extensions;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Integration tests que llaman directamente a UseThisCloudFrameworkSwagger() para cubrir el código real.
/// Estos tests usan WebApplication para permitir el testing del método de extensión real.
/// </summary>
public class SwaggerIntegrationTests
{
    /// <summary>
    /// TW6.INT1: Swagger habilitado sin RequireAdmin debe retornar 200.
    /// Cubre UseThisCloudFrameworkSwagger con Enabled=true, env permitido, RequireAdmin=false.
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_EnabledNoAdmin_ReturnsOk()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        });
        
        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        builder.WebHost.UseTestServer();
        
        var app = builder.Build();
        
        // Aplicar middlewares del framework
        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseThisCloudFrameworkSwagger(); // ← Llama al método real

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// TW6.INT2: Swagger deshabilitado debe retornar 404.
    /// Cubre UseThisCloudFrameworkSwagger con Enabled=false (early return línea 76).
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_Disabled_ReturnsNotFound()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        });

        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseThisCloudFrameworkSwagger();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// TW6.INT3: Ambiente no permitido debe retornar 404.
    /// Cubre UseThisCloudFrameworkSwagger con env gating (early return línea 85).
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_EnvironmentNotAllowed_ReturnsNotFound()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:AllowedEnvironments:0"] = "Production",
            ["ThisCloud:Web:Swagger:AllowedEnvironments:1"] = "Staging",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        });

        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseThisCloudFrameworkSwagger();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// TW6.INT4: RequireAdmin con authorization que falla debe retornar 403.
    /// Cubre UseThisCloudFrameworkSwagger con RequireAdmin middleware (líneas 89-107).
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_RequireAdminFails_ReturnsForbidden()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "true",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        });
        
        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        
        // Configurar autenticación y autorización
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => false));
        });

        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseThisCloudFrameworkSwagger();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// TW6.INT5: RequireAdmin con authorization exitosa debe retornar 200.
    /// Cubre UseThisCloudFrameworkSwagger con RequireAdmin middleware pasando (líneas 89-115).
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_RequireAdminSucceeds_ReturnsOk()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "true",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        });
        
        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        
        // Configurar autenticación y autorización
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => true));
        });

        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseThisCloudFrameworkSwagger();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// TW6.INT6: AllowedEnvironments vacío debe permitir todos los ambientes.
    /// Cubre branch línea 83 donde AllowedEnvironments.Length == 0.
    /// </summary>
    [Fact]
    public async Task UseSwaggerMethod_EmptyAllowedEnvironments_AllowsAllEnvs()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "RandomEnv"
        });
        
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
            // NO configurar AllowedEnvironments
        });
        
        builder.Services.AddThisCloudFrameworkWeb(builder.Configuration, "test-service");
        builder.WebHost.UseTestServer();

        var app = builder.Build();

        app.UseThisCloudFrameworkWeb();
        app.UseRouting();
        app.UseThisCloudFrameworkSwagger();

        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = app.GetTestServer().CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }

    /// <summary>
    /// Authentication handler fake para tests.
    /// </summary>
    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "test-user") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
