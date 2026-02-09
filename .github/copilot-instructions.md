# Copilot Instructions

## Repo root (MANDATORIO)
- El repo root se define SIEMPRE por: `git rev-parse --show-toplevel`.
- Antes de validar archivos/ejecutar comandos: `cd "$(git rev-parse --show-toplevel)"`.
- Prohibido crear `version.json` o `Directory.Build.props` fuera del repo root.

## Estructura (MANDATORIO)
- Respetar 1er y 2do nivel de carpetas según `estructurasCarpetas.txt`.
- Prohibido crear rutas duplicadas tipo `src/X/X/*.cs`.

## Solución (MANDATORIO)
- La solución oficial es: `ThisCloud.Framework.slnx`.
- Prohibido crear/usar `.sln`.
- Todos los comandos deben apuntar explícitamente a `ThisCloud.Framework.slnx`.

## Plan (MANDATORIO)
- Source of truth: `docs/Plan_ThisCloud_Framework_Web_v9.md`.
- Prohibido ejecutar tareas fuera de IDs W0.x–W8.x.
- Prohibido avanzar a Fase N+1 si Fase N no está cerrada (exit criteria verificada).

## Verificación antes de marcar ✅ (MANDATORIO)
- No marcar una tarea como completada si no se ejecutaron y pasaron:
  - `dotnet build ThisCloud.Framework.slnx -c Release`
  - `dotnet test ThisCloud.Framework.slnx -c Release /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line`
  - `dotnet pack ThisCloud.Framework.slnx -c Release -o ./artifacts`
  - `ls -la version.json Directory.Build.props` (desde repo root)

## Reporte (MANDATORIO, máximo 5 líneas)
1) IDs: ...
2) Verificación: build=..., test_cov=..., pack=...
3) Cambios: (máx 3 bullets)
4) Bloqueos: si/no + 1 línea
5) Próximo: (1–2 IDs, misma fase)

## Estilo de código
- Nombres claros, sin placeholders no usados (ej: eliminar `Class1` si no aporta).
- XML docs obligatoria en `src/*` (1591 como error); no aplicar a tests/samples.
