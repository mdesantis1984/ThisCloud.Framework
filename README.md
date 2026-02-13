# ThisCloud.Framework

> 🌐 **Versión canónica:** Español (este documento)  
> 🇬🇧 **Canonical version:** Spanish (this document) | [English Summary](#executive-summary-in-english) ⬇️

**Framework modular** para .NET 10+ con paquetes NuGet públicos que proporcionan funcionalidad estandarizada y lista para producción en aplicaciones empresariales.

---

## 📦 Frameworks Disponibles

| Framework | Propósito | Documentación |
|-----------|-----------|---------------|
| **Web** | APIs mínimas estandarizadas ASP.NET Core (contratos HTTP, correlación, CORS, Swagger) | [docs/web/README.md](docs/web/README.md) |
| **Loggings** | Logging estructurado enterprise-grade (Serilog, sinks, correlación, admin runtime, redaction) | [docs/loggings/README.es.md](docs/loggings/README.es.md) |

---

## 🎯 Paquetes ThisCloud.Framework.Loggings

| Paquete | Propósito | Documentación |
|---------|-----------|---------------|
| **ThisCloud.Framework.Loggings.Abstractions** | Contratos core de logging (interfaces, modelos, LogLevel canon) | [docs/loggings/packages/abstractions/README.es.md](docs/loggings/packages/abstractions/README.es.md) |
| **ThisCloud.Framework.Loggings.Serilog** | Implementación Serilog con sinks (Console, File 10MB rolling), enrichment, redaction, fail-fast Production | [docs/loggings/packages/serilog/README.es.md](docs/loggings/packages/serilog/README.es.md) |
| **ThisCloud.Framework.Loggings.Admin** | Endpoints de administración runtime (enable/disable, GET/PUT/PATCH settings, gating por env/policy) | [docs/loggings/packages/admin/README.es.md](docs/loggings/packages/admin/README.es.md) |

---

## ⚡ Inicio Rápido (Loggings)

### Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

### Program.cs mínimo

```csharp
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Registrar Serilog como logger (Console + File sinks con rolling 10MB)
builder.Host.UseThisCloudFrameworkSerilog(
    builder.Configuration,
    serviceName: "mi-api");

// Registrar servicios de logging (control runtime, redaction, correlation context)
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

### Configuración mínima (appsettings.json)

```json
{
  "ThisCloud": {
    "Loggings": {
      "IsEnabled": true,
      "MinimumLevel": "Information",
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

**Production** (`appsettings.Production.json`):
```json
{
  "ThisCloud": {
    "Loggings": {
      "MinimumLevel": "Warning",
      "Console": {
        "Enabled": false
      },
      "File": {
        "Enabled": true,
        "Path": "/var/log/myapp/log-.ndjson"
      }
    }
  }
}
```

---

## 🛡️ Checklist de Producción (Loggings)

- ✅ **NO loguear secretos**: `Authorization`, JWT, API keys, passwords, PII sin redaction
- ✅ **NO body logging** (request/response payloads crudos): prohibido por defecto
- ✅ **Redaction activada**: `Redaction.Enabled=true` (default)
- ✅ **Console.Enabled=false** en Production (performance + seguridad)
- ✅ **File sink configurado** con path absoluto y permisos correctos
- ✅ **Retention lógico**: responsabilidad del host (limpiar logs antiguos según `Retention.Days`)
- ✅ **Fail-fast Production**: config inválida detiene el arranque (no silent fallback)
- ✅ **Admin endpoints deshabilitados** en Production por defecto (`Admin.Enabled=false`)

---

## 📜 Licencia

**ISC License** (permisiva)

Copyright (c) 2025 Marco Alejandro De Santis

Se concede permiso para usar, copiar, modificar y/o distribuir este software con cualquier propósito, con o sin cargo, siempre que el aviso de copyright anterior y este aviso de permiso aparezcan en todas las copias.

**EL SOFTWARE SE PROPORCIONA "TAL CUAL" Y EL AUTOR NIEGA TODAS LAS GARANTÍAS CON RESPECTO A ESTE SOFTWARE, INCLUIDAS TODAS LAS GARANTÍAS IMPLÍCITAS DE COMERCIABILIDAD E IDONEIDAD. EN NINGÚN CASO EL AUTOR SERÁ RESPONSABLE DE NINGÚN DAÑO ESPECIAL, DIRECTO, INDIRECTO O CONSECUENTE O CUALQUIER DAÑO QUE RESULTE DE LA PÉRDIDA DE USO, DATOS O GANANCIAS, YA SEA EN UNA ACCIÓN DE CONTRATO, NEGLIGENCIA U OTRA ACCIÓN ILÍCITA, QUE SURJA DE O EN CONEXIÓN CON EL USO O EL RENDIMIENTO DE ESTE SOFTWARE.**

Ver [LICENSE](LICENSE) para el texto completo.

---

## ⚠️ Exención de Responsabilidad

### Español

**IMPORTANTE: Este software se proporciona "TAL CUAL", sin garantías de ningún tipo.**

- ❌ **Sin garantías**: No se garantiza que el software sea adecuado para ningún propósito específico, esté libre de errores o funcione ininterrumpidamente.
- ❌ **Sin responsabilidad**: El autor NO es responsable por:
  - Daños directos, indirectos, incidentales, especiales o consecuenciales
  - Pérdida de datos, interrupciones de servicio o tiempo de inactividad
  - Brechas de seguridad, filtraciones de información o vulnerabilidades
  - Sanciones regulatorias (GDPR, HIPAA, etc.), incumplimientos legales o auditorías fallidas
  - Pérdidas financieras, lucro cesante o daños a la reputación
- ⚠️ **Uso bajo responsabilidad del usuario**: Es responsabilidad exclusiva del usuario:
  - Validar que el software cumple con sus requisitos específicos
  - Implementar controles de seguridad adecuados (redaction, secrets management, access control)
  - Cumplir con regulaciones aplicables (protección de datos, auditoría, retención)
  - Gestionar retention/limpieza de logs, permisos de archivos y capacidad de almacenamiento
  - Probar en entornos no productivos antes de desplegar
- ❌ **Sin SLA ni soporte implícito**: No se garantiza tiempo de respuesta, corrección de bugs ni actualizaciones. El mantenimiento es voluntario y sin compromiso.

**El uso de este software implica la aceptación total de estos términos.**

---

### English

**IMPORTANT: This software is provided "AS IS", without warranties of any kind.**

- ❌ **No warranties**: No guarantee is made that the software is fit for any specific purpose, error-free or will operate uninterrupted.
- ❌ **No liability**: The author is NOT liable for:
  - Direct, indirect, incidental, special or consequential damages
  - Data loss, service interruptions or downtime
  - Security breaches, information leaks or vulnerabilities
  - Regulatory sanctions (GDPR, HIPAA, etc.), legal non-compliance or failed audits
  - Financial losses, lost profits or reputational damage
- ⚠️ **Use at your own risk**: It is the user's sole responsibility to:
  - Validate that the software meets their specific requirements
  - Implement appropriate security controls (redaction, secrets management, access control)
  - Comply with applicable regulations (data protection, audit, retention)
  - Manage log retention/cleanup, file permissions and storage capacity
  - Test in non-production environments before deploying
- ❌ **No SLA or implied support**: No response time, bug fixes or updates are guaranteed. Maintenance is voluntary and without commitment.

**Using this software implies full acceptance of these terms.**

---

## 🤝 Contribuciones

Este es un repositorio público. Las contribuciones son bienvenidas vía Pull Requests.

**Reglas**:
- Cobertura de línea ≥90% obligatoria (gate en CI)
- Prohibido commits directos a `main`/`develop` (solo PRs)
- Documentación bilingüe (ES/EN) mandatoria para cambios públicos
- Seguir convenciones de código existentes (Clean Architecture, SOLID)

---

## 📚 Documentación

- **Web Framework**: [docs/web/README.md](docs/web/README.md)
- **Loggings Framework**: [docs/loggings/README.es.md](docs/loggings/README.es.md) (ES) | [docs/loggings/README.en.md](docs/loggings/README.en.md) (EN)
- **Arquitectura Loggings**: [docs/loggings/ARCHITECTURE.es.md](docs/loggings/ARCHITECTURE.es.md) (ES) | [docs/loggings/ARCHITECTURE.en.md](docs/loggings/ARCHITECTURE.en.md) (EN)

---

## Executive Summary in English

**ThisCloud.Framework** is a modular framework for .NET 10+ with public NuGet packages providing standardized, production-ready functionality for enterprise applications.

### 🎯 Available Frameworks

| Framework | Purpose | Documentation |
|-----------|---------|---------------|
| **Web** | Standardized ASP.NET Core Minimal APIs (HTTP contracts, correlation, CORS, Swagger) | [docs/web/README.md](docs/web/README.md) |
| **Loggings** | Enterprise-grade structured logging (Serilog, sinks, correlation, runtime admin, redaction) | [docs/loggings/README.en.md](docs/loggings/README.en.md) |

### 📦 ThisCloud.Framework.Loggings Packages

| Package | Purpose | Documentation |
|---------|---------|---------------|
| **ThisCloud.Framework.Loggings.Abstractions** | Core logging contracts (interfaces, models, canonical LogLevel) | [docs/loggings/packages/abstractions/README.en.md](docs/loggings/packages/abstractions/README.en.md) |
| **ThisCloud.Framework.Loggings.Serilog** | Serilog implementation with sinks (Console, File 10MB rolling), enrichment, redaction, fail-fast Production | [docs/loggings/packages/serilog/README.en.md](docs/loggings/packages/serilog/README.en.md) |
| **ThisCloud.Framework.Loggings.Admin** | Runtime admin endpoints (enable/disable, GET/PUT/PATCH settings, env/policy gating) | [docs/loggings/packages/admin/README.en.md](docs/loggings/packages/admin/README.en.md) |

### ⚡ Quick Start (Loggings)

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

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

### 📜 License

**ISC License** (permissive) - See [LICENSE](LICENSE)

**THE SOFTWARE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND. THE AUTHOR IS NOT LIABLE FOR ANY DAMAGES.**

### ⚠️ Disclaimer

**Use at your own risk. No warranties. No liability for data loss, security breaches, regulatory sanctions, or any damages. No SLA or implied support.**

See [Spanish Disclaimer](#exención-de-responsabilidad) above for full text.

### 📚 Documentation

- **Web Framework**: [docs/web/README.md](docs/web/README.md)
- **Loggings Framework**: [docs/loggings/README.en.md](docs/loggings/README.en.md)
- **Loggings Architecture**: [docs/loggings/ARCHITECTURE.en.md](docs/loggings/ARCHITECTURE.en.md)

---

**Repository**: [github.com/mdesantis1984/ThisCloud.Framework](https://github.com/mdesantis1984/ThisCloud.Framework)  
**License**: ISC  
**Author**: Marco Alejandro De Santis © 2025
