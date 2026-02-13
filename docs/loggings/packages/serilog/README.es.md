# ThisCloud.Framework.Loggings.Serilog

> 📘 **Idioma**: Español | [English](README.en.md)

## Propósito

Implementación **Serilog** del framework de logging **ThisCloud.Framework.Loggings**. Proporciona:

- ✅ Sinks mínimos: **Console** + **File** (rolling por tamaño **10 MB**)
- ✅ Enrichment estándar (service, env, correlationId, requestId, traceId, userId)
- ✅ Redaction obligatoria (secretos, JWT, PII)
- ✅ Control runtime (enable/disable, cambiar niveles dinámicamente)
- ✅ Fail-fast Production (config inválida detiene arranque)
- ✅ Auditoría estructurada de cambios

---

## 📦 Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

**Dependencias**:
- `ThisCloud.Framework.Loggings.Abstractions`
- Serilog 4.3.1 + sinks (Console 6.1.1, File 7.0.0)

**Versión mínima**: .NET 10

---

## ⚡ Inicio Rápido

### Program.cs

```csharp
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Registrar Serilog como logger del host
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    serviceName: "mi-api");

// Registrar servicios de control runtime + redaction + correlation
builder.Services.AddThisCloudFrameworkLoggings(
    builder.Configuration,
    serviceName: "mi-api");

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
      "Overrides": {
        "Microsoft": "Warning",
        "System": "Warning"
      },
      "Console": {
        "Enabled": true
      },
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMb": 10,
        "RetainedFileCountLimit": 30,
        "UseCompactJson": true
      },
      "Retention": {
        "Days": 30
      },
      "Redaction": {
        "Enabled": true
      },
      "Correlation": {
        "HeaderName": "X-Correlation-Id",
        "GenerateIfMissing": true
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
      "MinimumLevel": "Warning",
      "Console": {
        "Enabled": false
      },
      "File": {
        "Path": "/var/log/myapp/log-.ndjson",
        "RollingFileSizeMb": 10
      }
    }
  }
}
```

---

## 🔧 Configuración Detallada

### Root: `ThisCloud:Loggings`

| Key | Tipo | Default | Regla Production |
|-----|------|---------|-----------------|
| `IsEnabled` | `bool` | `true` | — |
| `MinimumLevel` | `string` | `"Information"` | `"Warning"` recomendado |
| `Overrides` | `Dictionary<string,string>` | `{}` | Namespace → nivel (ej: `"Microsoft": "Warning"`) |

### Console Sink

| Key | Tipo | Default | Regla Production |
|-----|------|---------|-----------------|
| `Console.Enabled` | `bool` | `true` | **DEBE SER `false`** (performance + seguridad) |

### File Sink (Rolling 10 MB)

| Key | Tipo | Default | Límites | Regla Production |
|-----|------|---------|---------|-----------------|
| `File.Enabled` | `bool` | `true` | — | — |
| `File.Path` | `string` | `"logs/log-.ndjson"` | — | Path absoluto + permisos correctos |
| `File.RollingFileSizeMb` | `int` | `10` | **1..100** | 10 MB recomendado |
| `File.RetainedFileCountLimit` | `int` | `30` | **1..365** | — |
| `File.UseCompactJson` | `bool` | `true` | — | NDJSON (JSON Lines) |

**Rotación**: Cuando el archivo alcanza `RollingFileSizeMb`, se crea uno nuevo con sufijo timestamp.

### Retention

| Key | Tipo | Default | Límites |
|-----|------|---------|---------|
| `Retention.Days` | `int` | `30` | **1..3650** |

**Responsabilidad del host**: Este valor es lógico (TTL); el host debe implementar limpieza de archivos antiguos (job/cron).

### Redaction

| Key | Tipo | Default | Regla Production |
|-----|------|---------|-----------------|
| `Redaction.Enabled` | `bool` | `true` | **MANDATORIO `true`** |
| `Redaction.AdditionalPatterns` | `string[]` | `[]` | Patrones regex custom (opcional) |

**Patrones mínimos** (siempre activos si `Enabled=true`):
- `Authorization: Bearer <token>` → `[REDACTED]`
- JWT `eyJ...` → `[REDACTED_JWT]`
- `apiKey|token|secret|password` en `key=value` → `[REDACTED]`
- Emails (best-effort) → `[REDACTED_PII]`
- DNI/NIE (España, best-effort) → `[REDACTED_PII]`

### Correlation

| Key | Tipo | Default |
|-----|------|---------|
| `Correlation.HeaderName` | `string` | `"X-Correlation-Id"` |
| `Correlation.GenerateIfMissing` | `bool` | `true` |

**Integración con `ThisCloud.Framework.Web`**: Si el host usa el middleware de correlación de Web, los valores se reutilizan automáticamente.

---

## 🎯 API Pública

### `HostBuilderExtensions`

```csharp
public static IHostBuilder UseThisCloudFrameworkSerilog(
    this IHostBuilder host,
    IConfiguration configuration,
    string serviceName);
```

Registra Serilog como logger del host con sinks Console + File.

### `ServiceCollectionExtensions`

```csharp
public static IServiceCollection AddThisCloudFrameworkLoggings(
    this IServiceCollection services,
    IConfiguration configuration,
    string serviceName);
```

Registra:
- `ILoggingControlService` → `SerilogLoggingControlService` (Singleton)
- `ILogRedactor` → `DefaultLogRedactor` (Singleton)
- `ICorrelationContext` → `HttpContextCorrelationContext` (Scoped, si ASP.NET Core)
- `IAuditLogger` → `SerilogAuditLogger` (Singleton)

### Enrichment (Automático)

Propiedades enriquecidas en **todos** los logs:

```json
{
  "service": "mi-api",
  "env": "Production",
  "correlationId": "a1b2c3d4-...",
  "requestId": "e5f6g7h8-...",
  "traceId": "00-abc123...",
  "userId": "user@example.com",
  "sourceContext": "MyNamespace.MyClass"
}
```

**Keys exactas**: Ver `ThisCloudLogKeys` en `Abstractions`.

---

## 🛡️ Seguridad

### ❌ Prohibiciones (MANDATORIAS)

1. **NO loguear secretos**: `Authorization`, JWT completo, passwords, API keys, PII sin redaction
2. **NO body logging**: Request/response payloads crudos (prohibido por defecto)
3. **Redaction.Enabled=true** obligatorio en Production

### Fail-fast Production

Si la configuración es inválida en Production (ej: `RollingFileSizeMb=0`, path inexistente sin permisos), el arranque **falla inmediatamente** (no silent fallback).

**Validación**: `ProductionValidator` (automático en `UseThisCloudFrameworkSerilog`).

---

## 📋 Troubleshooting

### Problema: No se generan archivos de log

1. ✅ Verificar `File.Enabled=true` y `IsEnabled=true`
2. ✅ Verificar path absoluto y permisos de escritura
3. ✅ Verificar que el directorio existe (Serilog NO crea directorios padre)

```bash
# Linux/Mac
mkdir -p /var/log/myapp
chmod 755 /var/log/myapp

# Windows
New-Item -ItemType Directory -Path C:\logs\myapp -Force
```

### Problema: Console sink visible en Production

✅ Configurar `Console.Enabled=false` en `appsettings.Production.json`.

### Problema: Logs contienen secretos

✅ Verificar `Redaction.Enabled=true`.  
⚠️ Si usas `ILogger` para loguear objetos complejos, asegurar que los modelos **NO exponen secretos** en `ToString()`.

---

## 📚 Documentación Relacionada

- [Framework Loggings (índice)](../../README.es.md)
- [Paquete Abstractions (contratos)](../abstractions/README.es.md)
- [Paquete Admin (endpoints runtime)](../admin/README.es.md)
- [Arquitectura enterprise-grade](../../ARCHITECTURE.es.md)
- [README raíz del repo](../../../../README.md)

---

## ⚠️ Disclaimer

**Este software se proporciona "TAL CUAL", sin garantías. Ver [Disclaimer completo](../../../../README.md#exención-de-responsabilidad) para términos detallados.**

- Sin garantías de idoneidad, sin responsabilidad por pérdidas de datos, brechas de seguridad, interrupciones, sanciones regulatorias.
- Responsabilidad del usuario: validar configuración, gestionar retention/limpieza, cumplir regulaciones (GDPR, etc.).

---

## 📜 Licencia

**ISC License** - Ver [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
