# Arquitectura ThisCloud.Framework.Loggings

> 📘 **Idioma**: Español | [English](ARCHITECTURE.en.md)

## Visión General

**ThisCloud.Framework.Loggings** es un framework de logging estructurado basado en **Serilog** con administración runtime obligatoria, diseñado para aplicaciones .NET 10+ empresariales que requieren:

- ✅ Logging estructurado (NDJSON) con sinks mínimos (Console + File rolling 10MB)
- ✅ Correlación automática (CorrelationId, RequestId, TraceId W3C)
- ✅ Redaction obligatoria (secretos, JWT, PII)
- ✅ Control runtime sin reinicio (enable/disable, cambiar niveles)
- ✅ Fail-fast Production (config inválida detiene arranque)
- ✅ Administración vía endpoints (gated por env + policy)

---

## 🏗️ Capas y Dependencias

```
┌────────────────────────────────────────────────────────────┐
│  HOST APPLICATION (.NET 10+)                               │
│  - ASP.NET Core Minimal API / Worker Service / Console    │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  ├─► ILoggingControlService (runtime control)
                  ├─► ICorrelationContext (CorrelationId/RequestId)
                  ├─► IAuditLogger (cambios de config)
                  │
┌─────────────────▼──────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Admin                        │
│  - Minimal API endpoints (GET/PUT/PATCH settings, etc.)    │
│  - Gating: Enabled + AllowedEnvironments + Policy         │
│  - DTOs: LogSettingsDto, UpdateLogSettingsRequest, etc.   │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  │ depends on
                  ▼
┌────────────────────────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Serilog                      │
│  - HostBuilderExtensions.UseThisCloudFrameworkSerilog      │
│  - ServiceCollectionExtensions.AddThisCloudFrameworkLoggings│
│  - SerilogLoggingControlService (runtime reconfig)        │
│  - DefaultLogRedactor (Authorization/JWT/secrets/PII)     │
│  - ThisCloudContextEnricher (correlationId/requestId/etc.)│
│  - SerilogAuditLogger (auditoría sin secretos)            │
│  - ProductionValidator (fail-fast si config inválida)     │
└─────────────────┬──────────────────────────────────────────┘
                  │
                  │ depends on
                  ▼
┌────────────────────────────────────────────────────────────┐
│  ThisCloud.Framework.Loggings.Abstractions                 │
│  - LogLevel (enum: Verbose...Critical)                     │
│  - LogSettings + sub-models (Console/File/Retention/etc.) │
│  - Interfaces:                                             │
│    · ILoggingControlService                                │
│    · ILoggingSettingsStore                                 │
│    · ILogRedactor                                          │
│    · ICorrelationContext                                   │
│    · IAuditLogger                                          │
└────────────────────────────────────────────────────────────┘
```

**Reglas de dependencia (Onion Architecture)**:
- ❌ `Abstractions` NO depende de nadie (sin Serilog, sin ASP.NET Core)
- ✅ `Serilog` depende solo de `Abstractions` + paquetes Serilog
- ✅ `Admin` depende de `Abstractions` + ASP.NET Core Minimal APIs
- ✅ Host depende de `Serilog` (obligatorio) + `Admin` (opcional)

---

## ⚙️ Flujo de Configuración

### 1. Startup (Host)

```
IConfiguration (appsettings.json)
       │
       ├─► Bind: "ThisCloud:Loggings" → LogSettings
       │                                      │
       │                                      ▼
       │                           ThisCloudSerilogOptions
       │                                      │
       ▼                                      ▼
UseThisCloudFrameworkSerilog()    Serilog pipeline configuration
       │                                      │
       ├─► WriteTo.Console()                 │
       ├─► WriteTo.File(                     │
       │      path: "logs/log-.ndjson",      │
       │      rollingInterval: RollingInterval.Day,
       │      fileSizeLimitBytes: 10MB,      │
       │      rollOnFileSizeLimit: true)     │
       │                                     │
       ├─► MinimumLevel.Is(LogEventLevel)   │
       ├─► Enrich.With(ThisCloudContextEnricher)
       │                                     │
       └─► LoggingLevelSwitch (global)      │
                    │                        │
                    └────────────────────────┘
                             │
                             ▼
                    ILogger disponible en DI
```

### 2. Runtime Reconfig (vía Admin APIs o código)

```
Admin API: PATCH /api/admin/logging/settings
       │
       ▼
ILoggingControlService.PatchSettingsAsync(partialSettings)
       │
       ├─► Merge con LogSettings actuales
       ├─► Validar límites (RollingFileSizeMb 1..100, etc.)
       │
       ▼
LoggingLevelSwitch.MinimumLevel = newLevel  ← QUÉ CAMBIA
       │
       ├─► ✅ MinimumLevel (cambia en caliente)
       ├─► ✅ Overrides por namespace (cambia en caliente)
       │
       └─► ❌ Sinks (Console/File) NO se reconfiguran en caliente
           ❌ File.Path NO cambia sin reinicio
           ❌ File.RollingFileSizeMb NO cambia sin reinicio
```

**Limitaciones de reconfig runtime**:
- **Cambia en caliente**: `MinimumLevel`, `Overrides` (namespace → level)
- **NO cambia sin reinicio**: Sinks (Console/File habilitación), `File.Path`, `File.RollingFileSizeMb`, `Retention.Days`

**Razón**: Serilog no soporta reconfiguración dinámica de sinks después del bootstrap (limitation upstream).

---

## 🔗 Correlación (Trazabilidad)

### Fuentes de Correlación

```
HTTP Request (header X-Correlation-Id)
       │
       ├─► Si existe y es GUID válido → usar
       └─► Si NO existe y GenerateIfMissing=true → generar nuevo GUID
                │
                ▼
      HttpContext.Items["CorrelationId"]  ← si ASP.NET Core
                │
                ├─► ICorrelationContext.CorrelationId (DI Scoped)
                │
                ▼
      ThisCloudContextEnricher lee ICorrelationContext
                │
                ▼
      Log enriched property: "correlationId": "a1b2c3d4-..."
```

### Propiedades Enriquecidas (Automáticas)

Cada evento de log incluye:

```json
{
  "@t": "2026-02-15T14:30:00.123Z",
  "@m": "Hello world logged!",
  "@l": "Information",
  "service": "mi-api",
  "env": "Production",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-1234567890ab",
  "requestId": "e5f6g7h8-i9j0-1234-abcd-0987654321fe",
  "traceId": "00-abc123def456...",
  "userId": "user@example.com",
  "sourceContext": "MyNamespace.MyClass"
}
```

**Keys exactas** (definidas en `ThisCloudLogKeys`):
- `service` (string): Nombre del servicio (configurado en `UseThisCloudFrameworkSerilog`)
- `env` (string): Entorno (ASPNETCORE_ENVIRONMENT o DOTNET_ENVIRONMENT)
- `correlationId` (GUID): Correlación de request (si existe)
- `requestId` (GUID): Request ID único (si ASP.NET Core)
- `traceId` (string): W3C Trace ID (si `Activity.Current?.TraceId` existe)
- `userId` (string): User ID (si `ICorrelationContext.UserId` existe)
- `sourceContext` (string): Serilog namespace/clase origen del log

**Integración con `ThisCloud.Framework.Web`**:
- Si el host usa `ThisCloud.Framework.Web`, el middleware de correlación ya popula `HttpContext.Items["CorrelationId"]` y `HttpContext.Items["RequestId"]`.
- `HttpContextCorrelationContext` (implementación de `ICorrelationContext`) lee esos valores automáticamente.
- **Resultado**: Correlación end-to-end sin configuración adicional.

---

## 🛡️ Redaction (Sanitización)

### DefaultLogRedactor

**Responsabilidad**: Redactar datos sensibles en mensajes de log ANTES de escribir a sinks.

**Patrones mínimos mandatorios**:

| Patrón | Match | Reemplazo |
|--------|-------|-----------|
| `Authorization: Bearer <token>` | `Authorization: Bearer eyJ...` | `Authorization: Bearer [REDACTED]` |
| JWT standalone | `eyJ...` (inicio de JWT) | `[REDACTED_JWT]` |
| Secrets en key=value | `apiKey=abc123`, `password=secret` | `apiKey=[REDACTED]`, `password=[REDACTED]` |
| Secrets en key: value | `token: abc123` | `token: [REDACTED]` |
| Emails (best-effort) | `user@example.com` | `[REDACTED_PII]` |
| DNI/NIE España (best-effort) | `12345678Z`, `X1234567A` | `[REDACTED_PII]` |

**Límites del DefaultLogRedactor**:
- ✅ Redacta texto en **mensajes de log** (`message` string)
- ❌ NO redacta propiedades estructuradas complejas (ej: si logueas un objeto con `ILogger.LogInformation("{@Object}", obj)`, los campos del objeto NO se redactan)
- ❌ NO analiza contenido binario ni archivos adjuntos

**Responsabilidad del host**:
- NO loguear objetos con secretos expuestos en `ToString()` o propiedades públicas.
- Usar DTOs sin secrets para logging estructurado.
- Si necesitas redaction custom, implementar `ILogRedactor` y registrarlo en DI.

**Ejemplo de extensión**:

```csharp
public class CustomLogRedactor : ILogRedactor
{
    private readonly DefaultLogRedactor _default = new();
    
    public string Redact(string input)
    {
        // Aplicar redaction por defecto
        var redacted = _default.Redact(input);
        
        // Redaction custom adicional
        redacted = Regex.Replace(redacted, @"SSN:\s*\d{3}-\d{2}-\d{4}", "SSN: [REDACTED_PII]");
        
        return redacted;
    }
}

// Registrar en DI (reemplaza DefaultLogRedactor)
builder.Services.AddSingleton<ILogRedactor, CustomLogRedactor>();
```

---

## 🚨 Fail-fast Production

### ProductionValidator

**Propósito**: Evitar que el host arranque con configuración inválida en Production (detección temprana de errores).

**Reglas de validación (solo Production)**:

| Condición | Validación | Acción si falla |
|-----------|------------|-----------------|
| `File.Enabled=true` | `File.Path` debe estar configurado y no ser vacío | ❌ Lanza `InvalidOperationException` |
| `File.Enabled=true` | `File.RollingFileSizeMb` ∈ [1..100] | ❌ Lanza `InvalidOperationException` |
| `File.Enabled=true` | `File.RetainedFileCountLimit` ∈ [1..365] | ❌ Lanza `InvalidOperationException` |
| `MinimumLevel` | Debe ser un valor válido del enum `LogLevel` | ❌ Lanza `InvalidOperationException` |
| `Admin.Enabled=true` | `AllowedEnvironments` debe ser explícito (no vacío) | ❌ Lanza `InvalidOperationException` |
| `Admin.Enabled=true` | `RequireAdmin` debe ser `true` | ❌ Lanza `InvalidOperationException` |

**Razón del fail-fast**:
- ❌ **NO silent fallback**: Si Production arranca con `File.Path=""`, los logs se pierden silenciosamente → diagnóstico imposible.
- ✅ **Fail-fast**: Arranque falla → alerta temprana → fix antes de deploy → evita pérdida de logs en producción.

**Comportamiento en Development**:
- ⚠️ Validación relajada: Se permiten configuraciones incompletas (Console-only, File deshabilitado).
- Razón: Facilitar desarrollo rápido sin setup complejo.

---

## 🔌 Extension Points

### 1. ILoggingSettingsStore (Persistencia de Configuración)

**Propósito**: Persistir cambios de configuración realizados vía Admin APIs.

**Implementación default**: In-memory (no persistente entre reinicios).

**Cómo extender**:

```csharp
public class SqlLoggingSettingsStore : ILoggingSettingsStore
{
    private readonly string _connectionString;
    
    public string Version => "1.0-sql";
    
    public SqlLoggingSettingsStore(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<LogSettings?> GetAsync(CancellationToken ct = default)
    {
        // SELECT * FROM LoggingSettings WHERE Id = 'current'
        // Deserializar JSON → LogSettings
    }
    
    public async Task SaveAsync(LogSettings settings, CancellationToken ct = default)
    {
        // INSERT/UPDATE LoggingSettings
        // Serializar LogSettings → JSON
        // Guardar con timestamp para auditoría
    }
}

// Registrar en DI (reemplaza InMemoryLoggingSettingsStore)
builder.Services.AddSingleton<ILoggingSettingsStore>(
    new SqlLoggingSettingsStore(builder.Configuration.GetConnectionString("Logging")));
```

**Schema SQL recomendado** (ver `docs/loggings/sqlserver/schema_v1.sql` para schema completo):

```sql
CREATE TABLE LoggingSettings (
    Id NVARCHAR(50) PRIMARY KEY,  -- 'current'
    SettingsJson NVARCHAR(MAX) NOT NULL,
    LastModified DATETIME2 NOT NULL,
    ModifiedBy NVARCHAR(255)
);
```

---

### 2. ICorrelationContext (Fuente de Correlación Custom)

**Propósito**: Personalizar de dónde se obtienen CorrelationId/RequestId/TraceId/UserId.

**Implementación default**: `HttpContextCorrelationContext` (lee de `HttpContext.Items` si ASP.NET Core).

**Cómo extender** (ej: leer de un header custom):

```csharp
public class CustomHeaderCorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CustomHeaderCorrelationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-My-Correlation-Id"].FirstOrDefault();
    
    public string? RequestId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-My-Request-Id"].FirstOrDefault();
    
    public string? TraceId =>
        Activity.Current?.TraceId.ToString();
    
    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}

// Registrar en DI (reemplaza HttpContextCorrelationContext)
builder.Services.AddScoped<ICorrelationContext, CustomHeaderCorrelationContext>();
```

---

### 3. IAuditLogger (Auditoría Custom)

**Propósito**: Loguear cambios de configuración (Admin APIs) con formato custom.

**Implementación default**: `SerilogAuditLogger` (loguea con `ILogger<SerilogAuditLogger>` a nivel `Information`).

**Cómo extender** (ej: escribir a tabla de auditoría):

```csharp
public class SqlAuditLogger : IAuditLogger
{
    private readonly string _connectionString;
    
    public void LogChange(string action, string userId, object? before, object? after)
    {
        // INSERT INTO AuditLog (Action, UserId, BeforeJson, AfterJson, Timestamp)
        // Serializar before/after → JSON (asegurar NO incluir secretos)
    }
}

// Registrar en DI (reemplaza SerilogAuditLogger)
builder.Services.AddSingleton<IAuditLogger>(
    new SqlAuditLogger(builder.Configuration.GetConnectionString("Audit")));
```

**⚠️ REGLA CRÍTICA**: NO loguear secretos en `before`/`after`. Si el objeto contiene passwords/tokens, redactarlos antes de serializar.

---

### 4. ILogRedactor (Redaction Custom)

Ver sección [Redaction](#-redaction-sanitización) arriba.

---

## 🔒 Seguridad

### Superficie de Ataque

| Componente | Superficie | Protección |
|------------|-----------|------------|
| **Serilog sinks** | File system (logs escritos a disco) | Permisos de archivo (host responsable), retention (host responsable) |
| **Admin endpoints** | HTTP (GET/PUT/PATCH/POST/DELETE) | Gating: `Enabled=false` por defecto + `AllowedEnvironments` + `RequireAdmin=true` + Policy |
| **Logs en disco** | Acceso físico/red al file system | Encryption at rest (host responsable), log rotation (10MB + day), retention limpieza (host responsable) |

### Límites de Seguridad

**✅ Qué hace el framework**:
- Redaction de secretos en mensajes (Authorization/JWT/apiKey/etc.)
- Fail-fast si config inválida en Production
- Admin endpoints deshabilitados por defecto
- Policy enforcement si `RequireAdmin=true`

**❌ Qué NO hace el framework** (responsabilidad del host):
- ❌ **NO body logging**: Request/response payloads crudos NO se loguean (prohibido por diseño)
- ❌ **NO encryption de logs**: Si necesitas logs encriptados en disco, usar file system encryption (BitLocker, LUKS, etc.)
- ❌ **NO autenticación/autorización**: El host DEBE configurar `AddAuthentication()` + `AddAuthorization()` + policy `"Admin"`
- ❌ **NO limpieza automática de logs antiguos**: El host DEBE implementar job/cron para purgar logs > `Retention.Days`
- ❌ **NO SIEM/centralización**: El host DEBE configurar shippers (Logstash, Fluentd, etc.) si necesita logs centralizados

### Checklist de Seguridad Obligatorio

Ver [CHECKLIST.es.md](CHECKLIST.es.md) para checklist completo de consumo seguro.

---

## 📚 Referencias

- [README raíz (índice monorepo)](../../README.md)
- [Paquete Abstractions (contratos)](packages/abstractions/README.es.md)
- [Paquete Serilog (implementación)](packages/serilog/README.es.md)
- [Paquete Admin (endpoints)](packages/admin/README.es.md)
- [Checklist consumo seguro](CHECKLIST.es.md)

---

## ⚠️ Disclaimer

**Este software se proporciona "TAL CUAL", sin garantías. Ver [Disclaimer completo](../../README.md#exención-de-responsabilidad) para términos detallados.**

- Sin garantías de idoneidad, sin responsabilidad por pérdidas de datos, brechas de seguridad, interrupciones, sanciones regulatorias.
- Responsabilidad del usuario: validar configuración, gestionar retention/limpieza, cumplir regulaciones (GDPR, etc.).

---

## 📜 Licencia

**ISC License** - Ver [LICENSE](../../LICENSE)

Copyright (c) 2025 Marco Alejandro De Santis
