# ThisCloud.Framework.Loggings — Documentation Index

> **Framework Version**: 1.1  
> **Target**: .NET 10+  
> **License**: ISC

## Overview

ThisCloud.Framework.Loggings provides enterprise-grade structured logging built on Serilog with:
- Mandatory correlation (CorrelationId, RequestId, W3C TraceContext)
- Runtime administration via HTTP endpoints
- Sanitization/redaction for secrets and PII
- Production fail-fast validation
- SQL Server persistence design (documentation-only in v1.1; implementation in v1.2+)

**Important**: This framework ships **documentation and contracts only** for persistence. Host applications own migrations and database provisioning.

---

## 📚 Documentation

### Architecture & Design
- **[Architecture (English)](ARCHITECTURE.en.md)** — Layers, flows, correlation, redaction, fail-fast patterns
- **[Architecture (Español)](ARCHITECTURE.es.md)** — Arquitectura, flujos, correlación, redacción, patrones fail-fast

### Operational Checklists
- **[Checklist (English)](CHECKLIST.en.md)** — Security, production, admin, operations, support, compliance
- **[Checklist (Español)](CHECKLIST.es.md)** — Seguridad, producción, admin, operaciones, soporte, cumplimiento

### Phase Reports
- **[Informe Ejecutivo L5](INFORME_EJECUTIVO_L5.md)** — Sample integration validation, endpoint testing, production readiness

---

## 📦 Packages

### ThisCloud.Framework.Loggings.Abstractions
Public contracts: `LogLevel`, `LoggingSettings`, `ILoggingControlService`, `ICorrelationContext`.

- **[README (English)](packages/abstractions/README.en.md)**
- **[README (Español)](packages/abstractions/README.es.md)**

**NuGet**: `dotnet add package ThisCloud.Framework.Loggings.Abstractions`

### ThisCloud.Framework.Loggings.Serilog
Serilog implementation: bootstrap, enrichment, redaction, audit, runtime control, Console/File sinks (10MB rolling NDJSON).

- **[README (English)](packages/serilog/README.en.md)**
- **[README (Español)](packages/serilog/README.es.md)**

**NuGet**: `dotnet add package ThisCloud.Framework.Loggings.Serilog`

### ThisCloud.Framework.Loggings.Admin
Minimal API endpoints for runtime administration: Enable/Disable, GET/PUT/PATCH settings, Reset, policy-gated.

- **[README (English)](packages/admin/README.en.md)**
- **[README (Español)](packages/admin/README.es.md)**

**NuGet**: `dotnet add package ThisCloud.Framework.Loggings.Admin`

---

## 🗄️ SQL Server Persistence (v1.1 Design / v1.2 Implementation)

### Schema Documentation
- **[schema_v1.sql](sqlserver/schema_v1.sql)** — SQL Server DDL for logging persistence

**Tables**:
- `dbo.tc_loggings_settings` — Current configuration (singleton pattern, RowVersion concurrency)
- `dbo.tc_loggings_settings_history` — Audit trail (before/after snapshots, who/when/what/why)
- `dbo.tc_loggings_events` — Event storage (v1.2 prepared schema, time-series optimized)

**Target**: SQL Server 2019+ / Azure SQL Database

### ⚠️ Migration Ownership

**The framework does NOT execute migrations.**

- `schema_v1.sql` is a **documentation artifact** and **design contract** for v1.1.
- Host applications **own migrations** (via EF Core Migrations, Fluent Migrator, DbUp, etc.).
- Persistence implementation (read/write to DB) ships in **v1.2+**.
- v1.1 includes in-memory storage only (default: `InMemoryLoggingSettingsStore`).

**Recommended approach**:
1. Review `sqlserver/schema_v1.sql` for table structures and indexes.
2. Adapt DDL to your migration tool syntax.
3. Apply schema via your existing migration pipeline.
4. Configure connection strings and persistence layer when v1.2 is available.

---

## 🔒 Security Notes

### Redaction Boundaries
- **Sanitization happens at logging call sites** (application layer), NOT at persistence layer.
- Framework enforces redaction for: `Authorization`, `Cookie`, `X-API-Key`, JWT tokens, passwords.
- **Do NOT log sensitive payloads** (request/response bodies, PII, secrets).

### Schema Security
- `schema_v1.sql` contains **no secrets or PII**.
- Tables store configuration metadata and audit trails only.
- Event properties are stored as JSON; **host must validate redaction is applied before logging**.

### Production Hardening
- Use **TDE (Transparent Data Encryption)** for SQL Server databases.
- Use **encrypted connections** (SSL/TLS) for database access.
- Implement **retention policies** for `tc_loggings_settings_history` and `tc_loggings_events`.
- Gate Admin endpoints with **authorization policies** (e.g., `[Authorize(Policy = "Admin")]`).
- Restrict Admin to **Development/Staging** via `AllowedEnvironments` configuration.

---

## 🚀 Quick Start

1. **Install packages**:
   ```bash
   dotnet add package ThisCloud.Framework.Loggings.Serilog
   dotnet add package ThisCloud.Framework.Loggings.Admin
   ```

2. **Configure in `Program.cs`**:
   ```csharp
   builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration);
   builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration);
   app.MapThisCloudFrameworkLoggingsAdmin();
   ```

3. **Add `appsettings.json`**:
   ```json
   {
     "ThisCloud": {
       "Framework": {
         "Loggings": {
           "IsEnabled": true,
           "MinimumLevel": "Information"
         }
       }
     }
   }
   ```

4. **See sample**: `samples/ThisCloud.Sample.Loggings.MinimalApi/` for full integration.

---

## 📖 Additional Resources

- **Main Plan**: [Plan_ThisCloud.Framework.Loggings_v1.1.md](../Plan_ThisCloud.Framework.Loggings_v1.1.md)
- **Sample Application**: `samples/ThisCloud.Sample.Loggings.MinimalApi/README.md`
- **License**: [LICENSE](../../LICENSE) (ISC)
- **GitHub**: [ThisCloud.Framework](https://github.com/mdesantis1984/ThisCloud.Framework)

---

## 🌍 Language / Idioma

This README is in **English**. For Spanish documentation, see:
- [Arquitectura (ES)](ARCHITECTURE.es.md)
- [Checklist (ES)](CHECKLIST.es.md)
- [READMEs de paquetes en español](packages/)

Para documentación en español, consulte los archivos `.es.md` en las carpetas correspondientes.

---

**Last Updated**: 2026-02-15  
**Status**: Phase 6 (L6.2) — Documentation Index Completed
