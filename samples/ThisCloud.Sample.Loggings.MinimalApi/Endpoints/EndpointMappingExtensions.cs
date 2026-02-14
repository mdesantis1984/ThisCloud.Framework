// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Admin;

namespace ThisCloud.Sample.Loggings.MinimalApi.Endpoints;

/// <summary>
/// Options for configuring endpoint mapping in the sample application.
/// </summary>
public sealed class EndpointMapOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Swagger/OpenAPI should be enabled in Development.
    /// </summary>
    public bool EnableSwaggerInDev { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Admin endpoints should be mapped.
    /// </summary>
    /// <remarks>
    /// This is read from configuration (ThisCloud:Loggings:Admin:Enabled) and gated by environment.
    /// </remarks>
    public bool EnableAdmin { get; set; }

    /// <summary>
    /// Gets or sets the base path for public API endpoints.
    /// </summary>
    public string PublicApiBasePath { get; set; } = "/api";
}

/// <summary>
/// Extension methods for centralized endpoint mapping in the sample application.
/// </summary>
public static class EndpointMappingExtensions
{
    /// <summary>
    /// Maps all endpoints for the sample application (health, public API, admin, swagger).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="options">Endpoint mapping options.</param>
    /// <param name="apiName">Name of the API (used for grouping/tags).</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication SetEndpointMapAPIAll(
        this WebApplication app,
        EndpointMapOptions options,
        string apiName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(apiName);

        // Swagger/OpenAPI (Development only)
        if (app.Environment.IsDevelopment() && options.EnableSwaggerInDev)
        {
            app.MapOpenApi();
        }

        // Health endpoint (always available)
        const string serviceName = "ThisCloud.Sample.Loggings.MinimalApi";
        app.MapGet("/health", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Health check requested");
            return Results.Ok(new
            {
                status = "healthy",
                service = serviceName,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetHealth")
        .WithTags(apiName);

        // Public API endpoints
        var publicGroup = app.MapGroup(options.PublicApiBasePath)
            .WithTags(apiName);

        publicGroup.MapGet("/data", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Data endpoint requested");
            return Results.Ok(new
            {
                message = "Sample data response",
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetData");

        // Admin endpoints (gated by configuration)
        if (options.EnableAdmin)
        {
            app.MapThisCloudFrameworkLoggingsAdmin(app.Configuration);
        }

        return app;
    }
}
