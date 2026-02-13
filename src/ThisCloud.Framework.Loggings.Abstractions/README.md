# ThisCloud.Framework.Loggings.Abstractions

> **Español** | [English](#english)

## Español

**Contratos core de logging** para .NET 10+. Sin dependencias (Serilog-agnostic, ASP.NET Core-agnostic).

### Instalación

```bash
dotnet add package ThisCloud.Framework.Loggings.Abstractions
```

### API Principal

- **`LogLevel` enum**: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Critical`
- **`LogSettings` model**: Configuración completa (Console, File 10MB rolling, Retention, Redaction, Correlation)
- **Interfaces**:
  - `ILoggingControlService`: Enable/disable runtime, cambiar niveles
  - `ILoggingSettingsStore`: Persistir configuración
  - `ILogRedactor`: Redactar secretos
  - `ICorrelationContext`: CorrelationId/RequestId/TraceId/UserId
  - `IAuditLogger`: Auditoría de cambios

### Documentación Completa

- 📚 [Guía completa (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/abstractions/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/abstractions/README.en.md)
- 🏗️ [Arquitectura](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.es.md)

### Licencia

**ISC License** - Sin garantías ("AS IS"), uso bajo responsabilidad del usuario.  
Ver [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE) completo.

---

## English

**Core logging contracts** for .NET 10+. No dependencies (Serilog-agnostic, ASP.NET Core-agnostic).

### Install

```bash
dotnet add package ThisCloud.Framework.Loggings.Abstractions
```

### Main API

- **`LogLevel` enum**: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Critical`
- **`LogSettings` model**: Complete config (Console, File 10MB rolling, Retention, Redaction, Correlation)
- **Interfaces**:
  - `ILoggingControlService`: Runtime enable/disable, change levels
  - `ILoggingSettingsStore`: Persist configuration
  - `ILogRedactor`: Redact secrets
  - `ICorrelationContext`: CorrelationId/RequestId/TraceId/UserId
  - `IAuditLogger`: Change audit

### Full Documentation

- 📚 [Complete guide (ES)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/abstractions/README.es.md)
- 📚 [Full guide (EN)](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/packages/abstractions/README.en.md)
- 🏗️ [Architecture](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/docs/loggings/ARCHITECTURE.en.md)

### License

**ISC License** - No warranties ("AS IS"), use at your own risk.  
See full [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE).

---

**Copyright © 2025 Marco Alejandro De Santis**
