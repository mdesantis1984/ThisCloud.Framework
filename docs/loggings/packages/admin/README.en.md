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

### Automatic Gating

Endpoints **NOT exposed** if:
1. `Admin.Enabled=false` (default)
2. Current environment NOT in `AllowedEnvironments`

### Policy Protection

If `RequireAdmin=true` (default), endpoints require:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
```

### Audit Logging

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
