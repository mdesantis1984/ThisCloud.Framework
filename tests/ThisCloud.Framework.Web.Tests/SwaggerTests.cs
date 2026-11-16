using System;
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
using ThisCloud.Framework.Web.Middlewares;
using ThisCloud.Framework.Web.Options;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests para Swagger (W6.1-W6.4): habilitación, gating por ambiente, y protección admin.
/// NOTA: Estos tests usan el helper CreateTestHost que duplica la lógica del framework manualmente.
/// Para tests que cubren UseThisCloudFrameworkSwagger directamente, ver SwaggerIntegrationTests.
/// </summary>
public class SwaggerTests
{
    /// <summary>
    /// TW6.1: Cuando Swagger.Enabled=false, /swagger/v1/swagger.json devuelve 404.
    /// </summary>
    [Fact]
    public async Task SwaggerDisabled_ReturnsNotFound()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        using var host = CreateTestHost(config);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// TW6.2: Cuando environment no está en AllowedEnvironments, /swagger/v1/swagger.json devuelve 404.
    /// </summary>
    [Fact]
    public async Task SwaggerEnabledButEnvironmentNotAllowed_ReturnsNotFound()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:AllowedEnvironments:0"] = "Production",
            ["ThisCloud:Web:Swagger:AllowedEnvironments:1"] = "Staging",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        using var host = CreateTestHost(config, environmentName: "Development"); // Env no está en allowed
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// TW6.3a: Cuando RequireAdmin=true y usuario no tiene policy Admin, /swagger/v1/swagger.json devuelve 403.
    /// </summary>
    [Fact]
    public async Task SwaggerRequireAdminWithoutAdminClaim_ReturnsForbidden()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "true",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        using var host = CreateTestHost(config, configureServices: services =>
        {
            // Registrar autenticación fake sin admin
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Policy "Admin" que siempre falla
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => false));
            });
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// TW6.3b: Cuando RequireAdmin=true y usuario tiene policy Admin, /swagger/v1/swagger.json devuelve 200.
    /// </summary>
    [Fact]
    public async Task SwaggerRequireAdminWithAdminClaim_ReturnsOk()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "true",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        using var host = CreateTestHost(config, configureServices: services =>
        {
            // Registrar autenticación fake con admin
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Policy "Admin" que siempre pasa
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => true));
            });
        });

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// TW6.4: Cuando Swagger.Enabled=true y RequireAdmin=false (happy path sin autorización), /swagger/v1/swagger.json devuelve 200.
    /// Cubre el branch donde Swagger está habilitado, ambiente permitido, pero NO se requiere admin.
    /// </summary>
    [Fact]
    public async Task SwaggerEnabled_EnvAllowed_NoRequireAdmin_ReturnsOk()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        using var host = CreateTestHost(config);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// TW6.5: Cuando AllowedEnvironments está vacío, debe permitir todos los ambientes.
    /// Cubre el branch donde AllowedEnvironments.Length == 0.
    /// </summary>
    [Fact]
    public async Task SwaggerEnabled_EmptyAllowedEnvironments_AllowsAllEnvs()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "false",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
            // No configurar AllowedEnvironments (será array vacío)
        };

        using var host = CreateTestHost(config, environmentName: "SomeRandomEnv");
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// TW6.6: Cuando RequireAdmin=true pero IAuthorizationService es null (no registrado), debe permitir acceso.
    /// Cubre el branch donde authService == null dentro del middleware RequireAdmin.
    /// </summary>
    [Fact]
    public async Task SwaggerRequireAdmin_NoAuthServiceRegistered_ReturnsOk()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "true",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest"
        };

        // NO registrar services.AddAuthentication ni services.AddAuthorization
        // IAuthorizationService será null
        using var host = CreateTestHost(config);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Swagger document exposes the JWT Bearer security scheme and a global security requirement referencing it.
    /// </summary>
    [Fact]
    public async Task SwaggerDocument_DeclaresBearerSchemeAndGlobalRequirement()
    {
        var config = new Dictionary<string, string?>
        {
            ["ThisCloud:Web:ServiceName"] = "test-service",
            ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest",
            ["ThisCloud:Web:Swagger:Enabled"] = "true",
            ["ThisCloud:Web:Swagger:RequireAdmin"] = "false"
        };

        using var host = CreateTestHost(config);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("test-service", root.GetProperty("info").GetProperty("title").GetString());

        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());

        var security = root.GetProperty("security");
        Assert.Equal(1, security.GetArrayLength());
        Assert.True(security[0].TryGetProperty("Bearer", out var scopes));
        Assert.Equal(0, scopes.GetArrayLength());
    }

    /// <summary>
    /// Crea un host con TestServer y configuración custom.
    /// </summary>
    private static IHost CreateTestHost(
        Dictionary<string, string?> config,
        string environmentName = "Development",
        Action<IServiceCollection>? configureServices = null)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .UseEnvironment(environmentName)
            .ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(config!);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddThisCloudFrameworkWeb(context.Configuration, "test-service");
                configureServices?.Invoke(services);
            })
            .Configure(app =>
            {
                // Aplicar middlewares de forma manual (sin WebApplication)
                var options = app.ApplicationServices.GetRequiredService<IOptions<ThisCloudWebOptions>>().Value;

                // Exception mapping
                app.UseMiddleware<ExceptionMappingMiddleware>();

                // Correlation/Request IDs
                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseMiddleware<RequestIdMiddleware>();

                // Routing (requerido para Swagger)
                app.UseRouting();

                // Authentication/Authorization (solo si se configuró en configureServices)
                var authSchemes = app.ApplicationServices.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
                if (authSchemes != null)
                {
                    app.UseAuthentication();
                    app.UseAuthorization();
                }

                // CORS
                if (options.Cors.Enabled)
                {
                    app.UseCors("ThisCloudDefaultCors");
                }

                // Cookies
                app.UseCookiePolicy(new Microsoft.AspNetCore.Builder.CookiePolicyOptions
                {
                    Secure = options.Cookies.SecurePolicy,
                    HttpOnly = options.Cookies.HttpOnly ? Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always : Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None,
                    MinimumSameSitePolicy = options.Cookies.SameSite
                });

                // Swagger (W6.1-W6.4) - Usar lógica del framework
                if (options.Swagger.Enabled)
                {
                    var currentEnvironment = app.ApplicationServices.GetRequiredService<IHostEnvironment>().EnvironmentName;
                    var allowedEnvironments = options.Swagger.AllowedEnvironments ?? Array.Empty<string>();

                    if (allowedEnvironments.Length == 0 || allowedEnvironments.Contains(currentEnvironment, StringComparer.OrdinalIgnoreCase))
                    {
                        // RequireAdmin middleware
                        if (options.Swagger.RequireAdmin)
                        {
                            app.Use(async (context, next) =>
                            {
                                if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                                {
                                    var authService = context.RequestServices.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
                                    if (authService != null)
                                    {
                                        var authResult = await authService.AuthorizeAsync(context.User, null, "Admin");
                                        if (!authResult.Succeeded)
                                        {
                                            context.Response.StatusCode = 403;
                                            return;
                                        }
                                    }
                                }
                                await next(context);
                            });
                        }

                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.RoutePrefix = "swagger";
                        });
                    }
                }
            }));

        return hostBuilder.Start();
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
