# PLAN ThisCloud.Framework.Web — Web stack cross-cutting (Minimal APIs)

- Rama: `feature/thiscloud-framework-web`
- Versión: **1.0-framework.web.10**
- Fecha inicio: **2026-02-09**
- Última actualización: **2026-02-09**
- Estado global: ✅ **FASE 0 CERRADA** (W0.1–W0.6 completado; listo para Fase 1 — NO iniciar)

## Objetivo
Entregar un framework web **Copilot-ready** (sin ambigüedades) para:
- Contratos HTTP estandarizados (envelope + ProblemDetails)
- Respuestas y errores coherentes (Top5 status codes + set extendido)
- Correlation/Request Id (headers + meta)
- Middlewares base (exception mapping, correlation)
- CORS / Compression / Cookies policy por configuración
- OpenAPI/Swagger (Swashbuckle) con seguridad y convenciones
- Cobertura mínima mandatoria >=90% (fallar build si baja)

## Alcance
Paquetes (DECISIÓN CERRADA):
1) `ThisCloud.Framework.Contracts` (net10.0)
2) `ThisCloud.Framework.Web` (net10.0)

Fuera de alcance (pero se integra):
- AuthN (emisión tokens) y RBAC completo (lo define el host según `MiPerfil_SEGURIDAD.md`).
- Observability.Logging (plan separado).


## DECISIÓN CERRADA: Testing
- Framework de tests único: **xUnit.net v3** (`xunit.v3`). No se permiten MSTest/NUnit en ThisCloud.Framework.

## NuGet y versiones (DECISIÓN CERRADA)
### Runtime (src)
- `Swashbuckle.AspNetCore` **10.1.2**

### Testing (tests)
- `Microsoft.AspNetCore.Mvc.Testing` **10.0.2**
- `Microsoft.NET.Test.Sdk` **18.0.1**
- `xunit.v3` **3.2.2**
- `xunit.runner.visualstudio` **3.1.5**
- `coverlet.msbuild` **6.0.4** (enforce threshold)
- `FluentAssertions` **7.2.0** (se fija en v7.x)

### CLI opcional (no mandatorio)
- `dotnet-reportgenerator-globaltool` **5.5.1** (HTML report)

## Estructura de repositorio (DECISIÓN CERRADA)
- `ThisCloud.Framework.slnx`
- `src/ThisCloud.Framework.Contracts/ThisCloud.Framework.Contracts.csproj`
- `src/ThisCloud.Framework.Web/ThisCloud.Framework.Web.csproj`
- `tests/ThisCloud.Framework.Contracts.Tests/ThisCloud.Framework.Contracts.Tests.csproj`
- `tests/ThisCloud.Framework.Web.Tests/ThisCloud.Framework.Web.Tests.csproj`
- `samples/ThisCloud.Sample.MinimalApi/ThisCloud.Sample.MinimalApi.csproj`

## API pública del framework (DECISIÓN CERRADA)
### Namespaces y clases públicas
- `ThisCloud.Framework.Contracts.Web`
  - `ApiEnvelope<T>`
  - `Meta`
  - `ProblemDetailsDto`
  - `ErrorItem`
  - `PagedResult<T>`, `PaginationMeta`
  - `ErrorCode` (const string)
  - `ThisCloudHeaders` (const string)
- `ThisCloud.Framework.Contracts.Exceptions`
  - `ThisCloudException` (base)
  - `ValidationException`
  - `NotFoundException`
  - `ConflictException`
  - `ForbiddenException`
- `ThisCloud.Framework.Web`
  - `ThisCloudWebOptions` + sub-options (`CorsOptions`, `SwaggerOptions`, `CookiesOptions`, `CompressionOptions`)
  - `ServiceCollectionExtensions` (Add*)
  - `ApplicationBuilderExtensions` (Use*)
  - `ThisCloudResults` (helpers IResult)
  - Middlewares: `CorrelationIdMiddleware`, `RequestIdMiddleware`, `ExceptionMappingMiddleware`

### Signatures exactas
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThisCloudFrameworkWeb(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName);
}

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseThisCloudFrameworkWeb(this WebApplication app);
    public static WebApplication UseThisCloudFrameworkSwagger(this WebApplication app);
}
```

## Versionado NuGet (MANDATORIO) — Flow autoincremental (DECISIÓN CERRADA)

### Herramienta
- `Nerdbank.GitVersioning` **3.9.50** (MSBuild integration)
- Tool CLI opcional (recomendado en CI): `nbgv` **3.9.50**

### Reglas (sin excepciones)
- **Prohibido** setear `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion` manualmente en `.csproj`.
- La versión se calcula **automáticamente** desde Git y `version.json`.
- Cada commit en `main` produce una versión **única** (auto-increment via git height).

### Artefactos requeridos
1) `version.json` en la raíz del repo (o de la solución `ThisCloud.Framework.slnx`), ejemplo mínimo:
```json
{
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+\\.\\d+\\.\\d+$"
  ],
  "cloudBuild": {
    "buildNumber": {
      "enabled": true
    }
  }
}
```

2) `Directory.Build.props` (raíz) — enforce de versioning:
```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Nerdbank.GitVersioning" Version="3.9.50" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

3) `Directory.Packages.props` (si se usa Central Package Management) con versiones **fijas** (sin flotantes).

### Flow de releases (autoincrement + tag)
- Release estable: crear tag `vMAJOR.MINOR.PATCH` apuntando a commit de `main`.
- CI ejecuta `dotnet pack -c Release` y el paquete sale con esa versión.
- Para preparar release (opcional): `nbgv prepare-release` (solo equipo release).

### Criterios de aceptación (versioning)
- ✅ `dotnet pack` genera `.nupkg` con versión **derivada** de Git (sin modificar csproj).
- ✅ Dos commits consecutivos en `main` generan **dos versiones distintas**.
- ✅ Al crear tag `vX.Y.Z` la versión del paquete coincide con `X.Y.Z`.



## Git Flow (MANDATORIO) — reglas operativas (no ambiguas)

- Ramas base:
  - `main`: solo releases estables (tags `vX.Y.Z`).
  - `develop`: integración continua de features.
- Ramas de trabajo:
  - `feature/<id>-<slug>` (ej. `feature/W3-correlation-middleware`)
  - `release/<version>` (opcional para preparar release)
  - `hotfix/<id>-<slug>` (si hay incidente en producción)
- Reglas:
  - ❌ Prohibido commitear directo a `main` o `develop`.
  - ✅ Todo cambio entra por **Pull Request** con CI verde (build + tests + coverage >=90%).
  - ✅ PR debe referenciar IDs del plan (ej. `W3.1`, `W4.1`) en título o descripción.
  - ✅ Merge recomendado: **squash** (1 PR = 1 unidad lógica).  


## Configuración (MANDATORIO)
Key root: `ThisCloud:Web`
- `ServiceName` (string, requerido en Production)
- `Cors.Enabled` (bool)
- `Cors.AllowedOrigins` (string[], requerido si Enabled=true en Production)
- `Cors.AllowCredentials` (bool)
- `Compression.Enabled` (bool)
- `Cookies.SecurePolicy` (`Always|SameAsRequest|None`)
- `Cookies.SameSite` (`Strict|Lax|None`)
- `Cookies.HttpOnly` (bool)
- `Swagger.Enabled` (bool)
- `Swagger.RequireAdmin` (bool)
- `Swagger.AllowedEnvironments` (string[])

Validación mandatoria:
- Production + Cors.Enabled=true => AllowedOrigins no vacío y sin `"*"` si AllowCredentials=true.
- Production => Cookies.SecurePolicy=Always.

## WebContracts v1 (MANDATORIO)
### Headers estándar
- `X-Correlation-Id` GUID
- `X-Request-Id` GUID

Reglas:
- Si header falta o no es GUID => generar GUID nuevo.
- Siempre devolver ambos headers en la respuesta.

### Envelope estándar
Todas las respuestas JSON:
```json
{
  "meta": {
    "service": "users",
    "version": "v1",
    "timestampUtc": "2026-02-09T00:00:00Z",
    "correlationId": "GUID",
    "requestId": "GUID",
    "traceId": "W3C TraceId"
  },
  "data": { },
  "errors": []
}
```

### Status codes mandatorios
**Top5 mínimo (obligatorio en OpenAPI y en tests):**
- 200 OK
- 201 Created (+ Location)
- 303 SeeOther
- 400 BadRequest
- 502 BadGateway

**Set extendido (soportado por el framework y testeado donde aplique):**
- 401 Unauthorized
- 403 Forbidden
- 404 NotFound
- 409 Conflict
- 500 InternalServerError
- 504 GatewayTimeout

Regla: evitar 204 en endpoints JSON (se estandariza 200 con `data=null`).

### Errores (ProblemDetailsDto)
- Se retorna siempre dentro de `errors[]` (mínimo 1 item).
- `errors[0].status` coincide con HTTP status.
- `errors[0].code` usa `ErrorCode.*`.
- Validación: `extensions.errors` con diccionario campo → mensajes.

## Exception → HTTP mapping (MANDATORIO)
- `ValidationException` → 400 + `ErrorCode.VALIDATION_ERROR`
- `NotFoundException` → 404 + `ErrorCode.NOT_FOUND`
- `ConflictException` → 409 + `ErrorCode.CONFLICT`
- `ForbiddenException` → 403 + `ErrorCode.FORBIDDEN`
- `UnauthorizedAccessException` → 401 + `ErrorCode.UNAUTHORIZED`
- `HttpRequestException` (downstream) → 502 + `ErrorCode.UPSTREAM_FAILURE`
- `TimeoutException` → 504 + `ErrorCode.UPSTREAM_TIMEOUT`
- `Exception` (default) → 500 + `ErrorCode.UNHANDLED_ERROR`

## Fases y tareas

### Fase 0 — Setup de solución y paquetes

> Regla operativa: **NO avanzar a Fase 1** hasta que W0.5 pase (coverage line >=90) y el comando `dotnet test ... Threshold=90` sea OK.
Tareas
- W0.1 Crear los 2 proyectos `Contracts` y `Web` (Class Library net10.0, `IsPackable=true`).
- W0.2 Crear 2 proyectos de tests (xUnit v3 net10.0).
- W0.3 Configurar referencias:
  - `Web` referencia `Contracts`.
  - Tests referencian su proyecto target.
- W0.4 Agregar NuGet exactos (arriba) en cada csproj.
- W0.4B Versioning autoincremental:
  - crear `version.json` (raíz)
  - crear/ajustar `Directory.Build.props` (raíz) con `Nerdbank.GitVersioning` 3.9.50
  - (opcional CI) instalar `nbgv` 3.9.50 como dotnet tool.
- W0.5 Script estándar de tests con threshold:
  - `dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line`
- W0.6 XML docs mandatorio:
  - `GenerateDocumentationFile=true` en `src/*`
  - warning **1591** tratado como error SOLO en `src/*` (no tests/samples)

Tests
- TW0. Verifica que un test dummy pasa y coverage threshold se ejecuta (pipeline local).

Criterios de aceptación (Fase 0)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Existe `version.json` y NBGV está activo.
- ✅ `dotnet pack` produce versión derivada de Git.
- ✅ Todos los csproj net10.0.
- ✅ `dotnet test` falla si coverage <90 (se prueba bajando un test intencionalmente).
- ✅ No hay versiones flotantes (`*`, `>=`) en PackageReference.

Fase 0 — estado por tarea (porcentaje / verificación)
- W0.1 Crear proyectos `Contracts` y `Web`: 100% (files present, build verified)
- W0.2 Crear proyectos de tests: 100% (test projects present)
- W0.3 Configurar referencias (`Web` → `Contracts`): 100% (ProjectReference present, build verified)
- W0.4 Agregar NuGet exactos (runtime/swagger): 100% (Swashbuckle.AspNetCore 10.1.2 added, restore+build verified)
- W0.4B Versionado autoincremental (version.json + Directory.Build.props): 100% (files present at repo root, `dotnet pack` verified)
- W0.5 Script estándar tests con threshold (coverage >=90): PENDING (tests added and expanded; `dotnet test` runs produce coverage output but threshold run failed — additional test coverage or instrumentation required)
 - W0.5 Script estándar tests con threshold (coverage >=90): 100% (Completado — `dotnet test` con /p:Threshold=90 pasó y cobertura verificada en Release)
- W0.6 XML docs mandatorio + 1591 as error: 100% (GenerateDocumentationFile and WarningsAsErrors applied; build verified)

### Fase 1 — Contracts v1 (modelos + exceptions)
Tareas
- W1.1 `ThisCloudHeaders`:
  - `public const string CorrelationId = "X-Correlation-Id";`
  - `public const string RequestId = "X-Request-Id";`
- W1.2 `Meta` (record/class):
  - `string Service`, `string Version`, `DateTime TimestampUtc`, `Guid CorrelationId`, `Guid RequestId`, `string TraceId`
- W1.3 `ApiEnvelope<T>`:
  - `Meta Meta`, `T? Data`, `IReadOnlyList<ErrorItem> Errors`
- W1.4 `ProblemDetailsDto` + `ErrorItem`:
  - `type`, `title`, `status`, `detail`, `instance`, `extensions` (Dictionary<string, object>)
- W1.5 Exceptions (`ThisCloudException` + derivados) con:
  - `public string Code { get; }`
  - `public int Status { get; }`
  - `public IDictionary<string,string[]>? ValidationErrors { get; }` (solo ValidationException)

Tests (>=90%)
- TW1.1 Serialización/deserialización envelope.
- TW1.2 Serialización ProblemDetailsDto con `extensions`.
- TW1.3 Exceptions: constructores, Code/Status.

Criterios de aceptación (Fase 1)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ El contrato JSON es estable (snapshot tests opcional).
- ✅ Exceptions “transportables” (sin dependencias web).

### Fase 2 — Options + validación + DI
Tareas
- W2.1 `ThisCloudWebOptions` + sub-options.
- W2.2 `AddThisCloudFrameworkWeb(...)`:
  - Bind `ThisCloud:Web`
  - ValidateOptions (throw en startup si inválido)
  - Registrar `ProblemDetails` (built-in) solo para compatibilidad, pero el contrato se produce con nuestros DTOs.
- W2.3 Registrar CORS/Compression/Cookies según options.

Tests (>=90%)
- TW2.1 Options validation: Production + config insegura => throw.
- TW2.2 Options binding: valores por defecto correctos.

Criterios de aceptación (Fase 2)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ No hay “magic strings” fuera de opciones.
- ✅ Startup falla rápido en config inválida (fail-fast).

### Fase 3 — Middlewares (Correlation/RequestId)
Tareas
- W3.1 `CorrelationIdMiddleware`
  - Input: header `X-Correlation-Id`
  - Output: asegura GUID y escribe response header
  - Guarda `Guid` en `HttpContext.Items["CorrelationId"]`
- W3.2 `RequestIdMiddleware` (idéntico)
- W3.3 Helper `ThisCloudHttpContext`:
  - `GetCorrelationId(HttpContext)`
  - `GetRequestId(HttpContext)`
  - `GetTraceId(HttpContext)` (Activity.Current?.TraceId)

Tests (>=90%)
- TW3.1 Cuando header viene válido → se preserva.
- TW3.2 Cuando header viene inválido → se reemplaza (GUID nuevo).
- TW3.3 Response headers siempre presentes.

Criterios de aceptación (Fase 3)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Correlation/Request IDs consistentes durante el request.
- ✅ No se rompe si Activity es null.

### Fase 4 — Exception mapping + Results helpers
Tareas
- W4.1 `ExceptionMappingMiddleware`
  - catch global
  - mapea por tabla mandatoria (arriba)
  - retorna `ApiEnvelope<object?>` con `errors[0]` = `ProblemDetailsDto`
- W4.2 `ThisCloudResults` (static):
  - `Ok<T>(T data)`
  - `Created<T>(string location, T data)`
  - `SeeOther(string location)`
  - `BadRequest(code, title, detail, validationErrors?)`
  - `UpstreamFailure(detail?)` (502)
  - `Unauthorized(detail?)` (401)
  - `Forbidden(detail?)` (403)
  - `NotFound(detail?)` (404)
  - `Conflict(detail?)` (409)
  - `Unhandled(detail?)` (500)
  - `UpstreamTimeout(detail?)` (504)
- W4.3 Regla de uso (MANDATORIA)
  - En endpoints: **NO** usar `Results.Ok(...)` directo.
  - Siempre usar `ThisCloudResults.*`.

Tests (>=90%)
- TW4.1 Un endpoint que tira `ValidationException` devuelve 400 + envelope.
- TW4.2 Un endpoint que tira `HttpRequestException` devuelve 502 + envelope.
- TW4.3 Envelope incluye meta (ids + traceId).

CWarning (no negociable)
- Si un equipo ignora `ThisCloudResults` y retorna JSON “raw”, se considera bug de arquitectura (rechazar PR).

Criterios de aceptación (Fase 4)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Ningún error sale sin envelope.
- ✅ `errors[0].code/status/title` consistentes.

### Fase 5 — CORS / Compression / Cookies (end-to-end)
Tareas
- W5.1 Aplicar policy `ThisCloudDefaultCors` si Enabled.
- W5.2 Aplicar `ResponseCompression` si Enabled.
- W5.3 Aplicar `CookiePolicy` siempre (con defaults seguros).

Tests (>=90%)
- TW5.1 CORS: origin permitido => headers presentes; no permitido => no headers.
- TW5.2 Compression: response comprimida cuando corresponde (mínimo “Content-Encoding” presente).
- TW5.3 Cookies: en Production => SecurePolicy Always.

Criterios de aceptación (Fase 5)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ No se puede “abrir CORS” por accidente en Production.

### Fase 6 — Swagger (Swashbuckle) + protección
Tareas
- W6.1 `UseThisCloudFrameworkSwagger()`:
  - `app.UseSwagger()`
  - `app.UseSwaggerUI(c => c.RoutePrefix = "swagger")`
- W6.2 Generación:
  - `services.AddEndpointsApiExplorer()`
  - `services.AddSwaggerGen(...)`
  - Documentar Top5 status codes por convención
- W6.3 Seguridad:
  - Agregar scheme Bearer
  - Si `Swagger.RequireAdmin=true`:
    - Proteger path `/swagger` y `/swagger/*` con `IAuthorizationService` policy `"Admin"` (host la define)
- W6.4 Gating por ambiente:
  - Solo habilitar si env ∈ `Swagger.AllowedEnvironments`

Tests (>=90%)
- TW6.1 Swagger disabled => 404.
- TW6.2 Env no permitido => 404.
- TW6.3 RequireAdmin => 403 sin claims admin; 200 con admin.

Criterios de aceptación (Fase 6)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Swagger no expuesto en Production por defecto.
- ✅ Swagger no público sin admin cuando RequireAdmin=true.

### Fase 7 — Sample y guía de adopción
Tareas
- W7.1 `samples/ThisCloud.Sample.MinimalApi`:
  - `builder.Services.AddThisCloudFrameworkWeb(...)`
  - `app.UseThisCloudFrameworkWeb()`
  - 3 endpoints: OK, Created, ValidationException
- W7.2 README del paquete:
  - checklist de adopción
  - snippet Program.cs mínimo
  - appsettings ejemplo completo
  - “Reglas mandatorias” (ThisCloudResults, headers, swagger gating)
- W7.3 `Directory.Packages.props` (si aplica) con versiones exactas.

Tests (>=90%)
- TW7. Smoke tests sample con WebApplicationFactory.

Criterios de aceptación (Fase 7)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Copiar/pegar permite levantar un microservicio estándar en <15 min.


### Fase 8 — GitHub Packages (NuGet) + CI/CD (GitHub Actions) + Dependabot (MANDATORIO)
> Nota: Fase 8 ya tiene archivos creados, pero **no se avanza** hasta cerrar Fase 0 (W0.5).
**Objetivo:** publicar `ThisCloud.Framework.*` en **GitHub Packages (NuGet)** y automatizar:
- PR → ejecuta CI (build+tests+coverage>=90).
- Merge a `main` → **pack + publish** automático del NuGet.
- Dependabot mantiene actualizados NuGet y GitHub Actions.

#### Reglas (no ambiguas)
- El branch de release es **`main`**.
  - Si el repo hoy usa `master`, **primero** se renombra a `main` (tarea W8.1).
- Publicación **NO** se hace en `pull_request` (por seguridad y permisos). Se hace en `push` a `main` (merge del PR).
- Publicación a GitHub Packages usa **`GITHUB_TOKEN`** con permisos `contents:read` y `packages:write`.
- Los paquetes pueden ser **públicos**, pero **requieren autenticación para instalar** (developers usan PAT classic `read:packages` en su máquina).

#### Tareas
- W8.1 Alinear Git Flow con branch `main`:
  - Renombrar `master`→`main` si aplica.
  - Asegurar branches `develop` y `feature/*` según sección Git Flow.
- W8.2 Branch protection (repo settings):
  - Requerir PR para `main` (sin pushes directos).
  - Requerir checks: `CI` (build+tests+coverage).
- W8.3 Preparar metadata NuGet en todos los csproj `src/*`:
  - `RepositoryUrl` (link al repo) y `RepositoryType=git` (vincula el package al repo).
  - `PackageId`, `Authors`, `Company`, `Description`, `PackageTags`.
  - `GenerateDocumentationFile=true` (ya mandatorio en plan).
- W8.4 Crear workflow **CI**: `.github/workflows/ci.yml`
  - Trigger: `pull_request` hacia `develop` y `main`.
  - Steps: checkout, setup-dotnet 10.x, restore, build, test + coverage threshold.
- W8.5 Crear workflow **Publish**: `.github/workflows/publish.yml`
  - Trigger: `push` a `main` + `workflow_dispatch`.
  - Permisos:
    - `permissions: { contents: read, packages: write }`
  - Requisito: `actions/checkout` con `fetch-depth: 0` (NBGV necesita historial).
  - Steps: pack Release a `./artifacts`, agregar source GitHub Packages con `dotnet nuget add source`, push `*.nupkg` con `--skip-duplicate`.
- W8.6 Completar `.github/dependabot.yml`:
  - Ecosystems: `nuget` + `github-actions`.
  - `nuget` directories: `/src`, `/tests`, `/samples`.
  - Schedule: weekly, `open-pull-requests-limit: 10`.
- W8.7 Plantilla local de consumo (sin secretos en repo):
  - Crear `nuget.config.template` (sin credenciales) + instrucciones en README:
    - comando `dotnet nuget add source ...` con PAT classic para dev.

#### Workflows (copiar/pegar) — DECISIÓN CERRADA

**`.github/workflows/ci.yml`**
```yaml
name: CI

on:
  pull_request:
    branches: [ "develop", "main" ]

jobs:
  build_test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Restore
        run: dotnet restore ThisCloud.Framework.slnx
      - name: Build
        run: dotnet build ThisCloud.Framework.slnx -c Release --no-restore
      - name: Test (coverage >=90)
        run: dotnet test ThisCloud.Framework.slnx -c Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line
```

**`.github/workflows/publish.yml`**
```yaml
name: Publish NuGet (GitHub Packages)

on:
  push:
    branches: [ "main" ]
  workflow_dispatch:

permissions:
  contents: read
  packages: write

jobs:
  pack_and_push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore ThisCloud.Framework.slnx

      - name: Pack (Release)
        run: dotnet pack ThisCloud.Framework.slnx -c Release -o ./artifacts

      - name: Add GitHub Packages source (GITHUB_TOKEN)
        run: dotnet nuget add source --username ${{ github.actor }} --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text --name github "https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json"

      - name: Push packages
        run: dotnet nuget push "./artifacts/*.nupkg" --source "github" --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate
```

**`.github/dependabot.yml`** (base)
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/src"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10

  - package-ecosystem: "nuget"
    directory: "/tests"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10

  - package-ecosystem: "nuget"
    directory: "/samples"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

#### Tests (>=90%)
- TW8.1 PR a `main` ejecuta `CI` y pasa (incluye coverage).
- TW8.2 Merge del PR a `main` ejecuta `Publish` y publica `.nupkg` en GitHub Packages.

#### Criterios de aceptación (Fase 8)
- ✅ Cumple Git Flow (branch `feature/*`, PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Existe `CI` en PR (`develop`/`main`) con build+tests+coverage>=90.
- ✅ Existe `Publish` en `push` a `main` y publica en GitHub Packages (NuGet).
- ✅ Version del paquete es autoincremental (NBGV) y **cambia** entre commits.
- ✅ Dependabot crea PRs semanales para `nuget` y `github-actions`.
- ✅ No hay tokens/credenciales commiteados en repo.


## Cuadro de fases x tareas (planificación y estado)

> Estado: ⏳ Pendiente | 🟡 En progreso | ✅ Completado  
> Regla: **NO** marcar ✅ sin verificación CLI (`dotnet build/test/pack` + archivos visibles en repo root).

| Fase | ID   | Tarea (descripción) | % | Estado |
|:----:|:----:|----------------------|--:|:------|
| 0 | W0.1 | Crear proyectos `Contracts` y `Web` (Class Library net10.0, IsPackable=true) | 100% | ✅ Completado |
| 0 | W0.2 | Crear proyectos de tests xUnit v3 (Contracts.Tests/Web.Tests) | 100% | ✅ Completado |
| 0 | W0.3 | Configurar referencias (`Web` → `Contracts`; tests → targets) | 100% | ✅ Completado |
| 0 | W0.4 | Agregar NuGet con versiones exactas (sin flotantes) | 100% | ✅ Completado |
| 0 | W0.4B | Versionado autoincremental: `version.json` + `Directory.Build.props` (NBGV) | 100% | ✅ Completado |
| 0 | W0.5 | Script `dotnet test` con threshold line>=90 (coverlet.msbuild) | 0% | 🟡 En progreso (falla threshold) |
| 0 | W0.6 | XML docs mandatorio (GenerateDocumentationFile + warning 1591 como error) | 100% | ✅ Completado |
| 1 | W1.1 | `ThisCloudHeaders` (const strings) | 0% | ⏳ Pendiente |
| 1 | W1.2 | `Meta` (service/version/timestampUtc/correlationId/requestId/traceId) | 0% | ⏳ Pendiente |
| 1 | W1.3 | `ApiEnvelope<T>` (Meta/Data/Errors) | 0% | ⏳ Pendiente |
| 1 | W1.4 | `ProblemDetailsDto` + `ErrorItem` + extensions (code/errors) | 0% | ⏳ Pendiente |
| 1 | W1.5 | Exceptions: `ThisCloudException` + derivados (Validation/NotFound/Conflict/Forbidden) | 0% | ⏳ Pendiente |
| 2 | W2.1 | `ThisCloudWebOptions` + sub-options (Cors/Swagger/Cookies/Compression) | 0% | ⏳ Pendiente |
| 2 | W2.2 | `AddThisCloudFrameworkWeb(...)` (bind + validate + register services) | 0% | ⏳ Pendiente |
| 2 | W2.3 | Registrar CORS/Compression/Cookies según options | 0% | ⏳ Pendiente |
| 3 | W3.1 | `CorrelationIdMiddleware` (parse/generate + response header + Items) | 0% | ⏳ Pendiente |
| 3 | W3.2 | `RequestIdMiddleware` (idem) | 0% | ⏳ Pendiente |
| 3 | W3.3 | Helper `ThisCloudHttpContext` (GetCorrelationId/GetRequestId/GetTraceId) | 0% | ⏳ Pendiente |
| 4 | W4.1 | `ExceptionMappingMiddleware` (tabla mandatoria → envelope+ProblemDetailsDto) | 0% | ⏳ Pendiente |
| 4 | W4.2 | `ThisCloudResults` helpers (200/201/303/400/502 + extendidos) | 0% | ⏳ Pendiente |
| 4 | W4.3 | Regla mandatoria: endpoints deben usar `ThisCloudResults` (no `Results.*` raw) | 0% | ⏳ Pendiente |
| 5 | W5.1 | Aplicar policy `ThisCloudDefaultCors` si Enabled | 0% | ⏳ Pendiente |
| 5 | W5.2 | Aplicar `ResponseCompression` si Enabled | 0% | ⏳ Pendiente |
| 5 | W5.3 | Aplicar `CookiePolicy` siempre (defaults seguros) | 0% | ⏳ Pendiente |
| 6 | W6.1 | `UseThisCloudFrameworkSwagger()` (UseSwagger + UseSwaggerUI) | 0% | ⏳ Pendiente |
| 6 | W6.2 | `AddSwaggerGen` + `AddEndpointsApiExplorer` + convenciones (Top5 + schemas) | 0% | ⏳ Pendiente |
| 6 | W6.3 | Seguridad Swagger: Bearer scheme + `RequireAdmin` (policy "Admin") | 0% | ⏳ Pendiente |
| 6 | W6.4 | Gating por ambientes (`AllowedEnvironments`) | 0% | ⏳ Pendiente |
| 7 | W7.1 | Sample `ThisCloud.Sample.MinimalApi` (OK/Created/ValidationException) | 0% | ⏳ Pendiente |
| 7 | W7.2 | README copiable + checklist adopción + appsettings completo | 0% | ⏳ Pendiente |
| 7 | W7.3 | `Directory.Packages.props` (si aplica) con versiones exactas | 0% | ⏳ Pendiente |
| 8 | W8.1 | Alinear Git Flow con branch `main` (renombrar master→main si aplica) | 0% | ⏳ Pendiente |
| 8 | W8.2 | Branch protection: PR obligatorio + checks requeridos | 0% | ⏳ Pendiente |
| 8 | W8.3 | Metadata NuGet en csproj (RepositoryUrl, PackageId, etc.) | 0% | ⏳ Pendiente |
| 8 | W8.4 | Workflow CI (`.github/workflows/ci.yml`) | 50% | 🟡 En progreso |
| 8 | W8.5 | Workflow Publish (`.github/workflows/publish.yml`) | 50% | 🟡 En progreso |
| 8 | W8.6 | dependabot.yml (nuget + github-actions, multi-directorio) | 50% | 🟡 En progreso |
| 8 | W8.7 | `nuget.config.template` + instrucciones README (sin secretos) | 0% | ⏳ Pendiente |


## Criterios de aceptación globales (mandatorios)
- ✅ CI/CD GitHub Packages: workflow `CI` en PR y workflow `Publish` en merge a `main` (push a main) publicando NuGet.
- ✅ Cumplimiento de Git Flow (PR obligatorio, CI verde, sin commits directos a main/develop).
- ✅ Versionado NuGet autoincremental con Nerdbank.GitVersioning (sin versiones hardcodeadas en csproj).
- ✅ Coverage line >=90, enforced por build (coverlet.msbuild threshold).
- ✅ Todas las respuestas JSON con envelope (`ApiEnvelope<T>`).
- ✅ CorrelationId/RequestId headers siempre presentes (request y response).
- ✅ Exception mapping completo según tabla (incluye 400/401/403/404/409/502/504/500).
- ✅ Swagger protegido y gateado por env/config/policy.
- ✅ Documentación técnica **obligatoria**:
  - En `src/*`: **XML docs** (`/// <summary>...`) para **todo tipo público** y **todo miembro público**.
  - En csproj: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` y build falla si faltan docs (warning 1591 como error).
- ✅ Trazabilidad para rollback (sin comentarios manuales en código):
  - La “fecha/autor/versión” se obtiene de **Git + tags** (NBGV) y SourceLink del repositorio (historial es la fuente de verdad).
  - Criterio verificable: cada release tiene tag `vX.Y.Z` y el `.nupkg` coincide.

## Nota de alcance (importante)
- ❗ EF / Database-first / scaffolding / esquemas de DB **NO** pertenecen a `ThisCloud.Framework.Web`. Eso se define en el plan de persistencia (`Plan_ThisCloud_Framework_Persistence_v1.md`) y sus adapters.
