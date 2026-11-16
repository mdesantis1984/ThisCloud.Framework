# Releasing ThisCloud.Framework

This document describes the branching model, the local verification command and the release procedure for the NuGet packages published from this repository.

## Git Flow

| Branch | Purpose |
| --- | --- |
| `main` | Released code. Every published version is tagged on this branch. |
| `develop` | Integration branch for the next release. |
| `feature/*` | Work in progress. Created from `develop`, merged back into `develop` through a Pull Request. |

Rules:

1. Create `feature/<name>` from `develop`. Never commit directly to `main` or `develop`.
2. Open a Pull Request from `feature/<name>` to `develop`. The `ci` workflow (restore, build, tests, coverage gate, vulnerable-dependency audit) must pass.
3. To release, merge `develop` into `main` (Pull Request, merge commit).
4. Tag the release on `main` (see [Publishing a release](#publishing-a-release)).

## Local verification

Tests run on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (configured in `global.json`) with `coverlet.MTP` for coverage. Run exactly what CI runs:

```bash
dotnet restore ThisCloud.Framework.slnx
dotnet build ThisCloud.Framework.slnx -c Release --no-restore
dotnet test --solution ThisCloud.Framework.slnx -c Release --no-build \
  --coverlet --coverlet-output-format cobertura \
  --coverlet-threshold 90 --coverlet-threshold-type line
```

- The command fails (exit code 14) if any test project's line coverage is below 90%.
- Cobertura reports are written to `tests/*/bin/Release/net10.0/TestResults/`.
- Restore fails on a known high or critical vulnerability (`NU1903` / `NU1904`) in any direct or transitive package (`NuGetAudit`, see `Directory.Build.props`).

## Publishing a release

Versions are computed by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (`version.json` + git height), so **the tag must be created with `nbgv`**. A hand-written tag can differ from the version that ends up in the packages (for example, tag `v1.0.93` produced package `1.0.94`).

1. Make sure `main` contains the release (`develop` merged) and CI is green.
2. Check out `main` at the release commit and create the tag:

   ```bash
   dotnet tool install --global nbgv --version 3.10.94   # once
   git checkout main && git pull
   nbgv get-version -v NuGetPackageVersion               # version that will be published
   nbgv tag                                              # creates v<version> on HEAD
   ```

3. Push the tag. This triggers `.github/workflows/publish.yml`:

   ```bash
   git push origin v<version>
   ```

4. The workflow only runs on `v*` tags. It first verifies that the pushed tag equals `v$(nbgv get-version -v NuGetPackageVersion)` and fails with an `::error::` message otherwise (delete the tag and recreate it with `nbgv tag`). Then it restores, builds, runs the tests and the coverage gate, packs and pushes every `.nupkg` to nuget.org using `--skip-duplicate`.

## NuGet credentials and maintenance

- Publishing uses the repository Actions secret named `NUGET_API_KEY` (repository Settings, Actions section).
- NuGet API keys expire. Check the expiry date of the key on nuget.org and rotate the stored value **before** it expires; an expired key makes the publish step fail with `403`.
- Pushing tags and publishing packages are irreversible: a version pushed to nuget.org can only be unlisted, never replaced.
