using ThisCloud.Framework.Contracts.Exceptions;
using ThisCloud.Framework.Web.Extensions;
using ThisCloud.Framework.Web.Results;

var builder = WebApplication.CreateBuilder(args);

// Add endpoints API explorer (required for OpenAPI)
builder.Services.AddEndpointsApiExplorer();

// Add ThisCloud Framework Web (CORS, Cookies, Swagger, Exception mapping, Correlation/RequestId middlewares)
builder.Services.AddThisCloudFrameworkWeb(
    builder.Configuration,
    serviceName: "sample-minimal-api");

var app = builder.Build();

// Apply ThisCloud middlewares (Correlation, RequestId, Exception mapping, CORS, CookiePolicy)
app.UseThisCloudFrameworkWeb();

// Enable Swagger UI if configured (gated by AllowedEnvironments + RequireAdmin)
app.UseThisCloudFrameworkSwagger();

// Sample endpoints demonstrating ThisCloudResults usage

app.MapGet("/ok", () =>
{
    return ThisCloudResults.Ok(new
    {
        Message = "Hello from ThisCloud.Framework.Web sample!",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("GetOk");

app.MapPost("/created", () =>
{
    var id = Guid.NewGuid();
    var location = $"/items/{id}";
    var data = new { Id = id, Name = "New Item", CreatedAt = DateTime.UtcNow };

    return ThisCloudResults.Created(location, data);
})
.WithName("PostCreated");

app.MapGet("/throw-validation", () =>
{
    var validationErrors = new Dictionary<string, string[]?>
    {
        { "Email", new[] { "Invalid email format", "Email is required" } },
        { "Age", new[] { "Must be 18 or older" } }
    };

    throw new ValidationException("Validation failed for user input", validationErrors);
})
.WithName("GetThrowValidation");

app.Run();

// Make Program accessible for tests (WebApplicationFactory)
public partial class Program { }
