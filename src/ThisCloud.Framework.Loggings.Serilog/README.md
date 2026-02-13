# ThisCloud.Framework.Loggings.Serilog

> **Español** | [English](#english)

## Español

**Implementación Serilog** con sinks (Console + File rolling 10MB), enrichment, redaction, runtime control, fail-fast Production.

### Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

### Inicio Rápido

```csharp
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "mi-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "mi-api");

var app = builder.Build();
app.Run();
```

### appsettings.json

```json
{
  "ThisCloud": {
    "Loggings": {
      "MinimumLevel": "Information",
      "Console": { "Enabled": true },
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMb": 10
      },
      "Redaction": { "Enabled": true }
    }
  }
}
```

### ⚠️ Production

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

### Seguridad

- ❌ **NO loguear secretos**: `Authorization`, JWT, passwords, API keys
- ❌ **NO body logging** (prohibido por defecto)
- ✅ **Redaction.Enabled=true** obligatorio en Production
- ✅ **Fail-fast**: Config inválida detiene arranque

### Documentación Completa

- 📚 [Guía completa (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/serilog/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/serilog/README.en.md)
- 🏗️ [Arquitectura](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.es.md)

### Licencia

**ISC License** - Sin garantías, sin responsabilidad por pérdidas de datos/brechas de seguridad.  
Ver [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE) completo.

---

## English

**Serilog implementation** with sinks (Console + File rolling 10MB), enrichment, redaction, runtime control, fail-fast Production.

### Install

```bash
dotnet add package ThisCloud.Framework.Loggings.Serilog
```

### Quick Start

```csharp
using ThisCloud.Framework.Loggings.Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "my-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "my-api");

var app = builder.Build();
app.Run();
```

### appsettings.json

```json
{
  "ThisCloud": {
    "Loggings": {
      "MinimumLevel": "Information",
      "Console": { "Enabled": true },
      "File": {
        "Enabled": true,
        "Path": "logs/log-.ndjson",
        "RollingFileSizeMb": 10
      },
      "Redaction": { "Enabled": true }
    }
  }
}
```

### ⚠️ Production

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

### Security

- ❌ **NO logging secrets**: `Authorization`, JWT, passwords, API keys
- ❌ **NO body logging** (prohibited by default)
- ✅ **Redaction.Enabled=true** mandatory in Production
- ✅ **Fail-fast**: Invalid config stops startup

### Full Documentation

- 📚 [Complete guide (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/serilog/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/serilog/README.en.md)
- 🏗️ [Architecture](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.en.md)

### License

**ISC License** - No warranties, no liability for data loss/security breaches.  
See full [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE).

---

**Copyright © 2025 Marco Alejandro De Santis**
