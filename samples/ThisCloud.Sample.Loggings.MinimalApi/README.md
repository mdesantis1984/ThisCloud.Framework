# ThisCloud.Sample.Loggings.MinimalApi

**Sample Minimal API** demonstrating integration with **ThisCloud.Framework.Loggings** (Serilog-based structured logging) with Admin endpoints, environment-based gating, and policy enforcement.

---

## 🎯 Purpose

This sample shows how to:
- ✅ Integrate `ThisCloud.Framework.Loggings.*` in a Minimal API (`.NET 10`).
- ✅ Enable structured logging with **Serilog** (Console + File sinks, 10MB rolling).
- ✅ Enable **Admin endpoints** (`/api/admin/logging/*`) with **environment gating** and **policy enforcement**.
- ✅ Configure **correlation** (CorrelationId, RequestId, TraceId).
- ✅ Apply **redaction** (no secrets/PII in logs).
- ✅ Follow **Production checklist** (Admin disabled, Swagger disabled, fail-fast).

---

## 🚀 Quickstart (<15 min)

### Prerequisites
- **.NET 10 SDK** installed.
- Access to the **ThisCloud.Framework** solution (this repo).

### Step 1: Clone and navigate
```bash
cd F:\Proyectos\ThisCloudServices\03-Repo\ThisCloud.Framework
cd samples/ThisCloud.Sample.Loggings.MinimalApi
```

### Step 2: Set Admin API Key (required for Admin endpoints)
**⚠️ NEVER version API keys in appsettings files.**

**Option A: Environment variable (recommended)**
```bash
# Windows (PowerShell)
$env:SAMPLE_ADMIN_APIKEY="your-local-dev-key-here"

# Windows (CMD)
set SAMPLE_ADMIN_APIKEY=your-local-dev-key-here

# Linux/macOS
export SAMPLE_ADMIN_APIKEY="your-local-dev-key-here"
```

**Option B: User secrets (local dev only)**
```bash
dotnet user-secrets init
dotnet user-secrets set "SAMPLE_ADMIN_APIKEY" "your-local-dev-key-here"
```

> 💡 Replace `"your-local-dev-key-here"` with your own random string (example only, not for production).

### Step 3: Run in Development
```bash
dotnet run --environment=Development
```

Expected output:
- Console logs enabled.
- File logs written to `logs/log-.ndjson` (10MB rolling).
- Swagger UI available at `https://localhost:<port>/swagger` (or `/openapi/v1.json`).
- Admin endpoints available at `/api/admin/logging/*` (requires header `X-Admin-ApiKey` with the value you set above).

### Step 4: Test Health endpoint
```bash
curl -k https://localhost:7001/health
```

Response:
```json
{
  "status": "healthy",
  "service": "ThisCloud.Sample.Loggings.MinimalApi",
  "timestamp": "2026-02-15T12:00:00Z"
}
```

### Step 5: Test Admin endpoint (GET settings)
```bash
# Use your API key from Step 2
curl -k https://localhost:7001/api/admin/logging \
  -H "X-Admin-ApiKey: your-local-dev-key-here"
```

Response: current `LogSettings` JSON.

### Step 6: Test Admin endpoint (PATCH level)
```bash
curl -k https://localhost:7001/api/admin/logging \
  -X PATCH \
  -H "X-Admin-ApiKey: your-local-dev-key-here" \
  -H "Content-Type: application/json" \
  -d '{"minimumLevel":"Debug"}'
```

Verify in logs: level changed to `Debug` at runtime.

---

## 📁 Files Structure

```
samples/ThisCloud.Sample.Loggings.MinimalApi/
├── Program.cs                          # Main entry point (Minimal API setup)
├── appsettings.json                    # Base config (Admin disabled, File enabled)
├── appsettings.Development.json        # Dev overrides (Console+File+Admin enabled)
├── appsettings.Production.json         # Prod config (Admin disabled, Console disabled)
├── README.md                           # This file
└── RUNBOOK.md                          # Operational validation runbook
```

---

## 🔒 Security Notes

### NO SECRETS IN REPO
- 🚫 **NEVER commit API keys or secrets to appsettings files** (even in Development).
- ✅ **Use environment variables** (`SAMPLE_ADMIN_APIKEY`) or **user secrets** for local dev.
- ✅ **Production**: Use **Azure Key Vault**, **AWS Secrets Manager**, or similar secure storage.
- ⚠️ **Rotate keys regularly** and never share them in documentation, logs, or chat.

### How to set SAMPLE_ADMIN_APIKEY securely
**Local Development**:
```bash
# Environment variable (session-scoped, NOT persisted)
export SAMPLE_ADMIN_APIKEY="my-random-dev-key-12345"  # Example only

# User secrets (local machine only, NOT in repo)
dotnet user-secrets set "SAMPLE_ADMIN_APIKEY" "my-random-dev-key-12345"
```

**Production**:
- Use **Azure Key Vault** or equivalent.
- Inject secrets at runtime (never hardcode or version).
- See [Production Checklist](#-production-checklist-sample-specific) below.

### Admin Endpoints Gating
- By default: `Admin.Enabled=false` in Production.
- If enabled in Production: **MUST** set `RequireAdmin=true` + `AllowedEnvironments` explicitly.
- Policy enforcement: requests without valid `X-Admin-ApiKey` header → **401/403**.

### Swagger in Production
- **Disabled by default** (`isDevelopment` check in `Program.cs`).
- Never expose Swagger in Production (security risk).

---

## 📚 Documentation Links

### Framework Loggings Docs (Track B)
- **Architecture**: [docs/loggings/ARCHITECTURE.es.md](../../docs/loggings/ARCHITECTURE.es.md) (ES) | [docs/loggings/ARCHITECTURE.en.md](../../docs/loggings/ARCHITECTURE.en.md) (EN)
- **Abstractions**: [docs/loggings/abstractions/README.es.md](../../docs/loggings/abstractions/README.es.md) (ES) | [docs/loggings/abstractions/README.en.md](../../docs/loggings/abstractions/README.en.md) (EN)
- **Serilog**: [docs/loggings/serilog/README.es.md](../../docs/loggings/serilog/README.es.md) (ES) | [docs/loggings/serilog/README.en.md](../../docs/loggings/serilog/README.en.md) (EN)
- **Admin**: [docs/loggings/admin/README.es.md](../../docs/loggings/admin/README.es.md) (ES) | [docs/loggings/admin/README.en.md](../../docs/loggings/admin/README.en.md) (EN)
- **Checklist**: [docs/loggings/CHECKLIST.es.md](../../docs/loggings/CHECKLIST.es.md) (ES) | [docs/loggings/CHECKLIST.en.md](../../docs/loggings/CHECKLIST.en.md) (EN)

---

## ✅ Production Checklist (Sample-Specific)

Before deploying to Production:

- [ ] `Admin.Enabled=false` (or explicit `AllowedEnvironments` + `RequireAdmin=true`).
- [ ] `Console.Enabled=false` (reduce noise, use file-based logging).
- [ ] `File.Path` points to a valid, writable location (e.g., `/var/log/thiscloud-sample/`).
- [ ] `Redaction.Enabled=true` (no secrets/PII in logs).
- [ ] Swagger/OpenAPI **disabled** (remove `AddOpenApi` / `MapOpenApi`).
- [ ] API Key for Admin endpoints comes from **environment variable** (not hardcoded).
- [ ] Verify log rotation (10MB default, adjust `RollingFileSizeMb` if needed).
- [ ] Monitor disk usage for `RetainedFileCountLimit` (default 30 files).

See full checklist: [docs/loggings/CHECKLIST.es.md](../../docs/loggings/CHECKLIST.es.md)

---

## 🔧 Configuration Reference

### Minimal Integration (Copy/Paste)

```csharp
// Program.cs
using ThisCloud.Framework.Loggings.Admin;
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

const string ServiceName = "YourServiceName";

// Configure logging
builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, ServiceName);
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, ServiceName);

var app = builder.Build();

// Map Admin endpoints (optional, gated by config)
app.MapThisCloudFrameworkLoggingsAdmin(app.Configuration);

app.Run();
```

### appsettings.json (Minimal)

```json
{
  "ThisCloud": {
    "Loggings": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMb": 10
      },
      "Admin": {
        "Enabled": false
      }
    }
  }
}
```

---

## 🛠️ Operational Runbook

See [RUNBOOK.md](./RUNBOOK.md) for:
- How to verify logging is working.
- How to check file rotation (10MB).
- How to test Admin endpoints with `curl`.
- How to validate correlationId in logs.
- How to verify redaction (JWT/secrets).

---

## 📖 License

This sample is part of **ThisCloud.Framework** and follows the **ISC License**. See [LICENSE](../../LICENSE).

---

## 🆘 Support

This sample is provided **AS IS** with **no warranties** or **SLA**. For questions or issues:
- Check [docs/loggings/](../../docs/loggings/) for detailed documentation.
- Review [CHECKLIST.es.md](../../docs/loggings/CHECKLIST.es.md) for security/compliance guidelines.
- Open an issue in the repo (if applicable).

**No official support is implied or guaranteed.**
