using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ThisCloud.Framework.Contracts.Exceptions;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Extensions;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests para ExceptionMappingMiddleware (TW4.1-TW4.3).
/// </summary>
public class ExceptionMappingTests : IDisposable
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public ExceptionMappingTests()
    {
        var builder = new WebHostBuilder()
            .UseEnvironment("Development")
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ThisCloud:Web:ServiceName"] = "test-service",
                    ["ThisCloud:Web:Cors:Enabled"] = "false",
                    ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest",
                    ["ThisCloud:Web:Cookies:HttpOnly"] = "true",
                    ["ThisCloud:Web:Cookies:SameSite"] = "Lax"
                });
            })
            .ConfigureServices((context, services) =>
            {
                services.AddThisCloudFrameworkWeb(context.Configuration, "test-service");
                services.AddRouting();
            })
            .Configure(app =>
            {
                var webApp = (IApplicationBuilder)app;
                
                // Necesitamos simular WebApplication para UseThisCloudFrameworkWeb
                // Usar middleware directamente
                app.UseMiddleware<Web.Middlewares.ExceptionMappingMiddleware>();
                app.UseMiddleware<Web.Middlewares.CorrelationIdMiddleware>();
                app.UseMiddleware<Web.Middlewares.RequestIdMiddleware>();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    // TW4.1: Endpoint que lanza ValidationException
                    endpoints.MapGet("/test/validation-error", () =>
                    {
                        var errors = new Dictionary<string, string[]?>
                        {
                            ["email"] = new[] { "Invalid email format" },
                            ["age"] = new[] { "Must be at least 18" }
                        };
                        throw new ValidationException("Validation failed", errors);
                    });

                    // TW4.2: Endpoint que lanza HttpRequestException
                    endpoints.MapGet("/test/upstream-failure", () =>
                    {
                        throw new HttpRequestException("External API returned 500");
                    });

                    // TW4.3: Endpoint que lanza NotFoundException
                    endpoints.MapGet("/test/not-found", () =>
                    {
                        throw new NotFoundException("User with ID 123 not found");
                    });

                    // Endpoint que lanza ConflictException
                    endpoints.MapGet("/test/conflict", () =>
                    {
                        throw new ConflictException("Email already exists");
                    });

                    // Endpoint que lanza ForbiddenException
                    endpoints.MapGet("/test/forbidden", () =>
                    {
                        throw new ForbiddenException("Insufficient permissions");
                    });

                    // Endpoint que lanza UnauthorizedAccessException
                    endpoints.MapGet("/test/unauthorized", () =>
                    {
                        throw new UnauthorizedAccessException("Token expired");
                    });

                    // Endpoint que lanza TimeoutException
                    endpoints.MapGet("/test/timeout", () =>
                    {
                        throw new TimeoutException("Operation timed out");
                    });

                    // Endpoint que lanza Exception genérica
                    endpoints.MapGet("/test/unhandled", () =>
                    {
                        throw new InvalidOperationException("Something went wrong");
                    });
                });
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    /// <summary>
    /// TW4.1: ValidationException debe retornar 400 con envelope y validation errors en extensions.
    /// </summary>
    [Fact]
    public async Task WhenValidationException_Returns400WithEnvelopeAndValidationErrors()
    {
        // Act
        var response = await _client.GetAsync("/test/validation-error", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Meta);
        Assert.Equal("test-service", envelope.Meta.Service);
        Assert.Single(envelope.Errors);

        var error = envelope.Errors[0];
        Assert.Equal(400, error.Status);
        Assert.Equal("Validation Error", error.Title);
        Assert.Equal("Validation failed", error.Detail);
        Assert.Contains("https://httpstatuses.io/400", error.Type);

        // Verificar validationErrors en extensions
        Assert.True(error.Extensions.ContainsKey("errors"));
        var validationErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            JsonSerializer.Serialize(error.Extensions["errors"]));
        Assert.NotNull(validationErrors);
        Assert.Contains("email", validationErrors.Keys);
        Assert.Contains("age", validationErrors.Keys);
    }

    /// <summary>
    /// TW4.2: HttpRequestException debe retornar 502 con envelope.
    /// </summary>
    [Fact]
    public async Task WhenHttpRequestException_Returns502WithEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/test/upstream-failure", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.Single(envelope.Errors);

        var error = envelope.Errors[0];
        Assert.Equal(502, error.Status);
        Assert.Equal("Bad Gateway", error.Title);
        Assert.Contains("External API returned 500", error.Detail);
    }

    /// <summary>
    /// TW4.3: Envelope debe incluir meta completo (correlationId, requestId, traceId).
    /// </summary>
    [Fact]
    public async Task WhenExceptionThrown_EnvelopeIncludesCompleteMeta()
    {
        // Act
        var response = await _client.GetAsync("/test/not-found", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Meta);
        Assert.Equal("test-service", envelope.Meta.Service);
        Assert.NotEqual(Guid.Empty, envelope.Meta.CorrelationId);
        Assert.NotEqual(Guid.Empty, envelope.Meta.RequestId);
        Assert.NotNull(envelope.Meta.TraceId);
        Assert.NotEmpty(envelope.Meta.TraceId);
        Assert.True(envelope.Meta.TimestampUtc <= DateTime.UtcNow);
    }

    /// <summary>
    /// NotFoundException debe retornar 404.
    /// </summary>
    [Fact]
    public async Task WhenNotFoundException_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/test/not-found", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(404, error.Status);
        Assert.Equal("Not Found", error.Title);
    }

    /// <summary>
    /// ConflictException debe retornar 409.
    /// </summary>
    [Fact]
    public async Task WhenConflictException_Returns409()
    {
        // Act
        var response = await _client.GetAsync("/test/conflict", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(409, error.Status);
        Assert.Equal("Conflict", error.Title);
    }

    /// <summary>
    /// ForbiddenException debe retornar 403.
    /// </summary>
    [Fact]
    public async Task WhenForbiddenException_Returns403()
    {
        // Act
        var response = await _client.GetAsync("/test/forbidden", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(403, error.Status);
        Assert.Equal("Forbidden", error.Title);
    }

    /// <summary>
    /// UnauthorizedAccessException debe retornar 401.
    /// </summary>
    [Fact]
    public async Task WhenUnauthorizedAccessException_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/test/unauthorized", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(401, error.Status);
        Assert.Equal("Unauthorized", error.Title);
    }

    /// <summary>
    /// TimeoutException debe retornar 504.
    /// </summary>
    [Fact]
    public async Task WhenTimeoutException_Returns504()
    {
        // Act
        var response = await _client.GetAsync("/test/timeout", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(504, error.Status);
        Assert.Equal("Gateway Timeout", error.Title);
    }

    /// <summary>
    /// Exception genérica debe retornar 500.
    /// </summary>
    [Fact]
    public async Task WhenUnhandledException_Returns500()
    {
        // Act
        var response = await _client.GetAsync("/test/unhandled", TestContext.Current.CancellationToken);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(envelope);
        var error = envelope.Errors[0];
        Assert.Equal(500, error.Status);
        Assert.Equal("Internal Server Error", error.Title);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _server?.Dispose();
    }
}
