using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Admin;
using ThisCloud.Framework.Loggings.Serilog;

// NUEVO SAMPLE - ID: 20260215_001500
// Sample Minimal API demonstrating ThisCloud.Framework.Loggings integration
// with Admin endpoints, environment-based gating, and policy enforcement.

var builder = WebApplication.CreateBuilder(args);

// Service name for logging context
const string ServiceName = "ThisCloud.Sample.Loggings.MinimalApi";

// Configure logging framework (Serilog)
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    ServiceName
);

// Add framework logging services
builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration,
    ServiceName
);

// Add OpenAPI/Swagger (disabled in Production)
var isDevelopment = builder.Environment.IsDevelopment();
if (isDevelopment)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
}

// Add Authentication (required by Authorization middleware)
// For this sample, we use a no-op default scheme since auth is handled via policy assertion
builder.Services.AddAuthentication().AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Microsoft.AspNetCore.Authentication.NoOpAuthenticationHandler>("NoOp", options => { });

// Add Authorization (minimal sample auth via API Key)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAssertion(context =>
        {
            // Simple API Key check from header X-Admin-ApiKey
            // Real apps should use proper identity/auth provider
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            if (httpContext == null) return false;

            var apiKey = httpContext.Request.Headers["X-Admin-ApiKey"].FirstOrDefault();
            var expectedKey = builder.Configuration["SAMPLE_ADMIN_APIKEY"];

            // If no key configured, deny (fail-safe)
            if (string.IsNullOrWhiteSpace(expectedKey)) return false;

            return apiKey == expectedKey;
        });
    });
});

var app = builder.Build();

// Configure pipeline
if (isDevelopment)
{
    app.MapOpenApi();
}

// Map framework Admin endpoints
app.MapThisCloudFrameworkLoggingsAdmin(app.Configuration);

// Health endpoint with controlled logging
app.MapGet("/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("Health check requested");
    return Results.Ok(new
    {
        status = "healthy",
        service = ServiceName,
        timestamp = DateTime.UtcNow
    });
}).WithName("GetHealth");

// Sample data endpoint demonstrating correlation
app.MapGet("/api/data", (ILogger<Program> logger) =>
{
    logger.LogInformation("Data endpoint requested");
    return Results.Ok(new
    {
        message = "Sample data response",
        timestamp = DateTime.UtcNow
    });
}).WithName("GetData");

app.Run();

// Make Program accessible for testing
public partial class Program { }
