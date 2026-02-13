# ThisCloud.Framework.Loggings.Abstractions

> 📘 **Language**: English | [Español](README.es.md)

## Purpose

Core contracts package for the **ThisCloud.Framework.Loggings** framework. Provides implementation-independent interfaces, models and enums (no dependencies on Serilog or ASP.NET Core).

**Target audience**: Consumers who need to define logging contracts without coupling to a specific implementation.

---

## 📦 Installation

```bash
dotnet add package ThisCloud.Framework.Loggings.Abstractions
```

**Minimum version**: .NET 10

---

## 🎯 Public API

### Enum `LogLevel`

```csharp
namespace ThisCloud.Framework.Loggings.Abstractions;

public enum LogLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}
```

**Mandatory canon** for the entire framework. Compatible with Serilog levels.

---

### Model `LogSettings`

Complete logging configuration with safe defaults:

```csharp
public class LogSettings
{
    public bool IsEnabled { get; set; } = true;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public IReadOnlyDictionary<string, LogLevel> Overrides { get; set; }
    public ConsoleSinkSettings Console { get; set; }
    public FileSinkSettings File { get; set; }
    public RetentionSettings Retention { get; set; }
    public RedactionSettings Redaction { get; set; }
    public CorrelationSettings Correlation { get; set; }
}
```

**Defaults**:
- `File.RollingFileSizeMb = 10` (rolling by size **10 MB**)
- `File.RetainedFileCountLimit = 30`
- `Redaction.Enabled = true` (redaction active by default)

---

### Core Interfaces

#### `ILoggingControlService`

Runtime logging control (enable/disable, change settings):

```csharp
public interface ILoggingControlService
{
    Task EnableAsync(CancellationToken ct = default);
    Task DisableAsync(CancellationToken ct = default);
    Task<LogSettings> GetSettingsAsync(CancellationToken ct = default);
    Task UpdateSettingsAsync(LogSettings settings, CancellationToken ct = default);
    Task PatchSettingsAsync(object partialSettings, CancellationToken ct = default);
}
```

#### `ILoggingSettingsStore`

Configuration persistence (optional implementation by host):

```csharp
public interface ILoggingSettingsStore
{
    Task<LogSettings?> GetAsync(CancellationToken ct = default);
    Task SaveAsync(LogSettings settings, CancellationToken ct = default);
    string Version { get; }
}
```

#### `ILogRedactor`

Sensitive data redaction in logs:

```csharp
public interface ILogRedactor
{
    string Redact(string input);
}
```

**Mandatory minimum patterns** (see `DefaultLogRedactor` in `Serilog` package):
- `Authorization: Bearer <token>` → `Authorization: Bearer [REDACTED]`
- JWT `eyJ...` → `[REDACTED_JWT]`
- `apiKey|token|secret|password` in `key=value` → `[REDACTED]`

#### `ICorrelationContext`

Correlation context (GUID) for traceability:

```csharp
public interface ICorrelationContext
{
    string? CorrelationId { get; }
    string? RequestId { get; }
    string? TraceId { get; }
    string? UserId { get; }
}
```

**Integration with `ThisCloud.Framework.Web`**: If the host uses the Web framework's correlation middleware, `CorrelationId` and `RequestId` are automatically reused.

#### `IAuditLogger`

Structured audit logging (configuration changes, admin actions):

```csharp
public interface IAuditLogger
{
    void LogChange(string action, string userId, object? before, object? after);
}
```

**Rules**:
- DO NOT log secrets in `before`/`after` (use redaction if needed)
- `userId` can be `"system"` if the change is automatic

---

## 🔧 Configuration

This package **DOES NOT require configuration** (contracts only). Actual configuration is done in the `Serilog` or `Admin` package.

---

## 🛡️ Security

- ✅ **No external dependencies** (.NET 10 BCL only)
- ✅ **Public interfaces** for extension (DI-friendly)
- ❌ **NO redaction implementation** (responsibility of `Serilog` package)

---

## 📚 Related Documentation

- [Loggings Framework (index)](../../README.en.md)
- [Serilog Package (implementation)](../serilog/README.en.md)
- [Admin Package (runtime endpoints)](../admin/README.en.md)
- [Enterprise-grade Architecture](../../ARCHITECTURE.en.md)
- [Repository root README](../../../../README.md)

---

## ⚠️ Disclaimer

**This software is provided "AS IS", without warranties. See [Full Disclaimer](../../../../README.md#exención-de-responsabilidad) for detailed terms.**

- No fitness warranties, no liability for damages/losses, use at your own risk.

---

## 📜 License

**ISC License** - See [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
