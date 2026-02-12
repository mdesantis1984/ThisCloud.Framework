using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ThisCloud.Framework.Contracts.Web;
using Xunit;

namespace ThisCloud.Sample.MinimalApi.Tests;

/// <summary>
/// Custom factory to configure in-memory settings for tests.
/// </summary>
public class SampleAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ThisCloud:Web:ServiceName"] = "sample-test",
                ["ThisCloud:Web:Cors:Enabled"] = "false",
                ["ThisCloud:Web:Swagger:Enabled"] = "false",
                ["ThisCloud:Web:Cookies:SecurePolicy"] = "SameAsRequest",
                ["ThisCloud:Web:Cookies:SameSite"] = "Lax",
                ["ThisCloud:Web:Cookies:HttpOnly"] = "true",
                ["ThisCloud:Web:Compression:Enabled"] = "false"
            });
        });
    }
}

/// <summary>
/// Smoke tests for the sample Minimal API demonstrating ThisCloudFrameworkWeb usage.
/// </summary>
public class SampleSmokeTests : IClassFixture<SampleAppFactory>
{
    private readonly HttpClient _client;

    public SampleSmokeTests(SampleAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOk_Returns200WithEnvelope()
    {
        // Act
        var response = await _client.GetAsync("/ok", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(TestContext.Current.CancellationToken);
        envelope.Should().NotBeNull();
        envelope!.Meta.Should().NotBeNull();
        envelope.Meta.CorrelationId.Should().NotBeEmpty();
        envelope.Meta.RequestId.Should().NotBeEmpty();
        envelope.Data.Should().NotBeNull();
        envelope.Errors.Should().BeEmpty();

        // Verify correlation/request headers (HTTP headers are case-insensitive)
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostCreated_Returns201WithLocationHeader()
    {
        // Act
        var response = await _client.PostAsync("/created", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/items/");

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(TestContext.Current.CancellationToken);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Errors.Should().BeEmpty();

        // Verify correlation/request headers (HTTP headers are case-insensitive)
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetThrowValidation_Returns400WithValidationErrors()
    {
        // Act
        var response = await _client.GetAsync("/throw-validation", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<object?>>(TestContext.Current.CancellationToken);
        envelope.Should().NotBeNull();
        envelope!.Data.Should().BeNull();
        envelope.Errors.Should().HaveCount(1);

        var error = envelope.Errors[0];
        error.Status.Should().Be(400);
        error.Extensions.Should().ContainKey("errors");

        // Verify validation errors structure
        var validationErrors = error.Extensions!["errors"];
        validationErrors.Should().NotBeNull();

        // Verify correlation/request headers (HTTP headers are case-insensitive)
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase));
        response.Headers.Should().ContainSingle(h => h.Key.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }
}
