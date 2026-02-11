# ThisCloud.Framework.Web

**Copilot-ready web framework** for building standardized ASP.NET Core Minimal APIs with:
- ✅ Standardized HTTP contracts (envelope + ProblemDetails)
- ✅ Correlation/Request ID tracking
- ✅ Exception mapping to consistent error responses
- ✅ CORS / Cookies / Swagger with configuration-driven security
- ✅ OpenAPI documentation with Bearer authentication support
- ✅ Mandatory code coverage ≥90%

---

## Quick Start (< 15 minutes)

### 1. Installation

```bash
dotnet add package ThisCloud.Framework.Web
```

### 2. Minimal Program.cs

```csharp
using ThisCloud.Framework.Web;

var builder = WebApplication.CreateBuilder(args);

// Register framework services (CORS, Swagger, validation, middlewares)
builder.Services.AddThisCloudFrameworkWeb(
    builder.Configuration,
    serviceName: "my-api");

var app = builder.Build();

// Apply middlewares (Correlation, RequestId, Exception mapping, CORS, CookiePolicy)
app.UseThisCloudFrameworkWeb();

// Enable Swagger UI (gated by configuration)
app.UseThisCloudFrameworkSwagger();

// Your endpoints (use ThisCloudResults, NOT Results.*)
app.MapGet("/hello", () => ThisCloudResults.Ok(new { Message = "Hello!" }));

app.Run();
```

### 3. Configuration (appsettings.json)

```json
{
  "ThisCloud": {
    "Web": {
      "ServiceName": "my-api",

      "Cors": {
        "Enabled": true,
        "AllowedOrigins": ["http://localhost:3000"],
        "AllowCredentials": true
      },

      "Swagger": {
        "Enabled": true,
        "RequireAdmin": false,
        "AllowedEnvironments": ["Development"]
      },

      "Cookies": {
        "SecurePolicy": "SameAsRequest",
        "SameSite": "Lax",
        "HttpOnly": true
      },

      "Compression": {
        "Enabled": false
      }
    }
  }
}
```

**Production overrides** (`appsettings.Production.json`):
```json
{
  "ThisCloud": {
    "Web": {
      "Cors": {
        "AllowedOrigins": ["https://myapp.com"]
      },
      "Swagger": {
        "Enabled": false
      },
      "Cookies": {
        "SecurePolicy": "Always",
        "SameSite": "Strict"
      }
    }
  }
}
```

---

## Adoption Checklist

### Mandatory Rules

- ✅ **Use `ThisCloudResults` helpers** for ALL endpoint responses (NOT `Results.*` directly)
  - ✅ `ThisCloudResults.Ok<T>(data)`
  - ✅ `ThisCloudResults.Created<T>(location, data)`
  - ✅ `ThisCloudResults.BadRequest(code, title, detail, validationErrors?)`
  - ✅ `ThisCloudResults.NotFound(detail?)`
  - ✅ `ThisCloudResults.Unauthorized(detail?)`
  - ✅ `ThisCloudResults.Forbidden(detail?)`
  - ✅ `ThisCloudResults.Conflict(detail?)`
  - ✅ `ThisCloudResults.UpstreamFailure(detail?)` (502)
  - ✅ `ThisCloudResults.UpstreamTimeout(detail?)` (504)
  - ✅ `ThisCloudResults.Unhandled(detail?)` (500)
  - ✅ `ThisCloudResults.SeeOther(location)` (303)

- ✅ **Throw typed exceptions** (mapped automatically by `ExceptionMappingMiddleware`):
  - `ValidationException(message, validationErrors)` → 400 + `ErrorCode.VALIDATION_ERROR`
  - `NotFoundException(message)` → 404 + `ErrorCode.NOT_FOUND`
  - `ConflictException(message)` → 409 + `ErrorCode.CONFLICT`
  - `ForbiddenException(message)` → 403 + `ErrorCode.FORBIDDEN`
  - `UnauthorizedAccessException(message)` → 401 + `ErrorCode.UNAUTHORIZED`
  - `HttpRequestException(message)` → 502 + `ErrorCode.UPSTREAM_FAILURE`
  - `TimeoutException(message)` → 504 + `ErrorCode.UPSTREAM_TIMEOUT`
  - `Exception` (catch-all) → 500 + `ErrorCode.UNHANDLED_ERROR`

- ✅ **Production configuration**:
  - ✅ `ServiceName` must be set
  - ✅ `Cors.AllowedOrigins` must be explicit (NO wildcard `"*"` if `AllowCredentials=true`)
  - ✅ `Cookies.SecurePolicy` must be `"Always"`
  - ✅ `Swagger.Enabled` should be `false` (or gated by `AllowedEnvironments`)

- ✅ **Standard headers** (automatic):
  - `X-Correlation-Id` (GUID, preserved if valid or generated)
  - `X-Request-Id` (GUID, preserved if valid or generated)
  - Both are returned in response headers AND `meta` section of envelope

### Configuration Options

#### Cors
| Key | Type | Default | Production Rule |
|-----|------|---------|----------------|
| `Enabled` | `bool` | `false` | If `true`, `AllowedOrigins` must be explicit |
| `AllowedOrigins` | `string[]` | `[]` | NO wildcard `"*"` if `AllowCredentials=true` |
| `AllowCredentials` | `bool` | `false` | — |

#### Swagger
| Key | Type | Default | Production Rule |
|-----|------|---------|----------------|
| `Enabled` | `bool` | `false` | Should be `false` in Production |
| `RequireAdmin` | `bool` | `false` | If `true`, requires policy `"Admin"` for `/swagger` paths |
| `AllowedEnvironments` | `string[]` | `[]` | Only enable in listed environments (e.g., `["Development"]`) |

#### Cookies
| Key | Type | Default | Production Rule |
|-----|------|---------|----------------|
| `SecurePolicy` | `string` | `"SameAsRequest"` | Must be `"Always"` in Production |
| `SameSite` | `string` | `"Lax"` | `"Strict"` recommended in Production |
| `HttpOnly` | `bool` | `true` | — |

Options: `SecurePolicy` → `Always` | `SameAsRequest` | `None`  
Options: `SameSite` → `Strict` | `Lax` | `None`

#### Compression
| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `Enabled` | `bool` | `false` | ⚠️ **POSTPONED** in .NET 10 (W5.2) — API not available yet |

---

## Standard Envelope

All JSON responses follow this contract:

```json
{
  "meta": {
    "service": "my-api",
    "version": "v1",
    "timestampUtc": "2026-02-11T12:00:00Z",
    "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "requestId": "7f8c1e4a-9b2d-4d6f-a3c5-1e7d8f9a0b3c",
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
  },
  "data": {
    "message": "Success"
  },
  "errors": []
}
```

**On error** (e.g., 400 BadRequest):
```json
{
  "meta": { ... },
  "data": null,
  "errors": [
    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Validation Failed",
      "status": 400,
      "detail": "One or more validation errors occurred.",
      "instance": "/api/users",
      "code": "VALIDATION_ERROR",
      "extensions": {
        "errors": {
          "Email": ["Invalid email format", "Email is required"],
          "Age": ["Must be 18 or older"]
        }
      }
    }
  ]
}
```

---

## Top Status Codes (Mandatory OpenAPI Coverage)

| Code | Use Case | Helper Method |
|------|----------|---------------|
| **200** | Success | `ThisCloudResults.Ok<T>(data)` |
| **201** | Created (+ `Location` header) | `ThisCloudResults.Created<T>(location, data)` |
| **303** | See Other (redirect) | `ThisCloudResults.SeeOther(location)` |
| **400** | Bad Request (validation) | `ThisCloudResults.BadRequest(...)` or throw `ValidationException` |
| **401** | Unauthorized | `ThisCloudResults.Unauthorized(...)` or throw `UnauthorizedAccessException` |
| **403** | Forbidden | `ThisCloudResults.Forbidden(...)` or throw `ForbiddenException` |
| **404** | Not Found | `ThisCloudResults.NotFound(...)` or throw `NotFoundException` |
| **409** | Conflict | `ThisCloudResults.Conflict(...)` or throw `ConflictException` |
| **500** | Internal Server Error | `ThisCloudResults.Unhandled(...)` or unhandled `Exception` |
| **502** | Bad Gateway (upstream failure) | `ThisCloudResults.UpstreamFailure(...)` or `HttpRequestException` |
| **504** | Gateway Timeout | `ThisCloudResults.UpstreamTimeout(...)` or `TimeoutException` |

---

## OpenAPI / Swagger

- **Bearer Authentication** scheme is auto-configured
- **UI route:** `/swagger` (customizable via Swashbuckle options)
- **Gating:**
  - Disabled if `Swagger.Enabled != true`
  - Returns 404 if environment NOT in `AllowedEnvironments`
  - Returns 403 if `RequireAdmin=true` and user lacks `"Admin"` policy

**Example:** Only enable Swagger in Development:
```json
{
  "ThisCloud": {
    "Web": {
      "Swagger": {
        "Enabled": true,
        "AllowedEnvironments": ["Development"]
      }
    }
  }
}
```

---

## Code Coverage

This framework **enforces ≥90% line coverage** on all builds:

```bash
dotnet test -c Release \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=90 \
  /p:ThresholdType=line
```

**Build fails** if coverage drops below 90%.

---

## Known Limitations

### ResponseCompression (W5.2) — POSTPONED
- **Status:** Not available in .NET 10 (as of 2026-02-11)
- **Reason:** `Microsoft.AspNetCore.ResponseCompression` extension methods not present
- **Workaround:** `CompressionOptions.Enabled` is a placeholder; setting it has NO effect currently
- **Tracking:** Will be implemented when .NET 10 APIs become available

---

## Sample Application

See [`samples/ThisCloud.Sample.MinimalApi`](samples/ThisCloud.Sample.MinimalApi) for a working example with:
- ✅ 3 endpoints (200 OK, 201 Created, 400 ValidationException)
- ✅ Complete `appsettings.json` configuration
- ✅ Swagger UI enabled in Development
- ✅ CORS configured for `http://localhost:3000`

Run the sample:
```bash
cd samples/ThisCloud.Sample.MinimalApi
dotnet run
# Browse to: https://localhost:<port>/swagger
```

---

## Architecture

- **Clean Architecture** principles applied
- **Onion layering:**
  - `ThisCloud.Framework.Contracts` (Core): DTOs, exceptions (no ASP.NET dependencies)
  - `ThisCloud.Framework.Web` (Infrastructure): Middlewares, DI extensions, options validation
- **SOLID** compliance: interfaces in Core, implementations in Web layer

---

## Contributing

1. Create feature branch from `develop`
2. Follow [Git Flow](docs/Plan_ThisCloud_Framework_Web_v9.md#git-flow-mandatorio--reglas-operativas-no-ambiguas)
3. Ensure tests pass and coverage ≥90%
4. Create PR to `develop` (CI must pass)
5. Only ONE feature branch active at a time

---

## NuGet Package

Published to **GitHub Packages** (NuGet) via GitHub Actions:
- **CI workflow:** Runs on PR to `develop`/`main`
- **Publish workflow:** Runs on push to `main` (after PR merge)
- **Versioning:** Auto-incremental via [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)

---

## License

MIT (or your org license)

---

## Support

- **Issues:** [GitHub Issues](https://github.com/mdesantis1984/ThisCloud.Framework/issues)
- **Plan:** [docs/Plan_ThisCloud_Framework_Web_v9.md](docs/Plan_ThisCloud_Framework_Web_v9.md)