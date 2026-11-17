# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Package versions are computed by Nerdbank.GitVersioning (`version.json` + git height); see [docs/RELEASING.md](docs/RELEASING.md).

## [1.1.20] - 2026-11-16

### Fixed

- `DefaultLogRedactor` redacts sensitive values written as quoted JSON keys (`"password":"x"`, `"apiKey": "x"`, `"authorization":"Bearer x"`, and keys with a prefix or suffix such as `client_secret`).
- `DefaultLogRedactor` also redacts escaped JSON inside strings (`{\"password\":\"x\"}`, keeping the escaping valid) and single-quoted keys/values (`'password':'x'`).
- The phone pattern is no longer greedy: it matches 9–15 digits with a `+` prefix, separators between digit groups, or exactly 9 bare digits, and leaves ids, ISO dates, timestamps, epoch values, GUIDs and IPv4 addresses untouched. A dotted phone shaped exactly like an IPv4 address (four groups of up to three digits, each ≤ 255) is kept as-is.
- Log-level `Overrides` (configuration, `LogSettings.Overrides`, Admin `PUT`/`PATCH /settings`) now take effect at runtime: the most specific namespace wins, and an override can raise or lower a namespace's level. `Overrides: null` keeps the active overrides and an empty object clears them. Audit events are still never silenced.
- `POST /enable` after `POST /disable` restores the minimum level that was active before `/disable`. A level applied through settings while disabled is the one restored.
- Redactor patterns run in linear time (no catastrophic backtracking on crafted input, including the JWT pattern, which was already quadratic in 1.1.8), and a built-in pattern that hits the regex timeout makes `Redact` return `[REDACTED]` instead of throwing, so audit logging never breaks.
- `DELETE /settings` (`ResetSettingsAsync`) returns to the configured startup state: the level resets to Information, the overrides from configuration stay active and runtime-only overrides are cleared.
- `ThisCloud.Framework.Contracts` and `ThisCloud.Framework.Web` packages declare the ISC license (`PackageLicenseExpression`).

### Changed

- While logging is disabled, per-namespace overrides no longer let events through; only audit events are written.

### Known limitations

- After `DELETE /settings`, `GET /settings` reports no overrides even though the overrides from configuration are active (same as 1.1.8).
- Additional user-supplied redaction patterns that hit the regex timeout are skipped, and an unterminated quoted value (`"password":"abc` with no closing quote) is not redacted (same as 1.1.8).

## [1.1.8] - 2026-11-16

Published version of the 1.1 line (`1.1.<git height>`, computed by Nerdbank.GitVersioning).

### Breaking / behavior changes

These changes make the defaults fail closed. They can change the behavior of existing consumers; see the migration notes below.

- **Loggings.Admin: startup now fails outside Development without `RequireAdmin`.** When `ThisCloud:Loggings:Admin:Enabled=true` and the Admin endpoints would be mapped in any environment other than `Development` while `RequireAdmin=false` (the default), `MapThisCloudFrameworkLoggingsAdmin` throws `InvalidOperationException`.
- **Loggings.Admin: an empty `AllowedEnvironments` now means "Development only"** (it used to mean every environment). A non-empty list keeps its previous meaning.
- **Loggings.Admin: stricter validation on `PUT`/`PATCH /settings`.** `File.Path` must be relative, contain no `..` segments and resolve under the application content root (`ContentRootPath`, or the current directory). `Redaction.AdditionalPatterns` accepts at most 20 patterns of at most 512 characters, none blank, each a valid regular expression. Violations return `400` with the unchanged `application/json` body `{ "error": "<message>" }`; previously absolute paths such as `/var/log/app/log-.ndjson` were accepted.
- **Web: 5xx responses no longer echo the exception message.** `ExceptionMappingMiddleware` returns the generic `detail` `"An unexpected error occurred."` for 500/502/504 (the exception is logged server-side and the correlation id is kept). 4xx responses keep their current `detail`. The Loggings.Admin 500 ProblemDetails (already ProblemDetails before 1.1) use the same generic detail; the Admin 400 validation response keeps its `{ "error": "..." }` shape.
- **Web: `Swagger.RequireAdmin=true` without a registered `IAuthorizationService` now returns 403** (it used to serve Swagger without any check).
- **Dependencies:** `Swashbuckle.AspNetCore` 7.x → 10.3.x and `Microsoft.OpenApi` 1.x → 2.13.x in `ThisCloud.Framework.Web`. Consumers that reference `Microsoft.OpenApi.Models` types must migrate to the OpenApi 2.x API (`Microsoft.OpenApi` namespace, `OpenApiSecuritySchemeReference`).

#### Migration notes

1. **Admin outside Development:** set `ThisCloud:Loggings:Admin:RequireAdmin=true`, call `AddAuthorization()` with a policy named like `AdminPolicyName` (default `"Admin"`), and list the environment explicitly in `AllowedEnvironments`. Or set `Admin:Enabled=false`.
2. **Admin with an empty `AllowedEnvironments` in Staging/Production:** list the environments explicitly (and set `RequireAdmin=true`), otherwise the endpoints are no longer mapped.
3. **Runtime `File.Path`:** send relative paths (for example `logs/app/log-.ndjson`) instead of absolute ones. Absolute paths and `..` are rejected.
4. **Clients parsing 5xx `detail`:** do not rely on the exception message; use the correlation id (`meta.correlationId`) to find the server-side log entry.
5. **Swagger with `RequireAdmin=true`:** register `AddAuthentication()`/`AddAuthorization()` and the `"Admin"` policy, otherwise `/swagger` answers 403.

### Security

- Loggings.Admin audit events are no longer silenced by `POST /disable` or `MinimumLevel=Critical`: `UseThisCloudFrameworkSerilog` applies a per-source-context minimum-level override for `SerilogAuditLogger` (after the user overrides) so neither the global level switch nor a user `Overrides` entry filters them.
- Loggings.Admin no longer returns exception messages in 500 responses; the exception is logged instead.
- Dependency vulnerability gate: NuGetAudit (`all` mode) runs on every restore and `NU1903`/`NU1904` are errors. `Microsoft.OpenApi` was raised past GHSA-v5pm-xwqc-g5wc.
- CI/CD: GitHub Actions in `ci.yml` and `publish.yml` are pinned to full commit SHAs; `publish.yml` passes `NUGET_API_KEY` and `github.ref` through `env:` instead of expanding them inline in shell scripts.
- CI/CD: `publish.yml` uses NuGet Trusted Publishing (`NuGet/login`, GitHub OIDC) instead of a long-lived API key, and runs in the protected `nuget` environment (manual approval).
- Sample `ThisCloud.Sample.MinimalApi`: Swagger is only allowed in `Development`.

### Changed

- Upgraded dependencies to the latest net10.0-compatible versions: Microsoft.* 10.0.15, Serilog 4.4.0, Serilog.Settings.Configuration 10.0.1, xunit.v3 4.0.3, FluentAssertions 7.2.2 (8.x is commercially licensed), Nerdbank.GitVersioning 3.10.94, GitHub Actions checkout v6 / setup-dotnet v5 / upload-artifact v7.
- Tests run on Microsoft.Testing.Platform (`global.json`) with `coverlet.MTP` and a 90% line coverage gate per test project; the test command is `dotnet test --solution ThisCloud.Framework.slnx -c Release --no-build --coverlet --coverlet-output-format cobertura --coverlet-threshold 90 --coverlet-threshold-type line`.
- `publish.yml` verifies that the pushed tag equals the Nerdbank.GitVersioning version before publishing. See [docs/RELEASING.md](docs/RELEASING.md).

### Known limitations (fixed in 1.1.20)

- `DefaultLogRedactor` regex gaps (quoted JSON values, greedy phone matching).
- `Overrides` accepted by the Admin API are stored but not applied to the running Serilog logger.
- `POST /enable` re-enables the flag but does not restore the previous minimum level set by `POST /disable`.

## [1.0.x]

Releases published before this changelog existed (last published tag: `v1.0.93`, NuGet `1.0.94`).
