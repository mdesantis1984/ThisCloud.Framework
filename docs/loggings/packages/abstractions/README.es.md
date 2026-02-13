# ThisCloud.Framework.Loggings.Abstractions

> 📘 **Idioma**: Español | [English](README.en.md)

## Propósito

Paquete de contratos core para el framework de logging **ThisCloud.Framework.Loggings**. Proporciona interfaces, modelos y enums independientes de implementación (sin dependencias de Serilog ni ASP.NET Core).

**Público objetivo**: Consumidores que necesitan definir contratos de logging sin acoplarse a una implementación específica.

---

## 📦 Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Abstractions
```

**Versión mínima**: .NET 10

---

## 🎯 API Pública

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

**Canon mandatorio** para todo el framework. Compatible con niveles Serilog.

---

### Modelo `LogSettings`

Configuración completa de logging con valores por defecto seguros:

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
- `File.RollingFileSizeMb = 10` (rolling por tamaño **10 MB**)
- `File.RetainedFileCountLimit = 30`
- `Redaction.Enabled = true` (redaction activa por defecto)

---

### Interfaces Core

#### `ILoggingControlService`

Control runtime de logging (enable/disable, cambiar settings):

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

Persistencia de configuración (implementación opcional por el host):

```csharp
public interface ILoggingSettingsStore
{
    Task<LogSettings?> GetAsync(CancellationToken ct = default);
    Task SaveAsync(LogSettings settings, CancellationToken ct = default);
    string Version { get; }
}
```

#### `ILogRedactor`

Redaction de datos sensibles en logs:

```csharp
public interface ILogRedactor
{
    string Redact(string input);
}
```

**Patrones mínimos mandatorios** (ver `DefaultLogRedactor` en `Serilog` package):
- `Authorization: Bearer <token>` → `Authorization: Bearer [REDACTED]`
- JWT `eyJ...` → `[REDACTED_JWT]`
- `apiKey|token|secret|password` en `key=value` → `[REDACTED]`

#### `ICorrelationContext`

Contexto de correlación (GUID) para trazabilidad:

```csharp
public interface ICorrelationContext
{
    string? CorrelationId { get; }
    string? RequestId { get; }
    string? TraceId { get; }
    string? UserId { get; }
}
```

**Integración con `ThisCloud.Framework.Web`**: Si el host usa el middleware de correlación de Web framework, `CorrelationId` y `RequestId` se reutilizan automáticamente.

#### `IAuditLogger`

Logging de auditoría estructurada (cambios de configuración, admin actions):

```csharp
public interface IAuditLogger
{
    void LogChange(string action, string userId, object? before, object? after);
}
```

**Reglas**:
- NO loguear secretos en `before`/`after` (usar redaction si hace falta)
- `userId` puede ser `"system"` si el cambio es automático

---

## 🔧 Configuración

Este paquete **NO requiere configuración** (solo contratos). La configuración real se hace en el paquete `Serilog` o `Admin`.

---

## 🛡️ Seguridad

- ✅ **Sin dependencias externas** (solo .NET 10 BCL)
- ✅ **Interfaces públicas** para extensión (DI-friendly)
- ❌ **NO incluye implementación** de redaction (responsabilidad del paquete `Serilog`)

---

## 📚 Documentación Relacionada

- [Framework Loggings (índice)](../../README.es.md)
- [Paquete Serilog (implementación)](../serilog/README.es.md)
- [Paquete Admin (endpoints runtime)](../admin/README.es.md)
- [Arquitectura enterprise-grade](../../ARCHITECTURE.es.md)
- [README raíz del repo](../../../../README.md)

---

## ⚠️ Disclaimer

**Este software se proporciona "TAL CUAL", sin garantías. Ver [Disclaimer completo](../../../../README.md#exención-de-responsabilidad) para términos detallados.**

- Sin garantías de idoneidad, sin responsabilidad por daños/pérdidas, uso bajo responsabilidad del usuario.

---

## 📜 Licencia

**ISC License** - Ver [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
