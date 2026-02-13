# RUNBOOK — Validación Operativa (ThisCloud.Sample.Loggings.MinimalApi)

Este runbook documenta cómo **validar end-to-end** que el logging framework está funcionando correctamente en el sample.

---

## ⚠️ Prerequisito: Configurar SAMPLE_ADMIN_APIKEY

**ANTES de ejecutar los tests de Admin endpoints**, debes configurar la API key como variable de entorno o user-secrets.

**⚠️ NUNCA uses valores reales en este documento. Los ejemplos son ilustrativos.**

```bash
# Opción A: Variable de entorno (recomendado para tests locales)
export SAMPLE_ADMIN_APIKEY="my-local-test-key-12345"  # Ejemplo - usa tu propio valor

# Opción B: User secrets
dotnet user-secrets init --project samples/ThisCloud.Sample.Loggings.MinimalApi/
dotnet user-secrets set "SAMPLE_ADMIN_APIKEY" "my-local-test-key-12345" --project samples/ThisCloud.Sample.Loggings.MinimalApi/
```

> 🔒 **Seguridad**: Nunca versiones API keys. Rotalas regularmente. Usa Azure Key Vault en producción.

---

## 📋 Checklist de Validación

### 1. Build y arranque
```bash
cd F:\Proyectos\ThisCloudServices\03-Repo\ThisCloud.Framework
dotnet build samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj -c Release

# Asegúrate de tener SAMPLE_ADMIN_APIKEY configurada antes de ejecutar
dotnet run --project samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj --environment=Development
```

**Verificar**:
- ✅ Build OK sin warnings.
- ✅ Arranca sin excepciones.
- ✅ Logs aparecen en consola (si `Console.Enabled=true` en Development).

---

### 2. Verificar archivo de logs (File sink)
**Ubicación por defecto**: `samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-<date>.ndjson`

```bash
# Verificar que el archivo se creó
ls samples/ThisCloud.Sample.Loggings.MinimalApi/logs/

# Ver últimas 10 líneas (NDJSON)
tail -10 samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-*.ndjson
```

**Verificar**:
- ✅ Archivo `log-YYYYMMDD.ndjson` existe.
- ✅ Cada línea es un JSON válido (NDJSON format).
- ✅ Propiedades estándar presentes: `@t`, `@l`, `@mt`, `service`, `env`, `correlationId`, etc.

---

### 3. Verificar rotación por tamaño (10MB)
**Default**: `RollingFileSizeMb=10` (cada archivo no supera 10MB).

```bash
# Forzar logs masivos (loop)
for i in {1..100000}; do
  curl -k -s https://localhost:7001/health > /dev/null
done

# Verificar tamaño de archivos
ls -lh samples/ThisCloud.Sample.Loggings.MinimalApi/logs/
```

**Verificar**:
- ✅ Cuando un archivo alcanza ~10MB, se crea un nuevo archivo numerado (ej: `log-20260215_001.ndjson`).
- ✅ Los archivos más antiguos se eliminan si superan `RetainedFileCountLimit` (default 30).

---

### 4. Verificar correlationId en logs
**CorrelationId** debe estar presente en todas las líneas de log para la misma request.

```bash
# Request con correlationId personalizado
curl -k https://localhost:7001/health \
  -H "X-Correlation-Id: my-custom-correlation-12345"

# Buscar en logs
grep "my-custom-correlation-12345" samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-*.ndjson
```

**Verificar**:
- ✅ El `correlationId` aparece en los logs de esa request.
- ✅ Si no se envía el header, se genera automáticamente un GUID (si `GenerateIfMissing=true`).

---

### 5. Verificar redaction (secretos/JWT)
**Redaction** debe ocultar tokens/secretos/PII en logs.

**Probar** (endpoint que loguee un string tipo JWT ficticio):
```bash
curl -k https://localhost:7001/api/data \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fakepayload.fakesignature"
```

**Verificar en logs**:
```bash
grep "Authorization" samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-*.ndjson
```

**Esperado**:
- ✅ `Authorization: Bearer [REDACTED]` (no el JWT completo).
- ✅ Strings tipo `eyJ...` → `[REDACTED_JWT]`.

> **Nota**: El sample NO loguea headers por defecto (seguridad). Para probar redaction, puedes agregar temporalmente un log controlado con un JWT ficticio.

---

### 6. Verificar Admin endpoints (Development)
**Configuración por defecto en Development**:
- `Admin.Enabled=true`
- `AllowedEnvironments=["Development"]`
- `RequireAdmin=true` → Policy "Admin" con header `X-Admin-ApiKey`

**⚠️ Reemplaza `$SAMPLE_ADMIN_APIKEY` con el valor que configuraste en el prerequisito.**

#### 6.1 GET settings (sin auth → 401/403)
```bash
curl -k -i https://localhost:7001/api/admin/logging
```

**Esperado**: `401 Unauthorized` o `403 Forbidden` (sin header).

#### 6.2 GET settings (con auth → 200)
```bash
# Usa tu API key desde variable de entorno
curl -k https://localhost:7001/api/admin/logging \
  -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY"
```

**Esperado**:
```json
{
  "isEnabled": true,
  "minimumLevel": "Debug",
  "console": { "enabled": true },
  "file": { "enabled": true, "path": "logs/log-.ndjson", ... },
  ...
}
```

#### 6.3 PATCH settings (cambiar nivel runtime)
```bash
curl -k https://localhost:7001/api/admin/logging \
  -X PATCH \
  -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY" \
  -H "Content-Type: application/json" \
  -d '{"minimumLevel":"Verbose"}'
```

**Esperado**: `200 OK` + settings actualizados.

**Verificar en logs**:
```bash
tail -5 samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-*.ndjson
```

Debería aparecer:
- Log de auditoría: `"Configuration changed by admin API"`.
- Logs `Verbose` empiezan a aparecer (si había código que loguea en ese nivel).

#### 6.4 PUT settings (reemplazar completos)
```bash
curl -k https://localhost:7001/api/admin/logging \
  -X PUT \
  -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY" \
  -H "Content-Type: application/json" \
  -d '{
    "isEnabled": true,
    "minimumLevel": "Warning",
    "console": { "enabled": true },
    "file": { "enabled": true, "path": "logs/log-.ndjson", "rollingFileSizeMb": 10, "retainedFileCountLimit": 30, "useCompactJson": true },
    "retention": { "days": 30 },
    "redaction": { "enabled": true },
    "correlation": { "headerName": "X-Correlation-Id", "generateIfMissing": true }
  }'
```

**Esperado**: `200 OK` + nivel cambia a `Warning` → solo logs Warning+ se emiten.

#### 6.5 POST reset (restaurar defaults)
```bash
curl -k -X POST https://localhost:7001/api/admin/logging/reset \
  -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY"
```

**Esperado**: `200 OK` + settings vuelven a los valores de `appsettings.json`.

---

### 7. Verificar Admin endpoints en Production (disabled por defecto)
**Configuración por defecto en Production**:
- `Admin.Enabled=false`

```bash
dotnet run --project samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj --environment=Production

curl -k -i https://localhost:7001/api/admin/logging
```

**Esperado**: `404 Not Found` (endpoints NO mapeados).

---

### 8. Verificar fail-fast en Production (config inválida)
**Ejemplo**: cambiar `RollingFileSizeMb` fuera del rango [1..100] en `appsettings.Production.json`:

```json
{
  "ThisCloud": {
    "Loggings": {
      "File": {
        "RollingFileSizeMb": 200
      }
    }
  }
}
```

```bash
dotnet run --project samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj --environment=Production
```

**Esperado**:
- ❌ Exception al arrancar: `ArgumentOutOfRangeException` (RollingFileSizeMb must be between 1 and 100).
- ✅ Fail-fast activo (no arranca con config inválida).

**Revertir** el cambio para continuar.

---

## 📊 Resumen de Validación

| Área | Check | Estado |
|------|-------|--------|
| Build | Sin warnings | ✅ |
| Arranque | Sin excepciones | ✅ |
| File sink | Archivo NDJSON creado | ✅ |
| Rotación | 10MB por archivo | ✅ |
| CorrelationId | Presente en logs | ✅ |
| Redaction | JWT/secrets redactados | ✅ |
| Admin GET | 200 con auth, 401 sin auth | ✅ |
| Admin PATCH | Nivel cambia runtime | ✅ |
| Admin PUT | Settings reemplazados | ✅ |
| Admin POST reset | Settings restaurados | ✅ |
| Production Admin | 404 (disabled) | ✅ |
| Fail-fast | Exception con config inválida | ✅ |

---

## 🚀 Comandos rápidos (cheatsheet)

**⚠️ Asegúrate de configurar `SAMPLE_ADMIN_APIKEY` como env var antes de ejecutar comandos Admin.**

```bash
# Configurar API key (ejemplo - usa tu propio valor)
export SAMPLE_ADMIN_APIKEY="my-local-test-key-12345"

# Build + Run Development
dotnet build samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj -c Release
dotnet run --project samples/ThisCloud.Sample.Loggings.MinimalApi/ThisCloud.Sample.Loggings.MinimalApi.csproj --environment=Development

# Test Health
curl -k https://localhost:7001/health

# Test Admin GET (usa env var)
curl -k https://localhost:7001/api/admin/logging -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY"

# Test Admin PATCH (change level)
curl -k https://localhost:7001/api/admin/logging -X PATCH -H "X-Admin-ApiKey: $SAMPLE_ADMIN_APIKEY" -H "Content-Type: application/json" -d '{"minimumLevel":"Verbose"}'

# Ver logs
tail -f samples/ThisCloud.Sample.Loggings.MinimalApi/logs/log-*.ndjson

# Verificar tamaño archivos
ls -lh samples/ThisCloud.Sample.Loggings.MinimalApi/logs/
```

---

## 🔒 Nota de Seguridad

- 🚫 **NUNCA** versionar API keys en appsettings (ni siquiera en Development).
- ✅ Usa **variables de entorno** o **user-secrets** para local dev.
- ✅ Usa **Azure Key Vault** o similar en producción.
- ⚠️ **Rota claves regularmente** y no compartas valores reales en documentación.

---

## 📖 Referencias

- **Checklist completo**: [docs/loggings/CHECKLIST.es.md](../../docs/loggings/CHECKLIST.es.md)
- **Arquitectura**: [docs/loggings/ARCHITECTURE.es.md](../../docs/loggings/ARCHITECTURE.es.md)
- **Admin docs**: [docs/loggings/admin/README.es.md](../../docs/loggings/admin/README.es.md)

---

**Validación completada**. Si todos los checks pasan, el sample está listo para demostrar integración end-to-end de `ThisCloud.Framework.Loggings`.
