# Safe Consumption Checklist - ThisCloud.Framework.Loggings

> 📋 **Language**: English | [Español](CHECKLIST.es.md)

## Introduction

This checklist ensures **safe and compliant** use of **ThisCloud.Framework.Loggings** in production environments. Validate **ALL** points before deployment.

---

## ✅ Security

### Secrets and Sensitive Data

- [ ] **NO logging secrets**: Verify that `Authorization`, `Bearer`, JWT tokens, passwords, API keys are NOT logged
- [ ] **NO body logging**: Full request/response payloads are **prohibited** (include only metadata: method, path, statusCode, duration)
- [ ] **Redaction enabled**: `ThisCloud:Loggings:Redaction:Enabled=true` in Production (`appsettings.Production.json`)
- [ ] **Validate custom redaction**: If implementing custom `ILogRedactor`, verify it redacts ALL domain-specific secrets (SSN, card numbers, etc.)
- [ ] **No PII in structured logs**: When logging objects with `{@Object}`, ensure properties do NOT contain emails/DNI/NIE exposed in `ToString()` or public properties

### Headers and Authentication

- [ ] **Authorization header**: Verify logs do NOT include `Authorization: Bearer eyJ...` (DefaultLogRedactor redacts it, but validate)
- [ ] **API keys in query strings**: If using `?apiKey=...`, implement custom redaction (DefaultLogRedactor does NOT cover query strings)
- [ ] **Session cookies**: Do NOT log full `Cookie` headers (may contain session tokens)

### GDPR / Privacy

- [ ] **Consent**: If logging IPs/User-Agent, verify you have legal basis (legitimate interest, contract, etc.)
- [ ] **Right to erasure**: Implement procedure to purge logs if user requests data deletion
- [ ] **Retention compliance**: `Retention.Days` aligned with privacy policies (max 90 days recommended for GDPR, adjust as needed)

---

## ✅ Production

### File Sink Configuration

- [ ] **Console sink disabled**: `ThisCloud:Loggings:Console:Enabled=false` in `appsettings.Production.json` (console may expose data in container/k8s logs)
- [ ] **File sink enabled**: `ThisCloud:Loggings:File:Enabled=true`
- [ ] **File.Path valid**: Valid absolute path (e.g., `/var/log/myapp/log-.ndjson`), verify write permissions BEFORE deploy
- [ ] **RollingFileSizeMb configured**: Value between 1 and 100 MB (recommended: 10 MB for frequent rotation)
- [ ] **RetainedFileCountLimit**: Configured per available space (recommended: 7-30 files for 7-30 days at 10MB/day)

### Fail-fast Validation

- [ ] **ProductionValidator enabled**: Verify `ASPNETCORE_ENVIRONMENT=Production` or `DOTNET_ENVIRONMENT=Production`
- [ ] **No silent fallback**: If config invalid, startup MUST fail (do not continue with default config)
- [ ] **Test fail-fast**: Before deploy, simulate invalid config (`File.Path=""`) and verify startup fails with clear message

### MinimumLevel Production

- [ ] **Appropriate MinimumLevel**: Recommended `Warning` or `Error` in Production (avoid `Information`/`Debug` due to volume)
- [ ] **Overrides configured**: If you need `Debug` in specific namespace, use `Overrides` (e.g., `"MyApp.CriticalModule": "Debug"`)

### Retention and Cleanup

- [ ] **Host responsibility**: Framework does NOT delete old logs automatically
- [ ] **Cleanup job implemented**: Cron/scheduled task to purge logs > `Retention.Days` (use `find /var/log/myapp -name "log-*.ndjson" -mtime +$DAYS -delete` or similar)
- [ ] **Space monitoring**: Alerts if `/var/log` exceeds 80% capacity

---

## ✅ Admin Endpoints

### Admin Security

- [ ] **Admin.Enabled=false by default**: In base `appsettings.json`, `Admin.Enabled` must be `false`
- [ ] **AllowedEnvironments explicit**: If enabling Admin, configure `AllowedEnvironments` (e.g., `["Development", "Staging"]`, **NEVER** `["Production"]` without additional protection)
- [ ] **RequireAdmin=true mandatory**: If Admin enabled, `RequireAdmin=true` and configure `"Admin"` policy with `AddAuthorization()`
- [ ] **Robust policy**: `"Admin"` policy must require specific roles/claims (e.g., `RequireRole("Admin")`, NOT `RequireAuthenticatedUser()` alone)

### Public Exposure

- [ ] **NO public exposure**: Admin endpoints must NOT be accessible from Internet without VPN/firewall
- [ ] **Firewall/ACL**: Configure network rules to block `/api/admin/logging/*` from public IPs
- [ ] **Rate limiting**: Configure rate limiting on Admin endpoints (prevent brute force if policy vulnerability exists)

### Admin Auditing

- [ ] **IAuditLogger configured**: Verify config changes via Admin are audited (log level `Information` minimum)
- [ ] **Auditing without secrets**: Verify `IAuditLogger` does NOT log secrets in `before`/`after` (redact before serializing)
- [ ] **Centralize auditing**: If using SIEM, ensure Admin audit logs are sent (filter by `sourceContext: "SerilogAuditLogger"`)

---

## ✅ Operation

### Log Location and Access

- [ ] **Document File.Path**: In runbook/wiki, document where logs are written (e.g., `/var/log/myapp/log-20260215.ndjson`)
- [ ] **Read permissions**: Configure file permissions so only authorized users/processes read logs (e.g., `chmod 640`, owner `myapp:myapp`)
- [ ] **Backup**: If logs are critical for legal audit, configure backup before purging

### Validate Logging is Working

- [ ] **Smoke test post-deploy**: After deploy, make test request and verify logs appear in `File.Path`
- [ ] **Correlation check**: Verify `correlationId` appears in logs (send `X-Correlation-Id` header and search in logs)
- [ ] **Enrichment check**: Verify properties `service`, `env`, `userId` in logs (open `.ndjson` file and search for keys)

### File Rotation

- [ ] **Automatic rolling**: Framework rotates files automatically:
  - Daily (`log-20260215.ndjson` → `log-20260216.ndjson`)
  - By size (`log-20260215.ndjson` + `log-20260215_001.ndjson` if exceeds `RollingFileSizeMb`)
- [ ] **Validate rotation**: Generate logs > `RollingFileSizeMb` in test and verify `_001` file is created
- [ ] **File monitoring**: Configure alert if number of `log-*.ndjson` files exceeds `RetainedFileCountLimit` + 10% (indicates cleanup failure)

### Centralization (SIEM/ELK/Splunk)

- [ ] **Shipper configured**: If you need centralized logs, configure Filebeat/Fluentd/Logstash to read `File.Path`
- [ ] **NDJSON parsing**: Shipper must parse JSON (framework format), do NOT read as plain text
- [ ] **Secret filters**: Configure filters in shipper to redact secrets BEFORE sending to SIEM (defense in depth)

---

## ✅ Support and Responsibility

### Support Limits

- [ ] **No SLA**: OSS framework, use at your own risk, **no guarantees of availability or performance**
- [ ] **No 24/7 support**: GitHub issues are best-effort, no guaranteed response times
- [ ] **User responsibility**: User is responsible for:
  - Correct configuration (paths, permissions, retention)
  - GDPR / local regulation compliance
  - Security incident management (leaks, breaches)
  - Storage / infrastructure costs

### Updates

- [ ] **Monitor releases**: Subscribe to [GitHub Releases](https://github.com/mdesantis1984/ThisCloud.Framework/releases) for security updates
- [ ] **Breaking changes**: Read CHANGELOG before updating (may have breaking changes in config/API)
- [ ] **Test in Staging**: Do NOT update directly in Production, test in Staging environment first

---

## ✅ Security Incidents

### If a Secret was Leaked in Logs

**Immediate action** (in order):

1. [ ] **Rotate credentials**: Immediately change leaked API keys/passwords/tokens
2. [ ] **Purge logs**: Delete log files containing secrets:
   ```bash
   # Backup first (if legal audit requires)
   sudo cp /var/log/myapp/log-*.ndjson /backup/incident-$(date +%Y%m%d)/
   
   # Purge files with secrets
   sudo find /var/log/myapp -name "log-*.ndjson" -mtime -7 -delete
   ```
3. [ ] **Purge centralized logs**: If logs sent to SIEM/ELK, purge affected indexes:
   ```bash
   # Elasticsearch example
   curl -X DELETE "https://elk.example.com/logs-myapp-2026.02.15"
   ```
4. [ ] **Investigate cause**: Identify WHERE secret was logged (code line, middleware, etc.)
5. [ ] **Fix + custom redaction**: Implement custom `ILogRedactor` for leaked secret type
6. [ ] **Post-mortem**: Document incident, root cause, remediation, future prevention

### If Security Breach in Admin Endpoints

**Immediate action**:

1. [ ] **Disable Admin**: Change `Admin.Enabled=false` in config and redeploy
2. [ ] **Investigate access**: Review audit logs (`sourceContext: "SerilogAuditLogger"`) for unauthorized changes
3. [ ] **Revert malicious changes**: If config was altered via Admin, revert to known valid config
4. [ ] **Forensic analysis**: Identify how Admin was accessed (stolen credentials, weak policy, etc.)
5. [ ] **Harden**: Implement mitigations (MFA for Admin, mandatory VPN, WAF rules, etc.)

### Report Vulnerabilities

- [ ] **GitHub Security Advisory**: Report framework vulnerabilities via [GitHub Security](https://github.com/mdesantis1984/ThisCloud.Framework/security/advisories) (private)
- [ ] **NO public disclosure**: Do not publish PoC/details in public issues until fix is available

---

## ✅ Compliance / Regulatory

### GDPR (Europe)

- [ ] **Aligned retention**: `Retention.Days` <= 90 days (GDPR recommendation for operational logs)
- [ ] **DPA with processors**: If logs sent to cloud SIEM (Datadog, Splunk Cloud), sign DPA (Data Processing Agreement)
- [ ] **Processing record**: Document log processing in GDPR record (legal basis, data categories, recipients, terms)

### HIPAA (USA - Healthcare)

- [ ] **NO PHI in logs**: Prohibited to log PHI (Protected Health Information) without complete redaction
- [ ] **Encryption at rest**: Logs on disk must be encrypted (BitLocker, LUKS, etc.)
- [ ] **Audit trail**: Admin audit logs must be retained 6 years (HIPAA requirement)

### PCI-DSS (Payments)

- [ ] **NO cardholder data**: Prohibited to log PAN (Primary Account Number), CVV, PIN
- [ ] **Log retention**: 1 year active + 3 years archived (PCI-DSS 10.7)
- [ ] **Tamper protection**: Logs must NOT be modifiable (use file permissions 440, WORM storage, etc.)

---

## ✅ Performance

### Log Volume

- [ ] **Limit verbosity**: In Production, avoid `MinimumLevel: Debug` or `Verbose` (can generate GB/day)
- [ ] **Sampling**: If high traffic (>1000 req/s), consider sampling (log only 10% requests at `Debug`, 100% `Error`)
- [ ] **No logs in loops**: Verify there are NO `ILogger.Log()` inside tight loops (use summary log outside loop)

### Disk I/O

- [ ] **Fast disk**: Logs written to disk synchronously; use SSD or fast disk for `/var/log`
- [ ] **Async file sink**: Consider Serilog async sink if log latency is critical (see Serilog docs)

### Backpressure

- [ ] **Serilog queue**: If logs generated faster than written, Serilog may drop events (configure `WriteTo.Async()` with buffer if needed)

---

## 📚 References

- [Full Architecture (EN)](ARCHITECTURE.en.md) | [Arquitectura (ES)](ARCHITECTURE.es.md)
- [Abstractions Package](packages/abstractions/README.en.md)
- [Serilog Package](packages/serilog/README.en.md)
- [Admin Package](packages/admin/README.en.md)
- [Root README (monorepo)](../../README.md)

---

## ⚠️ Legal Disclaimer

**This software is provided "AS IS", without warranties of any kind.**

- ❌ **No warranties**: No implied warranty of merchantability, fitness, non-infringement.
- ❌ **No liability**: Author is NOT liable for data loss, security breaches, service interruptions, regulatory penalties (GDPR, HIPAA, PCI-DSS), incident costs.
- ✅ **User responsibility**: User assumes ALL responsibility for:
  - Correct and secure configuration
  - Compliance with applicable regulations
  - Security incident management
  - Infrastructure and operation costs

See full [LICENSE](../../LICENSE) for detailed terms.

---

## 📜 License

**ISC License**

Copyright (c) 2025 Marco Alejandro De Santis

Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted, provided that the above copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
