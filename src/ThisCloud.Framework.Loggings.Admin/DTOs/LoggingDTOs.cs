// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using ThisCloud.Framework.Loggings.Abstractions;

namespace ThisCloud.Framework.Loggings.Admin.DTOs;

/// <summary>
/// DTO para respuesta de GET /settings.
/// </summary>
public sealed record LogSettingsDto
{
    /// <summary>
    /// Nivel de logging mínimo global.
    /// </summary>
    public required LogLevel MinimumLevel { get; init; }

    /// <summary>
    /// Configuración del sink de consola.
    /// </summary>
    public required ConsoleSinkDto Console { get; init; }

    /// <summary>
    /// Configuración del sink de archivo.
    /// </summary>
    public required FileSinkDto File { get; init; }

    /// <summary>
    /// Configuración de retención de logs.
    /// </summary>
    public required RetentionDto Retention { get; init; }

    /// <summary>
    /// Configuración de redaction (sanitización de secretos).
    /// </summary>
    public required RedactionDto Redaction { get; init; }

    /// <summary>
    /// Configuración de correlación.
    /// </summary>
    public required CorrelationDto Correlation { get; init; }

    /// <summary>
    /// Configuración de administración.
    /// </summary>
    public required AdminDto Admin { get; init; }

    /// <summary>
    /// Overrides de nivel de logging por namespace.
    /// </summary>
    public required Dictionary<string, LogLevel> Overrides { get; init; }

    /// <summary>
    /// Indica si el logging está habilitado.
    /// </summary>
    public required bool IsEnabled { get; init; }
}

/// <summary>
/// DTO para configuración de consola.
/// </summary>
public sealed record ConsoleSinkDto
{
    /// <summary>
    /// Indica si el sink de consola está habilitado.
    /// </summary>
    public required bool Enabled { get; init; }
}

/// <summary>
/// DTO para configuración de archivo.
/// </summary>
public sealed record FileSinkDto
{
    /// <summary>
    /// Indica si el sink de archivo está habilitado.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Ruta del archivo de logs.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Tamaño máximo de archivo en MB antes de rotar.
    /// </summary>
    public required int RollingFileSizeMb { get; init; }

    /// <summary>
    /// Número máximo de archivos a retener.
    /// </summary>
    public required int RetainedFileCountLimit { get; init; }

    /// <summary>
    /// Indica si se usa formato NDJSON compacto.
    /// </summary>
    public required bool UseCompactJson { get; init; }
}

/// <summary>
/// DTO para configuración de retención.
/// </summary>
public sealed record RetentionDto
{
    /// <summary>
    /// Días de retención de logs (TTL lógico).
    /// </summary>
    public required int Days { get; init; }
}

/// <summary>
/// DTO para configuración de redaction.
/// </summary>
public sealed record RedactionDto
{
    /// <summary>
    /// Indica si la redaction está habilitada.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Patrones adicionales de redaction.
    /// </summary>
    public required string[] AdditionalPatterns { get; init; }
}

/// <summary>
/// DTO para configuración de correlación.
/// </summary>
public sealed record CorrelationDto
{
    /// <summary>
    /// Nombre del header HTTP para correlación.
    /// </summary>
    public required string HeaderName { get; init; }

    /// <summary>
    /// Indica si se genera un CorrelationId si no existe.
    /// </summary>
    public required bool GenerateIfMissing { get; init; }
}

/// <summary>
/// DTO para configuración de administración.
/// </summary>
public sealed record AdminDto
{
    /// <summary>
    /// Indica si los endpoints Admin están habilitados.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Ruta base de los endpoints Admin.
    /// </summary>
    public required string BasePath { get; init; }

    /// <summary>
    /// Entornos permitidos para Admin.
    /// </summary>
    public required string[] AllowedEnvironments { get; init; }

    /// <summary>
    /// Indica si se requiere policy "Admin".
    /// </summary>
    public required bool RequireAdmin { get; init; }

    /// <summary>
    /// Nombre de la policy de autorización.
    /// </summary>
    public required string AdminPolicyName { get; init; }
}

/// <summary>
/// Request para PUT /settings (reemplazo completo).
/// </summary>
public sealed record UpdateLogSettingsRequest
{
    /// <summary>
    /// Nivel de logging mínimo global.
    /// </summary>
    public LogLevel? MinimumLevel { get; init; }

    /// <summary>
    /// Configuración del sink de consola.
    /// </summary>
    public ConsoleSinkDto? Console { get; init; }

    /// <summary>
    /// Configuración del sink de archivo.
    /// </summary>
    public FileSinkDto? File { get; init; }

    /// <summary>
    /// Configuración de retención.
    /// </summary>
    public RetentionDto? Retention { get; init; }

    /// <summary>
    /// Configuración de redaction.
    /// </summary>
    public RedactionDto? Redaction { get; init; }

    /// <summary>
    /// Configuración de correlación.
    /// </summary>
    public CorrelationDto? Correlation { get; init; }

    /// <summary>
    /// Overrides de nivel por namespace.
    /// </summary>
    public Dictionary<string, LogLevel>? Overrides { get; init; }
}

/// <summary>
/// Request para PATCH /settings (merge parcial).
/// </summary>
public sealed record PatchLogSettingsRequest
{
    /// <summary>
    /// Nivel de logging mínimo global (opcional).
    /// </summary>
    public LogLevel? MinimumLevel { get; init; }

    /// <summary>
    /// Cambios parciales en console sink (opcional).
    /// </summary>
    public ConsoleSinkPatchDto? Console { get; init; }

    /// <summary>
    /// Cambios parciales en file sink (opcional).
    /// </summary>
    public FileSinkPatchDto? File { get; init; }

    /// <summary>
    /// Cambios parciales en retention (opcional).
    /// </summary>
    public RetentionPatchDto? Retention { get; init; }

    /// <summary>
    /// Cambios parciales en redaction (opcional).
    /// </summary>
    public RedactionPatchDto? Redaction { get; init; }

    /// <summary>
    /// Cambios parciales en correlation (opcional).
    /// </summary>
    public CorrelationPatchDto? Correlation { get; init; }

    /// <summary>
    /// Overrides a agregar/modificar (no elimina existentes).
    /// </summary>
    public Dictionary<string, LogLevel>? Overrides { get; init; }
}

/// <summary>
/// Cambios parciales en console sink.
/// </summary>
public sealed record ConsoleSinkPatchDto
{
    /// <summary>
    /// Habilitar/deshabilitar console sink.
    /// </summary>
    public bool? Enabled { get; init; }
}

/// <summary>
/// Cambios parciales en file sink.
/// </summary>
public sealed record FileSinkPatchDto
{
    /// <summary>
    /// Habilitar/deshabilitar file sink.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Ruta del archivo.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Tamaño de rolling en MB.
    /// </summary>
    public int? RollingFileSizeMb { get; init; }

    /// <summary>
    /// Límite de archivos retenidos.
    /// </summary>
    public int? RetainedFileCountLimit { get; init; }

    /// <summary>
    /// Formato JSON compacto.
    /// </summary>
    public bool? UseCompactJson { get; init; }
}

/// <summary>
/// Cambios parciales en retention.
/// </summary>
public sealed record RetentionPatchDto
{
    /// <summary>
    /// Días de retención.
    /// </summary>
    public int? Days { get; init; }
}

/// <summary>
/// Cambios parciales en redaction.
/// </summary>
public sealed record RedactionPatchDto
{
    /// <summary>
    /// Habilitar/deshabilitar redaction.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Patrones adicionales.
    /// </summary>
    public string[]? AdditionalPatterns { get; init; }
}

/// <summary>
/// Cambios parciales en correlation.
/// </summary>
public sealed record CorrelationPatchDto
{
    /// <summary>
    /// Nombre del header.
    /// </summary>
    public string? HeaderName { get; init; }

    /// <summary>
    /// Generar si falta.
    /// </summary>
    public bool? GenerateIfMissing { get; init; }
}
