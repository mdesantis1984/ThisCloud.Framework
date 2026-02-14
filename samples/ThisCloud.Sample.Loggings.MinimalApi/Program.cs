using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Admin;
using ThisCloud.Framework.Loggings.Serilog;
using ThisCloud.Sample.Loggings.MinimalApi.Auth;
using ThisCloud.Sample.Loggings.MinimalApi.Context;
using ThisCloud.Sample.Loggings.MinimalApi.Stores;

// NUEVO SAMPLE - ID: 20260215_001500
// Sample Minimal API demonstrating ThisCloud.Framework.Loggings integration
// with Admin endpoints, environment-based gating, and policy enforcement.

var builder = WebApplication.CreateBuilder(args);

// Service name for logging context
const string ServiceName = "ThisCloud.Sample.Loggings.MinimalApi";

// Environment and feature flags
var isDevelopment = builder.Environment.IsDevelopment();
var adminEnabled = builder.Configuration.GetValue<bool>("ThisCloud:Loggings:Admin:Enabled");

// DEV-ONLY WORKAROUND: ICorrelationContext must be Singleton for framework's root scope resolution
// Framework's HostBuilderExtensions.cs:87 resolves ICorrelationContext during Serilog bootstrap (root scope),
// but ServiceCollectionExtensions registers it as Scoped → InvalidOperationException in .NET 10.
// Production uses proper scoped implementation; this override is ONLY for sample E2E without modifying src/**.
if (isDevelopment && adminEnabled)
{
    builder.Services.AddSingleton<ICorrelationContext, SampleCorrelationContext>();
}

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

// Register in-memory settings store (SAMPLE-ONLY, does not override production implementations)
// Only needed when Admin endpoints are enabled for E2E testing
if (adminEnabled)
{
    builder.Services.TryAddSingleton<ILoggingSettingsStore, InMemoryLoggingSettingsStore>();
}

// Add OpenAPI/Swagger (disabled in Production)
if (isDevelopment)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
}

// Add Authentication (API Key via custom handler)
// For production, use proper identity provider (Azure AD, IdentityServer, etc.)
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey",
        options => { });

// Add Authorization (Admin policy requires authenticated user with Admin role)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

var app = builder.Build();

// Configure pipeline
if (isDevelopment)
{
    app.MapOpenApi();
}

// Add authentication/authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

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
