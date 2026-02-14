# ThisCloud.Framework.Web

**Copilot-ready web framework for ASP.NET Core Minimal APIs with standardized middlewares, CORS, and Swagger integration.**

## What is this?

`ThisCloud.Framework.Web` provides production-ready middleware, exception handling, correlation tracking, CORS configuration, and Swagger/OpenAPI integration for ASP.NET Core Minimal APIs.

## Installation

```bash
dotnet add package ThisCloud.Framework.Web
```

## Quick Start

```csharp
using ThisCloud.Framework.Web;

var builder = WebApplication.CreateBuilder(args);

// Add ThisCloud.Framework.Web services
builder.Services.AddThisCloudWeb(builder.Configuration);

var app = builder.Build();

// Use middlewares (correlation, exception mapping, etc.)
app.UseThisCloudWeb();

app.MapGet("/", () => "Hello from ThisCloud.Framework.Web!");

app.Run();
```

## Features

- ✅ **Correlation middleware** - Automatic request correlation ID tracking
- ✅ **Exception mapping** - Structured ProblemDetails responses for all exceptions
- ✅ **CORS support** - Configurable CORS policies via appsettings.json
- ✅ **Swagger/OpenAPI** - Auto-configured with security schemes and examples
- ✅ **Minimal API optimized** - Designed for .NET Minimal APIs
- ✅ **90% coverage enforced** - Battle-tested with mandatory test coverage

## Documentation

For full documentation, middleware guides, and advanced configuration:
- [Repository](https://github.com/mdesantis1984/ThisCloud.Framework)
- [Web Framework Docs](https://github.com/mdesantis1984/ThisCloud.Framework/tree/main/docs/web)

## License

ISC License - see [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE) for details.

## Support

This is an open-source project maintained on a best-effort basis. For issues or contributions, visit the [GitHub repository](https://github.com/mdesantis1984/ThisCloud.Framework).
