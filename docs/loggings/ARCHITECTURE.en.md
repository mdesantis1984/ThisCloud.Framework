# ThisCloud.Framework.Loggings Architecture

> 📘 **Language**: English | [Español (full architecture)](ARCHITECTURE.es.md)

## Overview

**ThisCloud.Framework.Loggings** is a structured logging framework based on **Serilog** with mandatory runtime administration, designed for .NET 10+ enterprise applications requiring:

- ✅ Structured logging (NDJSON) with minimal sinks (Console + File rolling 10MB)
- ✅ Automatic correlation (CorrelationId, RequestId, W3C TraceId)
- ✅ Mandatory redaction (secrets, JWT, PII)
- ✅ Runtime control without restart (enable/disable, change levels)
- ✅ Fail-fast Production (invalid config stops startup)
- ✅ Admin via endpoints (env + policy gated)

---

## 🏗️ Layers and Dependencies

```
┌────────────────────────────────────────────────────────────┐
│  HOST APPLICATION (.NET 10+)                               │
│  - ASP.NET Core Minimal API / Worker Service / Console    │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  ├─► ILoggingControlService (runtime control)
                  ├─► ICorrelationContext (CorrelationId/RequestId)
                  ├─► IAuditLogger (config changes)
                  │
┌─────────────────▼──────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Admin                        │
│  - Minimal API endpoints (GET/PUT/PATCH settings, etc.)    │
│  - Gating: Enabled + AllowedEnvironments + Policy         │
└─────────────────┬──────────────────────────────────────────┘
                  │ depends on
                  ▼
┌────────────────────────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Serilog                      │
│  - UseThisCloudFrameworkSerilog, AddThisCloudFrameworkLoggings│
│  - SerilogLoggingControlService (runtime reconfig)        │
│  - DefaultLogRedactor, ThisCloudContextEnricher, etc.     │
└─────────────────┬──────────────────────────────────────────┘
                  │ depends on
                  ▼
┌────────────────────────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Abstractions                 │
│  - LogLevel enum, LogSettings models, Interfaces          │
└────────────────────────────────────────────────────────────┘
```

**Dependency rules (Onion Architecture)**:
- ❌ `Abstractions` depends on nothing (no Serilog, no ASP.NET Core)
- ✅ `Serilog` depends only on `Abstractions` + Serilog packages
- ✅ `Admin` depends on `Abstractions` + ASP.NET Core Minimal APIs
- ✅ Host depends on `Serilog` (mandatory) + `Admin` (optional)

---

## ⚙️ Configuration Flow

### Startup

```
IConfiguration (appsettings.json)
       │
       ├─► Bind: "ThisCloud:Loggings" → LogSettings
       ▼
UseThisCloudFrameworkSerilog()
       │
       ├─► WriteTo.Console()
       ├─► WriteTo.File(path, 10MB rolling)
       ├─► MinimumLevel.Is(LogEventLevel)
       ├─► Enrich.With(ThisCloudContextEnricher)
       │
       └─► LoggingLevelSwitch (global)
                    │
                    ▼
           ILogger available in DI
```

### Runtime Reconfig

```
Admin API: PATCH /api/admin/logging/settings
       │
       ▼
ILoggingControlService.PatchSettingsAsync(partialSettings)
       │
       ├─► Merge with current LogSettings
       ├─► Validate limits (RollingFileSizeMb 1..100, etc.)
       │
       ▼
LoggingLevelSwitch.MinimumLevel = newLevel  ← CHANGES HOT
       │
       ├─► ✅ MinimumLevel (changes hot)
       ├─► ✅ Overrides per namespace (changes hot)
       │
       └─► ❌ Sinks (Console/File) NOT reconfigured hot
           ❌ File.Path NOT changed without restart
           ❌ File.RollingFileSizeMb NOT changed without restart
```

**Runtime reconfig limitations**:
- **Changes hot**: `MinimumLevel`, `Overrides` (namespace → level)
- **NO hot change**: Sinks enable/disable, `File.Path`, `File.RollingFileSizeMb`, `Retention.Days`

**Reason**: Serilog does not support dynamic sink reconfiguration post-bootstrap (upstream limitation).

---

## 🔗 Correlation (Traceability)

### Correlation Sources

```
HTTP Request (header X-Correlation-Id)
       │
       ├─► If exists and valid GUID → use
       └─► If NOT exists and GenerateIfMissing=true → generate new GUID
                │
                ▼
      HttpContext.Items["CorrelationId"]  ← if ASP.NET Core
                │
                ├─► ICorrelationContext.CorrelationId (DI Scoped)
                │
                ▼
      ThisCloudContextEnricher reads ICorrelationContext
                │
                ▼
      Log enriched property: "correlationId": "a1b2c3d4-..."
```

### Auto-Enriched Properties

Each log event includes:

```json
{
  "@t": "2026-02-15T14:30:00.123Z",
  "@m": "Hello world logged!",
  "@l": "Information",
  "service": "my-api",
  "env": "Production",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-1234567890ab",
  "requestId": "e5f6g7h8-i9j0-1234-abcd-0987654321fe",
  "traceId": "00-abc123def456...",
  "userId": "user@example.com",
  "sourceContext": "MyNamespace.MyClass"
}
```

**Exact keys** (defined in `ThisCloudLogKeys`):
- `service`, `env`, `correlationId`, `requestId`, `traceId`, `userId`, `sourceContext`

**Integration with `ThisCloud.Framework.Web`**:
- If host uses `ThisCloud.Framework.Web`, correlation middleware populates `HttpContext.Items["CorrelationId"]` and `HttpContext.Items["RequestId"]` automatically.
- **Result**: End-to-end correlation without additional config.

---

## 🛡️ Redaction (Sanitization)

### DefaultLogRedactor

**Responsibility**: Redact sensitive data in log messages BEFORE writing to sinks.

**Mandatory minimum patterns**:

| Pattern | Match | Replacement |
|---------|-------|-------------|
| `Authorization: Bearer <token>` | `Authorization: Bearer eyJ...` | `Authorization: Bearer [REDACTED]` |
| Standalone JWT | `eyJ...` (JWT start) | `[REDACTED_JWT]` |
| Secrets in key=value | `apiKey=abc123`, `password=secret` | `apiKey=[REDACTED]`, `password=[REDACTED]` |
| Secrets in key: value | `token: abc123` | `token: [REDACTED]` |
| Emails (best-effort) | `user@example.com` | `[REDACTED_PII]` |
| DNI/NIE Spain (best-effort) | `12345678Z`, `X1234567A` | `[REDACTED_PII]` |

**DefaultLogRedactor limits**:
- ✅ Redacts text in **log messages** (`message` string)
- ❌ Does NOT redact complex structured properties (if you log an object with `ILogger.LogInformation("{@Object}", obj)`, object fields are NOT redacted)
- ❌ Does NOT analyze binary content or file attachments

**Host responsibility**:
- DO NOT log objects with exposed secrets in `ToString()` or public properties.
- Use DTOs without secrets for structured logging.
- If custom redaction needed, implement `ILogRedactor` and register in DI.

---

## 🚨 Fail-fast Production

### ProductionValidator

**Purpose**: Prevent host from starting with invalid config in Production (early error detection).

**Validation rules (Production only)**:

| Condition | Validation | Action if fails |
|-----------|------------|-----------------|
| `File.Enabled=true` | `File.Path` must be configured and not empty | ❌ Throws `InvalidOperationException` |
| `File.Enabled=true` | `File.RollingFileSizeMb` ∈ [1..100] | ❌ Throws `InvalidOperationException` |
| `File.Enabled=true` | `File.RetainedFileCountLimit` ∈ [1..365] | ❌ Throws `InvalidOperationException` |
| `MinimumLevel` | Must be valid `LogLevel` enum value | ❌ Throws `InvalidOperationException` |
| `Admin.Enabled=true` | `AllowedEnvironments` must be explicit (not empty) | ❌ Throws `InvalidOperationException` |
| `Admin.Enabled=true` | `RequireAdmin` must be `true` | ❌ Throws `InvalidOperationException` |

**Fail-fast reason**:
- ❌ **NO silent fallback**: If Production starts with `File.Path=""`, logs are lost silently → diagnosis impossible.
- ✅ **Fail-fast**: Startup fails → early alert → fix before deploy → avoid log loss in production.

---

## 🔌 Extension Points

### 1. ILoggingSettingsStore (Config Persistence)

**Purpose**: Persist config changes made via Admin APIs.

**Default implementation**: In-memory (not persistent across restarts).

**How to extend**:

```csharp
public class SqlLoggingSettingsStore : ILoggingSettingsStore
{
    public string Version => "1.0-sql";
    
    public async Task<LogSettings?> GetAsync(CancellationToken ct = default)
    {
        // SELECT * FROM LoggingSettings WHERE Id = 'current'
    }
    
    public async Task SaveAsync(LogSettings settings, CancellationToken ct = default)
    {
        // INSERT/UPDATE LoggingSettings with timestamp
    }
}

// Register in DI (replaces InMemoryLoggingSettingsStore)
builder.Services.AddSingleton<ILoggingSettingsStore>(
    new SqlLoggingSettingsStore(connectionString));
```

---

### 2. ICorrelationContext (Custom Correlation Source)

**Purpose**: Customize where CorrelationId/RequestId/TraceId/UserId come from.

**Default implementation**: `HttpContextCorrelationContext` (reads from `HttpContext.Items` if ASP.NET Core).

**How to extend**:

```csharp
public class CustomHeaderCorrelationContext : ICorrelationContext
{
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-My-Correlation-Id"].FirstOrDefault();
    
    public string? RequestId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-My-Request-Id"].FirstOrDefault();
    
    // ... TraceId, UserId
}

// Register in DI (replaces HttpContextCorrelationContext)
builder.Services.AddScoped<ICorrelationContext, CustomHeaderCorrelationContext>();
```

---

### 3. IAuditLogger (Custom Audit)

**Purpose**: Log config changes (Admin APIs) with custom format.

**Default implementation**: `SerilogAuditLogger` (logs with `ILogger<SerilogAuditLogger>` at `Information` level).

**How to extend**:

```csharp
public class SqlAuditLogger : IAuditLogger
{
    public void LogChange(string action, string userId, object? before, object? after)
    {
        // INSERT INTO AuditLog (Action, UserId, BeforeJson, AfterJson, Timestamp)
        // Ensure NO secrets in before/after
    }
}

// Register in DI (replaces SerilogAuditLogger)
builder.Services.AddSingleton<IAuditLogger>(new SqlAuditLogger(connectionString));
```

**⚠️ CRITICAL RULE**: DO NOT log secrets in `before`/`after`. Redact before serializing if object contains passwords/tokens.

---

### 4. ILogRedactor (Custom Redaction)

See [Redaction](#-redaction-sanitization) section above.

---

## 🔒 Security

### Attack Surface

| Component | Surface | Protection |
|-----------|---------|------------|
| **Serilog sinks** | File system (logs written to disk) | File permissions (host responsible), retention (host responsible) |
| **Admin endpoints** | HTTP (GET/PUT/PATCH/POST/DELETE) | Gating: `Enabled=false` default + `AllowedEnvironments` + `RequireAdmin=true` + Policy |
| **Logs on disk** | Physical/network file system access | Encryption at rest (host responsible), log rotation (10MB + day), retention cleanup (host responsible) |

### Security Boundaries

**✅ What framework does**:
- Redacts secrets in messages (Authorization/JWT/apiKey/etc.)
- Fail-fast if invalid config in Production
- Admin endpoints disabled by default
- Policy enforcement if `RequireAdmin=true`

**❌ What framework does NOT do** (host responsibility):
- ❌ **NO body logging**: Request/response payloads NOT logged (prohibited by design)
- ❌ **NO log encryption**: If encrypted logs needed on disk, use file system encryption (BitLocker, LUKS, etc.)
- ❌ **NO authentication/authorization**: Host MUST configure `AddAuthentication()` + `AddAuthorization()` + policy `"Admin"`
- ❌ **NO automatic old log cleanup**: Host MUST implement job/cron to purge logs > `Retention.Days`
- ❌ **NO SIEM/centralization**: Host MUST configure shippers (Logstash, Fluentd, etc.) if centralized logs needed

### Mandatory Security Checklist

See [CHECKLIST.en.md](CHECKLIST.en.md) for complete safe consumption checklist.

---

## 📚 References

- [Repository root README (monorepo index)](../../README.md)
- [Abstractions Package (contracts)](packages/abstractions/README.en.md)
- [Serilog Package (implementation)](packages/serilog/README.en.md)
- [Admin Package (endpoints)](packages/admin/README.en.md)
- [Safe consumption checklist](CHECKLIST.en.md)
- **[Full Spanish architecture documentation](ARCHITECTURE.es.md)** ⬅️ **Detailed flows, diagrams, extension examples**

---

## ⚠️ Disclaimer

**This software is provided "AS IS", without warranties. See [Full Disclaimer](../../README.md#exención-de-responsabilidad).**

- No fitness warranties, no liability for data loss, security breaches, regulatory sanctions.
- User responsibility: validate config, manage retention/cleanup, comply with regulations.

---

## 📜 License

**ISC License** - See [LICENSE](../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
