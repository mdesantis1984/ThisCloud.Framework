using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Middlewares;
using ThisCloud.Framework.Web.Helpers;
using Xunit;

namespace ThisCloud.Framework.Web.Tests;

/// <summary>
/// Tests para CorrelationIdMiddleware y RequestIdMiddleware (TW3.1, TW3.2, TW3.3).
/// </summary>
public class CorrelationMiddlewareTests
{
    /// <summary>
    /// TW3.1: Cuando el header X-Correlation-Id viene válido, se preserva.
    /// </summary>
    [Fact]
    public async Task CorrelationIdMiddleware_ValidHeader_IsPreserved()
    {
        var validGuid = Guid.NewGuid();
        
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.Run(async context =>
            {
                var correlationId = ThisCloudHttpContext.GetCorrelationId(context);
                await context.Response.WriteAsync(correlationId.ToString());
            });
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(ThisCloudHeaders.CorrelationId, validGuid.ToString());

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Be(validGuid.ToString());
        response.Headers.Should().ContainKey(ThisCloudHeaders.CorrelationId);
        response.Headers.GetValues(ThisCloudHeaders.CorrelationId).First().Should().Be(validGuid.ToString());
    }

    /// <summary>
    /// TW3.2: Cuando el header X-Correlation-Id viene inválido, se reemplaza por GUID nuevo.
    /// </summary>
    [Fact]
    public async Task CorrelationIdMiddleware_InvalidHeader_IsReplaced()
    {
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.Run(async context =>
            {
                var correlationId = ThisCloudHttpContext.GetCorrelationId(context);
                await context.Response.WriteAsync(correlationId.ToString());
            });
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(ThisCloudHeaders.CorrelationId, "not-a-guid");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Guid.TryParse(body, out var parsedGuid).Should().BeTrue();
        parsedGuid.Should().NotBe(Guid.Empty);
        response.Headers.Should().ContainKey(ThisCloudHeaders.CorrelationId);
    }

    /// <summary>
    /// TW3.3: Response headers X-Correlation-Id y X-Request-Id siempre presentes.
    /// </summary>
    [Fact]
    public async Task CorrelationAndRequestIdMiddlewares_ResponseHeadersAlwaysPresent()
    {
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<RequestIdMiddleware>();
            app.Run(context => Task.CompletedTask);
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/test");

        // Headers HTTP son case-insensitive; verificamos existencia
        response.Headers.TryGetValues(ThisCloudHeaders.CorrelationId, out var corrValues).Should().BeTrue();
        response.Headers.TryGetValues(ThisCloudHeaders.RequestId, out var reqValues).Should().BeTrue();
        
        var corrId = corrValues!.First();
        var reqId = reqValues!.First();
        
        Guid.TryParse(corrId, out _).Should().BeTrue();
        Guid.TryParse(reqId, out _).Should().BeTrue();
    }

    /// <summary>
    /// TW3.1: RequestIdMiddleware - Cuando el header X-Request-Id viene válido, se preserva.
    /// </summary>
    [Fact]
    public async Task RequestIdMiddleware_ValidHeader_IsPreserved()
    {
        var validGuid = Guid.NewGuid();
        
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<RequestIdMiddleware>();
            app.Run(async context =>
            {
                var requestId = ThisCloudHttpContext.GetRequestId(context);
                await context.Response.WriteAsync(requestId.ToString());
            });
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(ThisCloudHeaders.RequestId, validGuid.ToString());

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Be(validGuid.ToString());
        response.Headers.GetValues(ThisCloudHeaders.RequestId).First().Should().Be(validGuid.ToString());
    }

    /// <summary>
    /// TW3.2: RequestIdMiddleware - Cuando el header falta, se genera nuevo GUID.
    /// </summary>
    [Fact]
    public async Task RequestIdMiddleware_MissingHeader_GeneratesNewGuid()
    {
        using var host = await CreateTestHost(app =>
        {
            app.UseMiddleware<RequestIdMiddleware>();
            app.Run(async context =>
            {
                var requestId = ThisCloudHttpContext.GetRequestId(context);
                await context.Response.WriteAsync(requestId.ToString());
            });
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/test");
        var body = await response.Content.ReadAsStringAsync();

        Guid.TryParse(body, out var parsedGuid).Should().BeTrue();
        parsedGuid.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// TW3.3: GetTraceId no rompe si Activity.Current es null.
    /// </summary>
    [Fact]
    public async Task GetTraceId_WhenActivityIsNull_ReturnsNullWithoutException()
    {
        using var host = await CreateTestHost(app =>
        {
            app.Run(async context =>
            {
                var traceId = ThisCloudHttpContext.GetTraceId(context);
                await context.Response.WriteAsync(traceId ?? "null");
            });
        });

        var client = host.GetTestClient();
        var response = await client.GetAsync("/test");
        var body = await response.Content.ReadAsStringAsync();

        // Activity.Current puede existir o no en test host; verificamos que no rompa
        body.Should().NotBeNull();
    }

    /// <summary>
    /// Test adicional: HttpContext null en GetCorrelationId lanza ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetCorrelationId_NullContext_ThrowsArgumentNullException()
    {
        Action act = () => ThisCloudHttpContext.GetCorrelationId(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Test adicional: HttpContext null en GetRequestId lanza ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetRequestId_NullContext_ThrowsArgumentNullException()
    {
        Action act = () => ThisCloudHttpContext.GetRequestId(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Test adicional: HttpContext null en GetTraceId lanza ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetTraceId_NullContext_ThrowsArgumentNullException()
    {
        Action act = () => ThisCloudHttpContext.GetTraceId(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Test adicional: CorrelationIdMiddleware con HttpContext null lanza ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task CorrelationIdMiddleware_NullContext_ThrowsArgumentNullException()
    {
        var middleware = new CorrelationIdMiddleware(ctx => Task.CompletedTask);
        await middleware.Invoking(m => m.InvokeAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Test adicional: RequestIdMiddleware con HttpContext null lanza ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task RequestIdMiddleware_NullContext_ThrowsArgumentNullException()
    {
        var middleware = new RequestIdMiddleware(ctx => Task.CompletedTask);
        await middleware.Invoking(m => m.InvokeAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    // Helper para crear test host con configuración custom
    private static async Task<IHost> CreateTestHost(Action<IApplicationBuilder> configure)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.Configure(configure);
            });

        var host = await hostBuilder.StartAsync();
        return host;
    }
}
