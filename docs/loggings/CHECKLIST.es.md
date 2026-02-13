# Checklist de Consumo Seguro - ThisCloud.Framework.Loggings

> 📋 **Idioma**: Español | [English](CHECKLIST.en.md)

## Introducción

Este checklist garantiza el **uso seguro y conforme** de **ThisCloud.Framework.Loggings** en entornos productivos. Validar **TODOS** los puntos antes de deploy.

---

## ✅ Seguridad

### Secretos y Datos Sensibles

- [ ] **NO loguear secretos**: Verificar que `Authorization`, `Bearer`, JWT tokens, passwords, API keys NO se loguean
- [ ] **NO body logging**: Request/response payloads completos están **prohibidos** (incluir solo metadata: método, path, statusCode, duración)
- [ ] **Redaction habilitada**: `ThisCloud:Loggings:Redaction:Enabled=true` en Production (`appsettings.Production.json`)
- [ ] **Validar redaction custom**: Si implementas `ILogRedactor` custom, verificar que redacta TODOS los secretos de tu dominio (SSN, números de tarjeta, etc.)
- [ ] **No PII en logs estructurados**: Al loguear objetos con `{@Object}`, asegurar que las propiedades NO contienen emails/DNI/NIE expuestos en `ToString()` o propiedades públicas

### Cabeceras y Autenticación

- [ ] **Authorization header**: Verificar que logs NO incluyen `Authorization: Bearer eyJ...` (DefaultLogRedactor lo redacta, pero validar)
- [ ] **API keys en query strings**: Si usas `?apiKey=...`, implementar redaction custom (DefaultLogRedactor NO cubre query strings)
- [ ] **Cookies de sesión**: NO loguear `Cookie` headers completos (pueden contener session tokens)

### GDPR / Privacidad

- [ ] **Consentimiento**: Si logueas IPs/User-Agent, verificar que tienes base legal (legítimo interés, contrato, etc.)
- [ ] **Derecho al olvido**: Implementar procedimiento para purgar logs si un usuario solicita eliminación de datos
- [ ] **Retention compliance**: `Retention.Days` alineado con políticas de privacidad (máx 90 días recomendado GDPR, ajustar según necesidad)

---

## ✅ Production

### Configuración File Sink

- [ ] **Console sink deshabilitado**: `ThisCloud:Loggings:Console:Enabled=false` en `appsettings.Production.json` (console puede exponer datos en logs de contenedor/k8s)
- [ ] **File sink habilitado**: `ThisCloud:Loggings:File:Enabled=true`
- [ ] **File.Path válido**: Ruta absoluta válida (ej: `/var/log/myapp/log-.ndjson`), verificar permisos de escritura ANTES de deploy
- [ ] **RollingFileSizeMb configurado**: Valor entre 1 y 100 MB (recomendado: 10 MB para rotación frecuente)
- [ ] **RetainedFileCountLimit**: Configurado según espacio disponible (recomendado: 7-30 archivos para 7-30 días con 10MB/día)

### Fail-fast Validation

- [ ] **ProductionValidator activado**: Verificar que `ASPNETCORE_ENVIRONMENT=Production` o `DOTNET_ENVIRONMENT=Production`
- [ ] **No silent fallback**: Si config inválida, arranque DEBE fallar (no continuar con config por defecto)
- [ ] **Testear fail-fast**: Antes de deploy, simular config inválida (`File.Path=""`) y verificar que arranque falla con mensaje claro

### MinimumLevel Production

- [ ] **MinimumLevel apropiado**: Recomendado `Warning` o `Error` en Production (evitar `Information`/`Debug` por volumen)
- [ ] **Overrides configurados**: Si necesitas `Debug` en namespace específico, usar `Overrides` (ej: `"MyApp.CriticalModule": "Debug"`)

### Retention y Limpieza

- [ ] **Responsabilidad del host**: Framework NO elimina logs antiguos automáticamente
- [ ] **Job de limpieza implementado**: Cron/scheduled task para purgar logs > `Retention.Days` (usar `find /var/log/myapp -name "log-*.ndjson" -mtime +$DAYS -delete` o similar)
- [ ] **Monitoreo de espacio**: Alertas si `/var/log` supera 80% capacidad

---

## ✅ Admin Endpoints

### Seguridad Admin

- [ ] **Admin.Enabled=false por defecto**: En `appsettings.json` base, `Admin.Enabled` debe ser `false`
- [ ] **AllowedEnvironments explícito**: Si habilitas Admin, configurar `AllowedEnvironments` (ej: `["Development", "Staging"]`, **NUNCA** `["Production"]` sin protección adicional)
- [ ] **RequireAdmin=true obligatorio**: Si Admin habilitado, `RequireAdmin=true` y configurar policy `"Admin"` con `AddAuthorization()`
- [ ] **Policy robusta**: Policy `"Admin"` debe requerir roles/claims específicos (ej: `RequireRole("Admin")`, NO `RequireAuthenticatedUser()` solo)

### Exposición Pública

- [ ] **NO exponer públicamente**: Admin endpoints NO deben ser accesibles desde Internet sin VPN/firewall
- [ ] **Firewall/ACL**: Configurar reglas de red para bloquear `/api/admin/logging/*` desde IPs públicas
- [ ] **Rate limiting**: Configurar rate limiting en Admin endpoints (prevenir brute force si hay vulnerabilidad en policy)

### Auditoría Admin

- [ ] **IAuditLogger configurado**: Verificar que cambios de configuración vía Admin se auditan (log level `Information` mínimo)
- [ ] **Auditoría sin secretos**: Verificar que `IAuditLogger` NO loguea secretos en `before`/`after` (redactar antes de serializar)
- [ ] **Centralizar auditoría**: Si usas SIEM, asegurar que logs de auditoría Admin se envían (filtrar por `sourceContext: "SerilogAuditLogger"`)

---

## ✅ Operación

### Ubicación y Acceso a Logs

- [ ] **Documentar File.Path**: En runbook/wiki, documentar dónde se escriben logs (ej: `/var/log/myapp/log-20260215.ndjson`)
- [ ] **Permisos de lectura**: Configurar permisos de archivo para que solo usuarios/procesos autorizados lean logs (ej: `chmod 640`, owner `myapp:myapp`)
- [ ] **Backup**: Si logs son críticos para auditoría legal, configurar backup antes de purgar

### Validar que se está Logueando

- [ ] **Smoke test post-deploy**: Después de deploy, hacer request de prueba y verificar que logs aparecen en `File.Path`
- [ ] **Check de correlación**: Verificar que `correlationId` aparece en logs (enviar `X-Correlation-Id` header y buscar en logs)
- [ ] **Check de enrichment**: Verificar propiedades `service`, `env`, `userId` en logs (abrir archivo `.ndjson` y buscar keys)

### Rotación de Archivos

- [ ] **Rolling automático**: Framework rota archivos automáticamente:
  - Diariamente (`log-20260215.ndjson` → `log-20260216.ndjson`)
  - Por tamaño (`log-20260215.ndjson` + `log-20260215_001.ndjson` si supera `RollingFileSizeMb`)
- [ ] **Validar rotación**: Generar logs > `RollingFileSizeMb` en test y verificar que se crea archivo `_001`
- [ ] **Monitoreo de archivos**: Configurar alerta si número de archivos `log-*.ndjson` supera `RetainedFileCountLimit` + 10% (indica fallo en limpieza)

### Centralización (SIEM/ELK/Splunk)

- [ ] **Shipper configurado**: Si necesitas logs centralizados, configurar Filebeat/Fluentd/Logstash para leer `File.Path`
- [ ] **Parsing NDJSON**: Shipper debe parsear JSON (formato del framework), NO leer como plain text
- [ ] **Filtros de secretos**: Configurar filtros en shipper para redactar secretos ANTES de enviar a SIEM (defensa en profundidad)

---

## ✅ Soporte y Responsabilidad

### Límites de Soporte

- [ ] **Sin SLA**: Framework OSS, uso bajo responsabilidad del usuario, **sin garantías de disponibilidad o rendimiento**
- [ ] **Sin soporte 24/7**: Issues en GitHub son best-effort, sin tiempos de respuesta garantizados
- [ ] **Responsabilidad del usuario**: Usuario es responsable de:
  - Configuración correcta (paths, permisos, retention)
  - Cumplimiento GDPR / regulaciones locales
  - Gestión de incidentes de seguridad (leaks, brechas)
  - Costos de almacenamiento / infraestructura

### Actualizaciones

- [ ] **Monitorear releases**: Suscribirse a [GitHub Releases](https://github.com/mdesantis1984/ThisCloud.Framework/releases) para updates de seguridad
- [ ] **Breaking changes**: Leer CHANGELOG antes de actualizar (puede haber breaking changes en config/API)
- [ ] **Testear en Staging**: NO actualizar directamente en Production, probar en entorno Staging primero

---

## ✅ Incidentes de Seguridad

### Si se Filtró un Secreto en Logs

**Acción inmediata** (en orden):

1. [ ] **Rotar credenciales**: Cambiar inmediatamente API keys/passwords/tokens filtrados
2. [ ] **Purgar logs**: Eliminar archivos de log que contienen secretos:
   ```bash
   # Backup primero (si auditoría legal requiere)
   sudo cp /var/log/myapp/log-*.ndjson /backup/incident-$(date +%Y%m%d)/
   
   # Purgar archivos con secretos
   sudo find /var/log/myapp -name "log-*.ndjson" -mtime -7 -delete
   ```
3. [ ] **Purgar logs centralizados**: Si logs enviados a SIEM/ELK, purgar índices afectados:
   ```bash
   # Ejemplo Elasticsearch
   curl -X DELETE "https://elk.example.com/logs-myapp-2026.02.15"
   ```
4. [ ] **Investigar causa**: Identificar DÓNDE se logueó el secreto (línea de código, middleware, etc.)
5. [ ] **Fix + redaction custom**: Implementar `ILogRedactor` custom para el tipo de secreto filtrado
6. [ ] **Post-mortem**: Documentar incidente, causa raíz, remediación, prevención futura

### Si Hay Brecha de Seguridad en Admin Endpoints

**Acción inmediata**:

1. [ ] **Deshabilitar Admin**: Cambiar `Admin.Enabled=false` en config y redesplegar
2. [ ] **Investigar acceso**: Revisar logs de auditoría (`sourceContext: "SerilogAuditLogger"`) para cambios no autorizados
3. [ ] **Revertir cambios maliciosos**: Si config fue alterada vía Admin, revertir a config válida conocida
4. [ ] **Análisis forense**: Identificar cómo se accedió a Admin (credenciales robadas, policy débil, etc.)
5. [ ] **Endurecer**: Implementar mitigaciones (MFA para Admin, VPN obligatoria, WAF rules, etc.)

### Reportar Vulnerabilidades

- [ ] **GitHub Security Advisory**: Reportar vulnerabilidades en framework vía [GitHub Security](https://github.com/mdesantis1984/ThisCloud.Framework/security/advisories) (privado)
- [ ] **NO disclosure público**: No publicar PoC/detalles en issues públicos hasta fix disponible

---

## ✅ Compliance / Regulatorio

### GDPR (Europa)

- [ ] **Retention alineado**: `Retention.Days` <= 90 días (recomendación GDPR para logs operacionales)
- [ ] **DPA con procesadores**: Si logs se envían a SIEM cloud (Datadog, Splunk Cloud), firmar DPA (Data Processing Agreement)
- [ ] **Registro de tratamiento**: Documentar tratamiento de logs en registro GDPR (base legal, categorías de datos, destinatarios, plazos)

### HIPAA (USA - Healthcare)

- [ ] **NO PHI en logs**: Prohibido loguear PHI (Protected Health Information) sin redaction completa
- [ ] **Encryption at rest**: Logs en disco deben estar encriptados (BitLocker, LUKS, etc.)
- [ ] **Audit trail**: Logs de auditoría Admin deben conservarse 6 años (HIPAA requirement)

### PCI-DSS (Pagos)

- [ ] **NO cardholder data**: Prohibido loguear PAN (Primary Account Number), CVV, PIN
- [ ] **Log retention**: 1 año activo + 3 años archivado (PCI-DSS 10.7)
- [ ] **Tamper protection**: Logs NO deben ser modificables (usar file permissions 440, WORM storage, etc.)

---

## ✅ Performance

### Volumen de Logs

- [ ] **Limitar verbosity**: En Production, evitar `MinimumLevel: Debug` o `Verbose` (puede generar GB/día)
- [ ] **Sampling**: Si alto tráfico (>1000 req/s), considerar sampling (loguear solo 10% requests con `Debug`, 100% `Error`)
- [ ] **No logs en loops**: Verificar que NO hay `ILogger.Log()` dentro de loops tight (usar log resumen fuera del loop)

### I/O Disk

- [ ] **Disk rápido**: Logs se escriben a disco sincronamente; usar SSD o disk rápido para `/var/log`
- [ ] **Async file sink**: Considerar Serilog async sink si latencia de logs es crítica (ver docs Serilog)

### Backpressure

- [ ] **Serilog queue**: Si logs se generan más rápido de lo que se escriben, Serilog puede dropear eventos (configurar `WriteTo.Async()` con buffer si necesario)

---

## 📚 Referencias

- [Arquitectura completa (ES)](ARCHITECTURE.es.md)
- [Paquete Abstractions](packages/abstractions/README.es.md)
- [Paquete Serilog](packages/serilog/README.es.md)
- [Paquete Admin](packages/admin/README.es.md)
- [README raíz (monorepo)](../../README.md)

---

## ⚠️ Disclaimer Legal

**Este software se proporciona "TAL CUAL", sin garantías de ningún tipo.**

- ❌ **Sin garantías**: Sin garantía implícita de idoneidad, comerciabilidad, no infracción.
- ❌ **Sin responsabilidad**: Autor NO es responsable por pérdidas de datos, brechas de seguridad, interrupciones de servicio, sanciones regulatorias (GDPR, HIPAA, PCI-DSS), costos de incidentes.
- ✅ **Responsabilidad del usuario**: Usuario asume TODA la responsabilidad de:
  - Configuración correcta y segura
  - Cumplimiento de regulaciones aplicables
  - Gestión de incidentes de seguridad
  - Costos de infraestructura y operación

Ver [LICENSE completa](../../LICENSE) para términos detallados.

---

## 📜 Licencia

**ISC License**

Copyright (c) 2025 Marco Alejandro De Santis

Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted, provided that the above copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
