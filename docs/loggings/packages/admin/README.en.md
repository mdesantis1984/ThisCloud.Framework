# ThisCloud.Framework.Loggings.Admin

> 📘 **Language**: English | [Español (full documentation)](README.es.md)

## Purpose

**Runtime admin** endpoints for the **ThisCloud.Framework.Loggings** framework. Allows:

- ✅ Enable/Disable logging dynamically (no app restart)
- ✅ GET/PUT/PATCH settings (levels, sinks, overrides)
- ✅ DELETE settings (reset to defaults)
- ✅ Environment gating (`AllowedEnvironments`)
- ✅ Policy protection (`RequireAdmin=true` → policy `"Admin"`)
- ✅ Structured audit logging of changes

**⚠️ WARNING**: These endpoints **MUST NOT be exposed in Production** without proper protection.

---

## 📦 Installation

```bash
dotnet add package ThisCloud.Framework.Loggings.Admin
```

**Minimum version**: .NET 10

---

## ⚡ Quick Start

```csharp
using ThisCloud.Framework.Loggings.Serilog;
using ThisCloud.Framework.Loggings.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "my-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "my-api");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.UseAuthorization();
app.MapThisCloudFrameworkLoggingsAdmin(app.Configuration);

app.Run();
```

### appsettings.Development.json

```json
{
  "ThisCloud": {
    "Loggings": {
      "Admin": {
        "Enabled": true,
        "AllowedEnvironments": ["Development"],
        "RequireAdmin": false,
        "BasePath": "/api/admin/logging"
      }
    }
  }
}
```

### appsettings.Production.json

```json
{
  "ThisCloud": {
    "Loggings": {
      "Admin": {
        "Enabled": false
      }
    }
  }
}
```

**Mandatory rule**: If `Enabled=true` in Production, **MUST** have:
- `AllowedEnvironments` explicit (e.g., `["Staging"]`)
- `RequireAdmin=true`
- Policy `"Admin"` configured with real roles/claims

---

## 🎯 Endpoints

Base path default: `/api/admin/logging`

- **GET `/settings`**: Get current configuration
- **PUT `/settings`**: Replace entire configuration (validated)
- **PATCH `/settings`**: Partial merge (only present fields updated)
- **POST `/enable`**: Enable logging (`IsEnabled=true`)
- **POST `/disable`**: Disable logging (`IsEnabled=false`)
- **DELETE `/settings`**: Reset to hardcoded defaults

---

## 🛡️ Security

### Automatic Gating (fail closed, since 1.1)

Endpoints **NOT exposed** if:
1. `Admin.Enabled=false` (default)
2. Current environment NOT in `AllowedEnvironments`. An **empty** `AllowedEnvironments` allows **only `Development`** (it never means "all environments"); a non-empty list keeps its meaning.

**Startup failure**: if Admin is enabled and mapped in any environment other than `Development` while `RequireAdmin=false`, `MapThisCloudFrameworkLoggingsAdmin` throws `InvalidOperationException` with a message naming the environment and `RequireAdmin`. Fix it by setting `RequireAdmin=true` (and configuring the `"Admin"` policy) or by disabling Admin. Development behavior is unchanged.

### Policy Protection

`RequireAdmin` defaults to `false` in code and is **mandatory** (`true`) outside Development. If `RequireAdmin=true`, endpoints require:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
```

### Runtime Settings Validation (PUT / PATCH)

Invalid input is rejected with `400` `application/problem+json` (the message is also exposed in the `error` extension member for backward compatibility) and nothing is stored or applied:

- `File.Path` must be a **relative** path: not rooted (`/x`, `\\x`, `C:\x`), no `..` segments (both `/` and `\` count as separators), and it must resolve **under the application content root** (`IHostEnvironment.ContentRootPath`, falling back to the current directory). Example: `logs/app/log-.ndjson`.
- `Redaction.AdditionalPatterns`: at most **20** patterns, each at most **512** characters, non-blank, and each must compile as a regular expression (validated with a 100 ms timeout).

### Error Responses

Backend failures return `500` `application/problem+json` with a **generic** `detail` (`"An unexpected error occurred."`). The exception message is never returned; the exception is logged server-side (category `ThisCloud.Framework.Loggings.Admin`).

### Audit Logging

Audit events are written by `SerilogAuditLogger` and are **not filtered by the global level switch** (a per-source-context minimum-level override is applied by `UseThisCloudFrameworkSerilog`), so `POST /disable` or `MinimumLevel=Critical` cannot silence the audit trail of the change itself.

Every change is logged via `IAuditLogger`:
```json
{
  "action": "UpdateSettings",
  "userId": "admin@example.com",
  "before": { "minimumLevel": "Information" },
  "after": { "minimumLevel": "Warning" }
}
```

**Rule**: NO secrets in `before`/`after` (use redaction if needed).

---

## 📚 Documentation

- [Loggings Framework (index)](../../README.en.md)
- [Abstractions Package (contracts)](../abstractions/README.en.md)
- [Serilog Package (implementation)](../serilog/README.en.md)
- [Enterprise Architecture](../../ARCHITECTURE.en.md)
- [Full Spanish documentation](README.es.md) ⬅️ **Detailed configuration, endpoints, troubleshooting**

---

## ⚠️ Disclaimer

**This software is provided "AS IS", without warranties. See [Full Disclaimer](../../../../README.md#exención-de-responsabilidad).**

- No liability for security breaches if Admin endpoints are exposed without protection.
- User responsibility: configure authentication/authorization, NO exposure in Production without strict gating.

---

## 📜 License

**ISC License** - See [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
