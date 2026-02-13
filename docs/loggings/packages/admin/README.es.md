# ThisCloud.Framework.Loggings.Admin

> 📘 **Idioma**: Español | [English](README.en.md)

## Propósito

Endpoints de administración **runtime** para el framework de logging **ThisCloud.Framework.Loggings**. Permite:

- ✅ Enable/Disable logging dinámicamente (sin reiniciar app)
- ✅ GET/PUT/PATCH settings (niveles, sinks, overrides)
- ✅ DELETE settings (reset a defaults)
- ✅ Gating por entorno (`AllowedEnvironments`)
- ✅ Protección por policy (`RequireAdmin=true` → policy `"Admin"`)
- ✅ Auditoría estructurada de cambios

**⚠️ ADVERTENCIA**: Estos endpoints **NO deben exponerse en Production** sin protección adecuada.

---

## 📦 Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Admin
```

**Dependencias**:
- `ThisCloud.Framework.Loggings.Abstractions`
- ASP.NET Core (Minimal APIs)

**Versión mínima**: .NET 10

---

## ⚡ Inicio Rápido

### Program.cs

```csharp
using ThisCloud.Framework.Loggings.Serilog;
using ThisCloud.Framework.Loggings.Admin;

var builder = WebApplication.CreateBuilder(args);

// Logging core
builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "mi-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "mi-api");

// (Opcional) Autenticación + autorización
builder.Services.AddAuthentication(/* ... */);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Admin endpoints (gated)
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

**Regla mandatoria**: Si `Enabled=true` en Production, **DEBE** tener:
- `AllowedEnvironments` explícito (ej: `["Staging"]`)
- `RequireAdmin=true`
- Policy `"Admin"` configurada con roles/claims reales

---

## 🔧 Configuración

### Root: `ThisCloud:Loggings:Admin`

| Key | Tipo | Default | Regla Production |
|-----|------|---------|-----------------|
| `Enabled` | `bool` | `false` | **DEBE SER `false`** (o gated estrictamente) |
| `AllowedEnvironments` | `string[]` | `[]` | Si `Enabled=true`, debe ser explícito |
| `RequireAdmin` | `bool` | `true` | Si `Enabled=true`, **MANDATORIO `true`** |
| `AdminPolicyName` | `string` | `"Admin"` | Nombre de policy AuthZ |
| `BasePath` | `string` | `"/api/admin/logging"` | — |

---

## 🎯 Endpoints

Base path default: `/api/admin/logging`

### GET `/settings`

Obtener configuración actual.

**Response** (200 OK):
```json
{
  "isEnabled": true,
  "minimumLevel": "Information",
  "overrides": {
    "Microsoft": "Warning"
  },
  "console": { "enabled": true },
  "file": {
    "enabled": true,
    "path": "logs/log-.ndjson",
    "rollingFileSizeMb": 10,
    "retainedFileCountLimit": 30,
    "useCompactJson": true
  },
  "redaction": { "enabled": true },
  "correlation": {
    "headerName": "X-Correlation-Id",
    "generateIfMissing": true
  }
}
```

### PUT `/settings`

Reemplazar configuración completa.

**Request**:
```json
{
  "isEnabled": true,
  "minimumLevel": "Warning",
  "console": { "enabled": false },
  "file": {
    "enabled": true,
    "path": "/var/log/app/log-.ndjson",
    "rollingFileSizeMb": 10
  }
}
```

**Response** (200 OK): Settings actuales tras aplicar cambio.

**Validación**: Si algún valor es inválido (ej: `RollingFileSizeMb=0`), retorna 400 Bad Request.

### PATCH `/settings`

Merge parcial de configuración.

**Request**:
```json
{
  "minimumLevel": "Error",
  "file": {
    "rollingFileSizeMb": 20
  }
}
```

**Semántica**: Solo los campos presentes se actualizan; el resto se preserva.

**Response** (200 OK): Settings completos tras merge.

**Validación**: Igual que PUT.

### POST `/enable`

Activar logging (`IsEnabled=true`).

**Request**: Body vacío (o `{}`).

**Response** (200 OK):
```json
{
  "isEnabled": true,
  "minimumLevel": "Information"
}
```

### POST `/disable`

Desactivar logging (`IsEnabled=false`).

**Request**: Body vacío (o `{}`).

**Response** (200 OK):
```json
{
  "isEnabled": false
}
```

**⚠️ CUIDADO**: Desactiva logging inmediatamente. No loguear después de deshabilitar puede ocultar errores críticos.

### DELETE `/settings`

Reset a defaults (hardcoded).

**Request**: Body vacío.

**Response** (200 OK): Settings reseteados a defaults.

---

## 🛡️ Seguridad

### Gating Automático

Los endpoints **NO se exponen** si:
1. `Admin.Enabled=false` (default)
2. Environment actual NO está en `AllowedEnvironments`

### Protección por Policy

Si `RequireAdmin=true` (default), los endpoints requieren:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
```

**Sin autenticación configurada**: Si `RequireAdmin=true` pero no hay `AddAuthentication`/`AddAuthorization`, las requests fallan con 401/403.

### Auditoría

Cada cambio se loguea vía `IAuditLogger`:
```json
{
  "action": "UpdateSettings",
  "userId": "admin@example.com",
  "before": { "minimumLevel": "Information" },
  "after": { "minimumLevel": "Warning" }
}
```

**Regla**: NO loguear secretos en `before`/`after` (redaction si hace falta).

---

## 📋 Troubleshooting

### Problema: Endpoints no aparecen en Swagger

✅ Admin endpoints se mapean solo si `Admin.Enabled=true` y env permitido.  
✅ Verificar `AllowedEnvironments` incluye el env actual (`ASPNETCORE_ENVIRONMENT`).

### Problema: 401 Unauthorized en desarrollo

✅ Si `RequireAdmin=true`, configurar autenticación o cambiar a `RequireAdmin=false` en Development.

### Problema: PATCH no aplica cambios

✅ PATCH hace merge con settings actuales; verificar que la request contiene las keys correctas (case-sensitive en JSON).

---

## 📚 Documentación Relacionada

- [Framework Loggings (índice)](../../README.es.md)
- [Paquete Abstractions (contratos)](../abstractions/README.es.md)
- [Paquete Serilog (implementación)](../serilog/README.es.md)
- [Arquitectura enterprise-grade](../../ARCHITECTURE.es.md)
- [README raíz del repo](../../../../README.md)

---

## ⚠️ Disclaimer

**Este software se proporciona "TAL CUAL", sin garantías. Ver [Disclaimer completo](../../../../README.md#exención-de-responsabilidad) para términos detallados.**

- Sin garantías de idoneidad, sin responsabilidad por brechas de seguridad si Admin endpoints se exponen sin protección.
- Responsabilidad del usuario: configurar autenticación/autorización adecuada, NO exponer en Production sin gating estricto.

---

## 📜 Licencia

**ISC License** - Ver [LICENSE](../../../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
