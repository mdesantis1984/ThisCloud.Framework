# ThisCloud.Framework.Loggings.Serilog

> 📘 **Language**: English | [Español (full documentation)](README.es.md)

## Purpose

**Serilog** implementation of the **ThisCloud.Framework.Loggings** framework. Provides:

- ✅ Minimal sinks: **Console** + **File** (rolling by size **10 MB**)
- ✅ Standard enrichment (service, env, correlationId, requestId, traceId, userId)
- ✅ Mandatory redaction (secrets, JWT, PII)
- ✅ Runtime control (enable/disable, change levels dynamically)
- ✅ Fail-fast Production (invalid config stops startup)
- ✅ Structured audit logging of changes

---

## 📦 Installation

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

**Minimum version**: .NET 10

---

## ⚡ Quick Start

```csharp
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, serviceName: "my-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, serviceName: "my-api");

var app = builder.Build();
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Hello world logged!");
    return Results.Ok(new { Message = "OK" });
});
app.Run();
```

### appsettings.json

```json
{
  "ThisCloud": {
    "Loggings": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
      "Console": { "Enabled": true },
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMb": 10,
        "UseCompactJson": true
      },
      "Redaction": { "Enabled": true }
    }
  }
}
```

### Production Config

```json
{
  "ThisCloud": {
    "Loggings": {
      "MinimumLevel": "Warning",
      "Console": { "Enabled": false },
      "File": { "Path": "/var/log/myapp/log-.ndjson" }
    }
  }
}
```

---

## 🛡️ Security

- ❌ **NO logging secrets**: `Authorization`, JWT, passwords, API keys, PII without redaction
- ❌ **NO body logging** (request/response payloads): prohibited by default
- ✅ **Redaction.Enabled=true** mandatory in Production
- ✅ **Fail-fast Production**: Invalid config stops startup (no silent fallback)

---

## 📋 Production Checklist

- ✅ `Console.Enabled=false` (performance + security)
- ✅ `File.Path` absolute with correct permissions
- ✅ `MinimumLevel="Warning"` or higher
- ✅ `Redaction.Enabled=true`
- ✅ Retention cleanup implemented (host responsibility)

---

## 📚 Documentation

- [Loggings Framework (index)](../../README.en.md)
- [Abstractions Package (contracts)](../abstractions/README.en.md)
- [Admin Package (runtime endpoints)](../admin/README.en.md)
- [Enterprise Architecture](../../ARCHITECTURE.en.md)
- [Full Spanish documentation](README.es.md) ⬅️ **Detailed configuration, troubleshooting, API**

---

## ⚠️ Disclaimer

**This software is provided "AS IS", without warranties. See [Full Disclaimer](../../../../README.md#exención-de-responsabilidad).**

- No fitness warranties, no liability for data loss, security breaches, regulatory sanctions.
- User responsibility: validate config, manage retention, comply with regulations.

---

## 📜 License

**ISC License** - See [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
