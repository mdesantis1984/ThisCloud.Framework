# ThisCloud.Framework.Loggings.Admin

> **Español** | [English](#english)

## Español

**Endpoints de administración runtime** para logging (enable/disable, GET/PUT/PATCH settings). **⚠️ NO exponer en Production sin protección.**

### Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Admin
```

### Inicio Rápido

```csharp
using ThisCloud.Framework.Loggings.Serilog;
using ThisCloud.Framework.Loggings.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "mi-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "mi-api");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();
app.UseAuthorization();

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
        "RequireAdmin": false
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

### Endpoints

- `GET /api/admin/logging/settings`: Obtener configuración
- `PUT /api/admin/logging/settings`: Reemplazar configuración
- `PATCH /api/admin/logging/settings`: Merge parcial
- `POST /api/admin/logging/enable`: Activar logging
- `POST /api/admin/logging/disable`: Desactivar logging
- `DELETE /api/admin/logging/settings`: Reset a defaults

### ⚠️ Seguridad

- ✅ Endpoints deshabilitados por defecto (`Enabled=false`)
- ✅ Gating por entorno (`AllowedEnvironments`)
- ✅ Policy obligatoria (`RequireAdmin=true` → policy `"Admin"`)
- ❌ **NO exponer públicamente sin autenticación**

### Documentación Completa

- 📚 [Guía completa (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/admin/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/admin/README.en.md)
- 🏗️ [Arquitectura](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.es.md)

### Licencia

**ISC License** - Sin garantías, sin responsabilidad por brechas de seguridad si se expone sin protección.  
Ver [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE) completo.

---

## English

**Runtime admin endpoints** for logging (enable/disable, GET/PUT/PATCH settings). **⚠️ DO NOT expose in Production without protection.**

### Install

```bash
dotnet add package ThisCloud.Framework.Loggings.Admin
```

### Quick Start

```csharp
using ThisCloud.Framework.Loggings.Serilog;
using ThisCloud.Framework.Loggings.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseThisCloudFrameworkSerilog(builder.Configuration, "my-api");
builder.Services.AddThisCloudFrameworkLoggings(builder.Configuration, "my-api");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();
app.UseAuthorization();

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
        "RequireAdmin": false
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

### Endpoints

- `GET /api/admin/logging/settings`: Get configuration
- `PUT /api/admin/logging/settings`: Replace configuration
- `PATCH /api/admin/logging/settings`: Partial merge
- `POST /api/admin/logging/enable`: Enable logging
- `POST /api/admin/logging/disable`: Disable logging
- `DELETE /api/admin/logging/settings`: Reset to defaults

### ⚠️ Security

- ✅ Endpoints disabled by default (`Enabled=false`)
- ✅ Environment gating (`AllowedEnvironments`)
- ✅ Mandatory policy (`RequireAdmin=true` → policy `"Admin"`)
- ❌ **DO NOT expose publicly without authentication**

### Full Documentation

- 📚 [Complete guide (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/admin/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/admin/README.en.md)
- 🏗️ [Architecture](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.en.md)

### License

**ISC License** - No warranties, no liability for security breaches if exposed without protection.  
See full [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE).

---

**Copyright © 2025 Marco Alejandro De Santis**
