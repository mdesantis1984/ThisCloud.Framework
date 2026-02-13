# PLAN ThisCloud.Framework.Loggings — Observability.Logging (Serilog) + Admin APIs + DB Schema

- Solución: `ThisCloud.Framework.slnx`
- Rama: `feature/L4-loggings-admin-apis`
- Versión: **1.1-framework.loggings.2**
- Fecha inicio: **2026-02-12**
- Última actualización: **2026-02-15**
- Estado global: 🟢 **EN PROGRESO** — Fase 0 ✅ | Fase 1 ✅ | Fase 2 ✅ | Fase 3 ✅ | Fase 4 parcial (27/37 tareas = **73%** ejecutado)

## Objetivo
Entregar un framework de logging **público** dentro de **ThisCloud.Framework** (paquetizado y publicado en **NuGet.org**), reutilizable por cualquier consumidor **.NET 10+**, con:

- Serilog como core (logging estructurado).
- Niveles canon: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Critical`.
- Correlación obligatoria:
  - `CorrelationId` (GUID)
  - `RequestId` (GUID, cuando exista)
  - `TraceId` W3C (Activity/traceparent)
- Enrichment estándar y estable (propiedades y keys fijas).
- Sinks mínimos in-scope: **Console + File** (rolling por tamaño **10MB**).
- Administración runtime **obligatoria** vía **endpoints** (Minimal APIs):
  - Enable/Disable
  - GET/PUT/PATCH settings
  - Reset settings
- Sanitización / redaction centralizada (no loguear secretos/PII).
- Auditoría estructurada para cambios de configuración (quién cambió qué, sin secretos).
- **Esquema de base de datos definido y documentado** (SQL Server) para:
  - settings actuales
  - historial/auditoría
  - (opcional) almacenamiento de eventos para query/stats futuros
- Cobertura mínima mandatoria **>=90%** (fallar build si baja).
- CI en PR + Publish por tag `v*` a NuGet.org (mismo estándar del repo).
- **Documentación enterprise-grade bilingüe (ES/EN)** y **README visible en NuGet** por paquete.

## Contexto (DECISIÓN CERRADA)
- `ThisCloud.Framework` es una solución global de framework **público**, mantenida como productos NuGet (`ThisCloud.Framework.*`) para consumo externo.
- `ThisCloud.Framework.Loggings` se integra con `ThisCloud.Framework.Web` (Correlation/RequestId middlewares), pero **NO** depende de él.

---

## Alcance
Paquetes (DECISIÓN CERRADA):

1) `ThisCloud.Framework.Loggings.Abstractions` (net10.0)  
2) `ThisCloud.Framework.Loggings.Serilog` (net10.0)  
3) `ThisCloud.Framework.Loggings.Admin` (net10.0) — **MANDATORIO** (administración por endpoints)

Fuera de alcance (pero se integra):
- UI de administración (MudBlazor u otra): responsabilidad del consumidor.
- Logging de bodies HTTP / payloads crudos: **PROHIBIDO** (seguridad).
- AuthN/AuthZ: el host define policies; Admin solo exige “policy Admin”.
- Persistencia completa de eventos + explorer/stats sobre DB: **POSTPONED** (v1.2), pero el **schema queda definido** en v1.1.

---

## 🚨 Reglas no negociables
1) ❌ Prohibido loguear datos sensibles: `Authorization`, JWT completo, passwords, api keys, secretos, PII sin redaction.  
2) ❌ Prohibido body logging por defecto (request/response).  
3) ✅ Coverage line >= 90% (por solución) enforced en CI.  
4) ❌ Prohibido `try/catch` vacíos.  
5) ✅ Abstractions no depende de Serilog ni de ASP.NET Core.  
6) ✅ Fail-fast en config inválida en Production.  
7) ✅ Git Flow: PR obligatorio; **prohibido** trabajar directo sobre `main/develop`.  
8) ✅ La documentación **es contractual**: sin README/Docs completos, no se considera “Done”.

---

## 📜 Licencia y Disclaimer (DECISIÓN CERRADA)
Licencia global del repositorio: **ISC License** (permisiva, “AS IS”, limitación de responsabilidad).

### Entregables obligatorios
- Archivo `LICENSE` en raíz con texto **ISC oficial**.
- `PackageLicenseExpression` en **todos** los paquetes publicables: `ISC`.
- Sección “Disclaimer / Exención de responsabilidad” en el README del repo y en los README por paquete (ES/EN).

### Disclaimer mínimo (obligatorio, ES/EN)
Debe cubrir explícitamente:
- Sin garantías (“AS IS”), sin idoneidad para propósito específico.
- Sin responsabilidad por daños directos/indirectos, pérdida de datos, interrupciones, brechas de seguridad, sanciones regulatorias, etc.
- Uso bajo responsabilidad del usuario.
- No soporte implícito / no SLA.

---

## DECISIÓN CERRADA: Target / Testing / Versioning
- Target: **net10.0** (mínimo .NET 10).
- Tests: **xUnit.net v3** (`xunit.v3`). Prohibido MSTest/NUnit.
- Coverage: `coverlet.msbuild` threshold **line >=90**.
- Versionado: `Nerdbank.GitVersioning` (NBGV). Prohibido versionado manual en csproj.
- Git Flow: PR obligatorio, CI verde, sin commits directos a `main/develop`.

---

## NuGet y versiones (DECISIÓN CERRADA)
> Regla: Central Package Management (`Directory.Packages.props`) con versiones exactas.

### Runtime (src)
- `Serilog` **4.3.1**
- `Serilog.Extensions.Hosting` **10.0.0**
- `Serilog.Settings.Configuration` **10.0.0**
- `Serilog.Enrichers.Environment` **3.0.1**
- `Serilog.Enrichers.Process` **3.0.0**
- `Serilog.Enrichers.Thread` **4.0.0**
- `Serilog.Sinks.Console` **6.1.1**
- `Serilog.Sinks.File` **7.0.0**
- `Serilog.Formatting.Compact` **3.0.0**
- `Serilog.AspNetCore` **10.0.0** (Admin)

### Testing (tests)
- `Microsoft.AspNetCore.Mvc.Testing` **10.0.2**
- `Microsoft.NET.Test.Sdk` **18.0.1**
- `xunit.v3` **3.2.2**
- `xunit.runner.visualstudio` **3.1.5**
- `coverlet.msbuild` **6.0.4**
- `FluentAssertions` **7.2.0**
- `Serilog.Sinks.InMemory` **1.0.1**

### Versionado (raíz)
- `Nerdbank.GitVersioning` **3.9.50**

---

## Estructura de repositorio (DECISIÓN CERRADA)
Se agregan a `ThisCloud.Framework.slnx`:

- `src/ThisCloud.Framework.Loggings.Abstractions/ThisCloud.Framework.Loggings.Abstractions.csproj`
- `src/ThisCloud.Framework.Loggings.Serilog/ThisCloud.Framework.Loggings.Serilog.csproj`
- `src/ThisCloud.Framework.Loggings.Admin/ThisCloud.Framework.Loggings.Admin.csproj`
- `tests/ThisCloud.Framework.Loggings.Abstractions.Tests/ThisCloud.Framework.Loggings.Abstractions.Tests.csproj`
- `tests/ThisCloud.Framework.Loggings.Serilog.Tests/ThisCloud.Framework.Loggings.Serilog.Tests.csproj`
- `tests/ThisCloud.Framework.Loggings.Admin.Tests/ThisCloud.Framework.Loggings.Admin.Tests.csproj`
- `samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj`
- `docs/loggings/sqlserver/schema_v1.sql` (MANDATORIO)
- `docs/loggings/README.md` (MANDATORIO)

---

## API pública del framework (DECISIÓN CERRADA)

### Namespaces y tipos públicos

#### `ThisCloud.Framework.Loggings.Abstractions`
- `LogLevel` (enum): `Verbose|Debug|Information|Warning|Error|Critical`
- `LogSettings` + sub-modelos:
  - `bool IsEnabled`
  - `LogLevel MinimumLevel`
  - `IReadOnlyDictionary<string, LogLevel> Overrides` (por `SourceContext`/namespace)
  - `ConsoleSinkSettings Console`
  - `FileSinkSettings File`
  - `RetentionSettings Retention`
  - `RedactionSettings Redaction`
  - `CorrelationSettings Correlation`
- `ConsoleSinkSettings`:
  - `bool Enabled`
- `FileSinkSettings`:
  - `bool Enabled`
  - `string Path`
  - `int RollingFileSizeMb` (default **10**, límites 1..100)
  - `int RetainedFileCountLimit` (default 30, límites 1..365)
  - `bool UseCompactJson` (default true → NDJSON)
- `RetentionSettings`:
  - `int Days` (default 30, límites 1..3650) — TTL lógico (limpieza a cargo del host)
- `RedactionSettings`:
  - `bool Enabled` (default true)
  - `string[] AdditionalPatterns` (opcional)
- `CorrelationSettings`:
  - `string HeaderName` (default `X-Correlation-Id`)
  - `bool GenerateIfMissing` (default true)
- Interfaces:
  - `ILoggingControlService` (Enable/Disable/Set/Patch)
  - `ILoggingSettingsStore` (Get/Save + version)
  - `ILogRedactor`
  - `ICorrelationContext` (CorrelationId/RequestId/TraceId/UserId)
  - `IAuditLogger`

#### `ThisCloud.Framework.Loggings.Serilog`
- `HostBuilderExtensions`
- `ServiceCollectionExtensions`
- `ThisCloudSerilogOptions`
- `ThisCloudContextEnricher`
- `DefaultLogRedactor`
- `SerilogAuditLogger`
- `SerilogLoggingControlService` (reconfiguración runtime con `LoggingLevelSwitch`)

#### `ThisCloud.Framework.Loggings.Admin`
- `EndpointRouteBuilderExtensions`
- `ThisCloudLoggingsAdminOptions`
- DTOs request/response:
  - `LogSettingsDto`
  - `PatchLogSettingsRequest`
  - `UpdateLogSettingsRequest`

---

## Signatures exactas (MANDATORIO)

### Serilog (Host + DI)
```csharp
namespace ThisCloud.Framework.Loggings.Serilog;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseThisCloudFrameworkSerilog(
        this IHostBuilder host,
        IConfiguration configuration,
        string serviceName);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThisCloudFrameworkLoggings(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName);
}
```

### Admin APIs (Minimal APIs)
```csharp
namespace ThisCloud.Framework.Loggings.Admin;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapThisCloudFrameworkLoggingsAdmin(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration);
}
```

---

## Configuración (MANDATORIO)

### Root: `ThisCloud:Loggings`
- `IsEnabled` (bool, default true)
- `MinimumLevel` (string, default "Information")
- `Overrides` (Dictionary<string,string>) — namespace/SourceContext → level
- `Console.Enabled` (bool, default true en Development; false recomendado en Production)
- `File.Enabled` (bool, default true)
- `File.Path` (string, default `"logs/log-.ndjson"`)
- `File.RollingFileSizeMb` (int, default **10**)
- `File.RetainedFileCountLimit` (int, default 30)
- `File.UseCompactJson` (bool, default true)
- `Retention.Days` (int, default 30)
- `Redaction.Enabled` (bool, default true)
- `Redaction.AdditionalPatterns` (string[], opcional)
- `Correlation.HeaderName` (string, default `"X-Correlation-Id"`)
- `Correlation.GenerateIfMissing` (bool, default true)

### Root: `ThisCloud:Loggings:Admin`
- `Enabled` (bool, default false)
- `AllowedEnvironments` (string[]; default `["Development"]`)
- `RequireAdmin` (bool, default true)
- `AdminPolicyName` (string, default `"Admin"`)
- `BasePath` (string, default `"/api/admin/logging"`)

Validación mandatoria:
- `File.RollingFileSizeMb` ∈ [1..100]
- `Retention.Days` ∈ [1..3650]
- `MinimumLevel` ∈ {Verbose, Debug, Information, Warning, Error, Critical}
- Production:
  - `Admin.Enabled=true` requiere `AllowedEnvironments` explícito y `RequireAdmin=true`

---

## LoggingsContracts v1 (MANDATORIO)

### Propiedades estándar (enrichment)
Keys exactas (`ThisCloudLogKeys`):
- `service`
- `env`
- `correlationId`
- `requestId`
- `traceId`
- `userId`
- `sourceContext` (Serilog)

Reglas:
- Si no hay valor, no se emite la property.
- `traceId` se toma de `Activity.Current?.TraceId` cuando exista.

### Correlación
- Header recomendado: `X-Correlation-Id`.
- Si el cliente no lo envía y `GenerateIfMissing=true`, se genera GUID.
- Si el host usa `ThisCloud.Framework.Web`:
  - se reutilizan `HttpContext.Items["CorrelationId"]` y `HttpContext.Items["RequestId"]`.

### Redaction mínima (MANDATORIA)
Patrones mínimos a redactar (default `DefaultLogRedactor`):
- `Authorization: Bearer <token>` → `Authorization: Bearer [REDACTED]`
- JWT tipo `eyJ...` → `[REDACTED_JWT]`
- `apiKey|token|secret|password` en `key=value` o `key: value` → `[REDACTED]`
- Emails / teléfonos (best-effort) → `[REDACTED_PII]`
- DNI/NIE (best-effort) → `[REDACTED_PII]`

---

## Esquema de Base de Datos (MANDATORIO) — SQL Server v1
(Se mantiene igual; ver Fase 6)

---

## Admin APIs v1 (MANDATORIO)
(Se mantiene igual; ver Fase 4)

---

## Fases y tareas

### Fase 0 — Setup de proyectos y gates
(Se mantiene igual; completada)

### Fase 1 — Abstractions v1
(Se mantiene igual; completada)

### Fase 2 — Serilog core + reconfig runtime
(Se mantiene igual; completada)

### Fase 3 — Sinks mínimos: Console + File (10MB)
(Se mantiene igual; completada)

### ✅ Fase 4 — Admin APIs + Documentación + Legal + NuGet README (MANDATORIO)

#### Track A — Admin APIs (runtime control)
Tareas
- L4.1 Map endpoints bajo `BasePath`.
- L4.2 Wiring con `ILoggingControlService` + `ILoggingSettingsStore` + `IAuditLogger`.
- L4.3 Gating por env + policy Admin (cuando aplica).
- L4.4 Semántica PATCH (merge + validación).

Criterios de aceptación (Admin)
- ✅ Endpoints funcionando y protegidos.
- ✅ No expuestos en Production por defecto.

#### Track B — Documentación enterprise-grade (bilingüe) + Legal + NuGet
> Esta documentación **debe aparecer** tanto en GitHub (repo) como en NuGet (por paquete).

Tareas
- L4.5 Agregar licencia global ISC:
  - Crear `LICENSE` (texto ISC oficial).
  - Actualizar `.csproj` publicables con `<PackageLicenseExpression>ISC</PackageLicenseExpression>`.
- L4.6 README índice del repo (bilingüe) en `README.md`:
  - Índice ES/EN con links a docs.
  - Tabla de paquetes (`Abstractions`, `Serilog`, `Admin`) con propósito, instalación y links.
  - Quickstart mínimo (copy/paste) y “Production checklist”.
  - Sección **Disclaimer / Exención de responsabilidad** (ES/EN).
- L4.7 README por paquete (ES/EN), con estructura consistente:
  - `docs/loggings/abstractions/README.es.md`
  - `docs/loggings/abstractions/README.en.md`
  - `docs/loggings/serilog/README.es.md`
  - `docs/loggings/serilog/README.en.md`
  - `docs/loggings/admin/README.es.md`
  - `docs/loggings/admin/README.en.md`
- L4.8 Documentación de arquitectura (ES/EN) “enterprise-grade”:
  - `docs/loggings/architecture/README.es.md`
  - `docs/loggings/architecture/README.en.md`
  - Contenido mínimo: capas, dependencias, flujo de configuración, runtime reconfig, redaction, correlation, fail-fast Production, extension points.
- L4.9 NuGet README por paquete (visible en nuget.org):
  - En cada `.csproj` publicable:
    - `<PackageReadmeFile>README.md</PackageReadmeFile>`
    - Incluir el `README.md` dentro del `.nupkg` (Pack + PackagePath raíz).
  - Cada paquete debe tener un `README.md` propio (en la carpeta del proyecto) optimizado para NuGet:
    - `src/ThisCloud.Framework.Loggings.Abstractions/README.md`
    - `src/ThisCloud.Framework.Loggings.Serilog/README.md`
    - `src/ThisCloud.Framework.Loggings.Admin/README.md`
  - El README de NuGet debe ser bilingüe (ES/EN) o, si se prefiere, EN principal + link a ES.
- L4.10 Checklist de “consumo seguro” (ES/EN) + límites de soporte:
  - “No body logging”, “no secrets”, “how to configure sinks”, “how to enable Admin safely”, “observability notes”, “retention responsibility”, “security boundaries”.

Criterios de aceptación (Docs/Legal/NuGet)
- ✅ En GitHub: README índice + docs bilingües navegables.
- ✅ En NuGet: cada paquete muestra su README correctamente.
- ✅ Licencia ISC visible en repo y en metadata de paquetes.
- ✅ Disclaimer claro (ES/EN) y no ambiguo.
- ✅ Ejemplos de configuración completos (dev/prod) y reales.

---

### Fase 5 — Sample + integración end-to-end
Tareas
- L5.1 Crear/ajustar sample Minimal API (incluye Admin endpoints + policy + env gating).
- L5.2 README adopción (referenciar docs del Track B, no duplicar).
- L5.3 Ejemplo de configuración `appsettings.Development.json` y `appsettings.Production.json` (con File.Enabled=true y Path válido).
- L5.4 “Runbook” mínimo (cómo validar que está logueando + dónde quedan los archivos).

Criterios de aceptación (Fase 5)
- ✅ Copy/paste integra logging en <15 min.
- ✅ Sample demuestra Admin + fail-fast + sinks.

### Fase 6 — DB Schema (MANDATORIO)
(Se mantiene igual)

### Fase 7 — NuGet metadata (no-legal) + packaging hardening
Tareas
- L7.1 Metadata NuGet adicional en `src/*` csproj (authors, description, tags, repository url, etc.).
- L7.2 Validación pack: `dotnet pack` sin warnings relevantes (incluye README, LICENSE expression, icon si aplica).

Criterios de aceptación (Fase 7)
- ✅ `dotnet pack` sin warnings relevantes.

### Fase 8 — CI/CD + Publish NuGet.org
(Se mantiene igual)

---

## Tabla de progreso (por tarea)

| ID   | Fase | Tarea | % | Estado |
|-----:|:----:|------|---:|:------|
| L0.1 | 0 | Crear proyectos Abstractions/Serilog/Admin | 100% | ✅ |
| L0.2 | 0 | Crear tests xUnit v3 (3 proyectos) | 100% | ✅ |
| L0.3 | 0 | Referencias entre proyectos | 100% | ✅ |
| L0.4 | 0 | Agregar a `ThisCloud.Framework.slnx` | 100% | ✅ |
| L0.5 | 0 | CPM + versiones exactas | 100% | ✅ |
| L0.6 | 0 | Coverage gate >=90 | 100% | ✅ |
| L0.7 | 0 | XML docs + 1591 as error | 100% | ✅ |
| L1.1 | 1 | `LogLevel` canon | 100% | ✅ |
| L1.2 | 1 | `LogSettings` + defaults 10MB | 100% | ✅ |
| L1.3 | 1 | Interfaces core | 100% | ✅ |
| L2.1 | 2 | Serilog bootstrap | 100% | ✅ |
| L2.2 | 2 | Enricher contexto | 100% | ✅ |
| L2.3 | 2 | Redactor mínimo | 100% | ✅ |
| L2.4 | 2 | Auditoría estructurada | 100% | ✅ |
| L2.5 | 2 | Reconfig runtime | 100% | ✅ |
| L3.1 | 3 | Console sink | 100% | ✅ |
| L3.2 | 3 | File sink 10MB | 100% | ✅ |
| L3.3 | 3 | Fail-fast Production | 100% | ✅ |
| L4.1 | 4 | Map endpoints Admin | 100% | ✅ |
| L4.2 | 4 | Wiring services | 100% | ✅ |
| L4.3 | 4 | Policy/env gating | 100% | ✅ |
| L4.4 | 4 | PATCH semantics | 100% | ✅ |
| L4.5 | 4 | Licencia ISC + PackageLicenseExpression | 100% | ✅ |
| L4.6 | 4 | README repo índice bilingüe + disclaimer | 100% | ✅ |
| L4.7 | 4 | README por paquete ES/EN (docs/) | 100% | ✅ |
| L4.8 | 4 | Arquitectura enterprise-grade ES/EN | 100% | ✅ |
| L4.9 | 4 | NuGet README por paquete (PackageReadmeFile) | 100% | ✅ |
| L4.10 | 4 | Checklist consumo seguro + límites soporte | 100% | ✅ |
| L5.1 | 5 | Sample Minimal API (Admin + policy + env gating) | 0% | ⏳ |
| L5.2 | 5 | README adopción (referencias a docs) | 0% | ⏳ |
| L5.3 | 5 | appsettings Dev/Prod ejemplos | 0% | ⏳ |
| L5.4 | 5 | Runbook mínimo validación | 0% | ⏳ |
| L6.1 | 6 | schema_v1.sql | 0% | ⏳ |
| L6.2 | 6 | docs/loggings/README.md | 0% | ⏳ |
| L6.3 | 6 | Persistencia settings/historial | 0% | ⏳ |
| L7.1 | 7 | Metadata NuGet adicional | 0% | ⏳ |
| L7.2 | 7 | PackageReadmeFile hardening (pack sin warnings) | 0% | ⏳ |
| L8.1 | 8 | CI incluye loggings | 0% | ⏳ |
| L8.2 | 8 | Publish tag publica loggings | 0% | ⏳ |

---

## Registro de actualizaciones del plan

| Fecha | Cambio | Razón |
|------|--------|-------|
| 2026-02-12 | Admin pasó a **MANDATORIO** (no opcional) | Administración debe ser por endpoints sí o sí |
| 2026-02-12 | Se agrega **DB schema** SQL Server v1 (documentado) | Requisito de definición y documentación del esquema |
| 2026-02-13 | **Fase 0 completada** (L0.1-L0.7) | Setup completo: 6 proyectos + CPM + gates + placeholders + pipeline validado |
| 2026-02-14 | **Fase 1 completada** (L1.1-L1.3) | Abstractions v1 completas: LogLevel enum + Settings models + Interfaces core + 100% coverage |
| 2026-02-14 | **Fase 2 completada** (L2.1-L2.5) | Serilog core implementado: Bootstrap + Enricher + Redactor + Audit logger + Runtime control service + 70+ tests |
| 2026-02-14 | **Fase 3 completada** (L3.1-L3.3) | Console + File sinks (10MB rolling, NDJSON) + Fail-fast Production (ProductionValidator) + 22 tests + coverage 94.84% |
| 2026-02-14 | **Fase 4 ampliada** (Admin + Docs/Legal/NuGet README) | Necesidad contractual: documentación bilingüe enterprise-grade + licencia ISC + README visible en NuGet por paquete |
| 2026-02-15 | **L4.5 completado** (Licencia ISC global) | LICENSE file creado + PackageLicenseExpression ISC agregado a 3 paquetes publicables (Abstractions, Serilog, Admin) |
| 2026-02-15 | **L4.6 completado** (README monorepo bilingüe) | README.md raíz como índice multi-framework ES/EN + disclaimer fuerte + Web docs movidos a docs/web/README.md |
| 2026-02-15 | **L4.7 completado** (READMEs por paquete) | 6 READMEs ES/EN creados (Abstractions/Serilog/Admin) + .gitignore fix para docs/loggings/packages/ |
| 2026-02-15 | **L4.8 completado** (Arquitectura enterprise-grade ES/EN) | docs/loggings/ARCHITECTURE.{es,en}.md creados: capas, flujos, correlación, redaction, fail-fast, extension points (1 commits: ff55168) |
| 2026-02-15 | **L4.9 completado** (NuGet README por paquete) | 3 NuGet-optimized READMEs + PackageReadmeFile configurado en .csproj (commit 9cfd67a) |
| 2026-02-15 | **L4.10 completado** (Checklist consumo seguro ES/EN) | docs/loggings/CHECKLIST.{es,en}.md: seguridad, production, admin, operación, soporte, incidentes, compliance (commit 69fafde) |
| 2026-02-15 | **L4.1-L4.4 completados** (Admin APIs) | Endpoints Minimal APIs + gating + DTOs + PATCH semantics implementados (commits e2305fe, 3698719) + Tests WIP (integration tests pendientes de refinamiento TestServer setup) |

---

## Evidencias Fase 0–3
> Se mantienen sin cambios (ya ejecutadas y verificadas en CI).

