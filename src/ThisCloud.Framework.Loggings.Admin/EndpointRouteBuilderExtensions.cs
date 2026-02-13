// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThisCloud.Framework.Loggings.Abstractions;
using ThisCloud.Framework.Loggings.Admin.DTOs;

namespace ThisCloud.Framework.Loggings.Admin;

/// <summary>
/// Extensiones para mapear endpoints de administración de logging.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Mapea los endpoints de administración de logging (Minimal APIs).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    /// <remarks>
    /// Los endpoints solo se mapean si:
    /// - <c>ThisCloud:Loggings:Admin:Enabled=true</c>
    /// - El entorno actual está en <c>AllowedEnvironments</c>
    /// Si <c>RequireAdmin=true</c>, todos los endpoints requieren policy "Admin".
    /// </remarks>
    public static IEndpointRouteBuilder MapThisCloudFrameworkLoggingsAdmin(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind options desde config
        var options = new ThisCloudLoggingsAdminOptions();
        configuration.GetSection("ThisCloud:Loggings:Admin").Bind(options);

        // Gating: Si Admin no está habilitado, NO mapear nada
        if (!options.Enabled)
        {
            return endpoints;
        }

        // Gating: Verificar entorno
        var environment = endpoints.ServiceProvider.GetService<IHostEnvironment>();
        if (environment != null && options.AllowedEnvironments.Length > 0)
        {
            if (!options.AllowedEnvironments.Contains(environment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
            {
                // Entorno no permitido, NO mapear endpoints
                return endpoints;
            }
        }

        // BasePath desde options
        var basePath = options.BasePath;

        // Grupo con BasePath para todos los endpoints Admin
        var group = endpoints.MapGroup(basePath);

        // Si RequireAdmin=true, aplicar policy a todos los endpoints
        if (options.RequireAdmin)
        {
            group = group.RequireAuthorization(options.AdminPolicyName);
        }

        // GET /settings - Obtener configuración actual
        group.MapGet("/settings", GetSettings)
            .WithName("GetLoggingSettings")
            .WithDescription("Obtiene la configuración de logging actual")
            .Produces<LogSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // PUT /settings - Reemplazar configuración completa
        group.MapPut("/settings", PutSettings)
            .WithName("PutLoggingSettings")
            .WithDescription("Reemplaza la configuración de logging completa")
            .Produces<LogSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // PATCH /settings - Merge parcial de configuración
        group.MapPatch("/settings", PatchSettings)
            .WithName("PatchLoggingSettings")
            .WithDescription("Aplica cambios parciales a la configuración de logging")
            .Produces<LogSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

        // POST /enable - Activar logging
        group.MapPost("/enable", EnableLogging)
            .WithName("EnableLogging")
            .WithDescription("Activa el logging system-wide")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status500InternalServerError);

        // POST /disable - Desactivar logging
        group.MapPost("/disable", DisableLogging)
            .WithName("DisableLogging")
            .WithDescription("Desactiva el logging system-wide")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status500InternalServerError);

        // DELETE /settings - Reset a defaults
        group.MapDelete("/settings", DeleteSettings)
            .WithName("DeleteLoggingSettings")
            .WithDescription("Resetea la configuración de logging a valores por defecto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> GetSettings(
        [FromServices] ILoggingSettingsStore store,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await store.GetSettingsAsync(cancellationToken);
            var dto = MapToDto(settings);
            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al obtener configuración de logging");
        }
    }

    private static async Task<IResult> PutSettings(
        [FromBody] UpdateLogSettingsRequest request,
        [FromServices] ILoggingControlService controlService,
        [FromServices] ILoggingSettingsStore store,
        [FromServices] IAuditLogger auditLogger,
        [FromServices] HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validar request
            var validationError = ValidateUpdateRequest(request);
            if (validationError != null)
            {
                return Results.BadRequest(new { error = validationError });
            }

            // Obtener settings actuales para auditoría
            var currentSettings = await store.GetSettingsAsync(cancellationToken);

            // Construir nuevos settings desde request
            var newSettings = BuildSettingsFromUpdateRequest(request, currentSettings);

            // Aplicar settings
            await controlService.SetSettingsAsync(newSettings, cancellationToken);

            // Auditoría
            var userId = httpContext.User?.Identity?.Name ?? "anonymous";
            var correlationId = httpContext.Items["CorrelationId"] as Guid?;
            await auditLogger.LogAuditEventAsync(
                action: "PutSettings",
                userId: userId,
                correlationId: correlationId,
                details: $"Replaced logging settings",
                cancellationToken: cancellationToken);

            // Retornar nuevos settings
            var updatedSettings = await store.GetSettingsAsync(cancellationToken);
            var dto = MapToDto(updatedSettings);
            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al actualizar configuración de logging");
        }
    }

    private static async Task<IResult> PatchSettings(
        [FromBody] PatchLogSettingsRequest request,
        [FromServices] ILoggingControlService controlService,
        [FromServices] ILoggingSettingsStore store,
        [FromServices] IAuditLogger auditLogger,
        [FromServices] HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validar request
            var validationError = ValidatePatchRequest(request);
            if (validationError != null)
            {
                return Results.BadRequest(new { error = validationError });
            }

            // Obtener settings actuales
            var currentSettings = await store.GetSettingsAsync(cancellationToken);

            // Aplicar merge parcial
            var mergedSettings = MergeSettings(currentSettings, request);

            // Aplicar settings merged
            await controlService.SetSettingsAsync(mergedSettings, cancellationToken);

            // Auditoría
            var userId = httpContext.User?.Identity?.Name ?? "anonymous";
            var correlationId = httpContext.Items["CorrelationId"] as Guid?;
            await auditLogger.LogAuditEventAsync(
                action: "PatchSettings",
                userId: userId,
                correlationId: correlationId,
                details: $"Merged partial settings",
                cancellationToken: cancellationToken);

            // Retornar settings actualizados
            var updatedSettings = await store.GetSettingsAsync(cancellationToken);
            var dto = MapToDto(updatedSettings);
            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al aplicar patch de configuración");
        }
    }

    private static async Task<IResult> EnableLogging(
        [FromServices] ILoggingControlService controlService,
        [FromServices] IAuditLogger auditLogger,
        [FromServices] HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await controlService.EnableAsync(cancellationToken);

            var userId = httpContext.User?.Identity?.Name ?? "anonymous";
            var correlationId = httpContext.Items["CorrelationId"] as Guid?;
            await auditLogger.LogAuditEventAsync(
                action: "EnableLogging",
                userId: userId,
                correlationId: correlationId,
                details: "Logging enabled system-wide",
                cancellationToken: cancellationToken);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al habilitar logging");
        }
    }

    private static async Task<IResult> DisableLogging(
        [FromServices] ILoggingControlService controlService,
        [FromServices] IAuditLogger auditLogger,
        [FromServices] HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await controlService.DisableAsync(cancellationToken);

            var userId = httpContext.User?.Identity?.Name ?? "anonymous";
            var correlationId = httpContext.Items["CorrelationId"] as Guid?;
            await auditLogger.LogAuditEventAsync(
                action: "DisableLogging",
                userId: userId,
                correlationId: correlationId,
                details: "Logging disabled system-wide",
                cancellationToken: cancellationToken);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al deshabilitar logging");
        }
    }

    private static async Task<IResult> DeleteSettings(
        [FromServices] ILoggingControlService controlService,
        [FromServices] IAuditLogger auditLogger,
        [FromServices] HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await controlService.ResetSettingsAsync(cancellationToken);

            var userId = httpContext.User?.Identity?.Name ?? "anonymous";
            var correlationId = httpContext.Items["CorrelationId"] as Guid?;
            await auditLogger.LogAuditEventAsync(
                action: "ResetSettings",
                userId: userId,
                correlationId: correlationId,
                details: "Reset settings to defaults",
                cancellationToken: cancellationToken);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error al resetear configuración");
        }
    }

    // Mapping helpers
    private static LogSettingsDto MapToDto(LogSettings settings)
    {
        return new LogSettingsDto
        {
            MinimumLevel = settings.MinimumLevel,
            Console = new ConsoleSinkDto { Enabled = settings.Console.Enabled },
            File = new FileSinkDto
            {
                Enabled = settings.File.Enabled,
                Path = settings.File.Path,
                RollingFileSizeMb = settings.File.RollingFileSizeMb,
                RetainedFileCountLimit = settings.File.RetainedFileCountLimit,
                UseCompactJson = settings.File.UseCompactJson
            },
            Retention = new RetentionDto { Days = settings.Retention.Days },
            Redaction = new RedactionDto
            {
                Enabled = settings.Redaction.Enabled,
                AdditionalPatterns = settings.Redaction.AdditionalPatterns?.ToArray() ?? Array.Empty<string>()
            },
            Correlation = new CorrelationDto
            {
                HeaderName = settings.Correlation.HeaderName,
                GenerateIfMissing = settings.Correlation.GenerateIfMissing
            },
            Admin = new AdminDto
            {
                Enabled = false, // Admin settings no se exponen directamente
                BasePath = "/api/admin/logging",
                AllowedEnvironments = Array.Empty<string>(),
                RequireAdmin = false,
                AdminPolicyName = "Admin"
            },
            Overrides = settings.Overrides != null
                ? new Dictionary<string, LogLevel>(settings.Overrides)
                : new Dictionary<string, LogLevel>(),
            IsEnabled = settings.IsEnabled
        };
    }

    private static LogSettings BuildSettingsFromUpdateRequest(
        UpdateLogSettingsRequest request,
        LogSettings currentSettings)
    {
        return new LogSettings
        {
            IsEnabled = currentSettings.IsEnabled,
            MinimumLevel = request.MinimumLevel ?? currentSettings.MinimumLevel,
            Overrides = request.Overrides ?? currentSettings.Overrides,
            Console = new ConsoleSinkSettings
            {
                Enabled = request.Console?.Enabled ?? currentSettings.Console.Enabled
            },
            File = new FileSinkSettings
            {
                Enabled = request.File?.Enabled ?? currentSettings.File.Enabled,
                Path = request.File?.Path ?? currentSettings.File.Path,
                RollingFileSizeMb = request.File?.RollingFileSizeMb ?? currentSettings.File.RollingFileSizeMb,
                RetainedFileCountLimit = request.File?.RetainedFileCountLimit ?? currentSettings.File.RetainedFileCountLimit,
                UseCompactJson = request.File?.UseCompactJson ?? currentSettings.File.UseCompactJson
            },
            Retention = new RetentionSettings
            {
                Days = request.Retention?.Days ?? currentSettings.Retention.Days
            },
            Redaction = new RedactionSettings
            {
                Enabled = request.Redaction?.Enabled ?? currentSettings.Redaction.Enabled,
                AdditionalPatterns = request.Redaction?.AdditionalPatterns ?? currentSettings.Redaction.AdditionalPatterns
            },
            Correlation = new CorrelationSettings
            {
                HeaderName = request.Correlation?.HeaderName ?? currentSettings.Correlation.HeaderName,
                GenerateIfMissing = request.Correlation?.GenerateIfMissing ?? currentSettings.Correlation.GenerateIfMissing
            }
        };
    }

    private static LogSettings MergeSettings(LogSettings current, PatchLogSettingsRequest patch)
    {
        var merged = new LogSettings
        {
            IsEnabled = current.IsEnabled,
            MinimumLevel = patch.MinimumLevel ?? current.MinimumLevel,
            Console = new ConsoleSinkSettings
            {
                Enabled = patch.Console?.Enabled ?? current.Console.Enabled
            },
            File = new FileSinkSettings
            {
                Enabled = patch.File?.Enabled ?? current.File.Enabled,
                Path = patch.File?.Path ?? current.File.Path,
                RollingFileSizeMb = patch.File?.RollingFileSizeMb ?? current.File.RollingFileSizeMb,
                RetainedFileCountLimit = patch.File?.RetainedFileCountLimit ?? current.File.RetainedFileCountLimit,
                UseCompactJson = patch.File?.UseCompactJson ?? current.File.UseCompactJson
            },
            Retention = new RetentionSettings
            {
                Days = patch.Retention?.Days ?? current.Retention.Days
            },
            Redaction = new RedactionSettings
            {
                Enabled = patch.Redaction?.Enabled ?? current.Redaction.Enabled,
                AdditionalPatterns = patch.Redaction?.AdditionalPatterns ?? current.Redaction.AdditionalPatterns
            },
            Correlation = new CorrelationSettings
            {
                HeaderName = patch.Correlation?.HeaderName ?? current.Correlation.HeaderName,
                GenerateIfMissing = patch.Correlation?.GenerateIfMissing ?? current.Correlation.GenerateIfMissing
            }
        };

        // Merge overrides (add/update, no remove)
        if (patch.Overrides != null && patch.Overrides.Count > 0)
        {
            var mergedOverrides = current.Overrides != null
                ? new Dictionary<string, LogLevel>(current.Overrides)
                : new Dictionary<string, LogLevel>();

            foreach (var kvp in patch.Overrides)
            {
                mergedOverrides[kvp.Key] = kvp.Value;
            }

            merged.Overrides = mergedOverrides;
        }
        else
        {
            merged.Overrides = current.Overrides;
        }

        return merged;
    }

    // Validation helpers
    private static string? ValidateUpdateRequest(UpdateLogSettingsRequest request)
    {
        if (request.File != null)
        {
            if (request.File.RollingFileSizeMb < 1 || request.File.RollingFileSizeMb > 100)
            {
                return "File.RollingFileSizeMb must be between 1 and 100";
            }

            if (request.File.RetainedFileCountLimit < 1 || request.File.RetainedFileCountLimit > 365)
            {
                return "File.RetainedFileCountLimit must be between 1 and 365";
            }

            if (string.IsNullOrWhiteSpace(request.File.Path))
            {
                return "File.Path cannot be empty when File settings provided";
            }
        }

        if (request.Retention != null)
        {
            if (request.Retention.Days < 1 || request.Retention.Days > 3650)
            {
                return "Retention.Days must be between 1 and 3650";
            }
        }

        return null;
    }

    private static string? ValidatePatchRequest(PatchLogSettingsRequest request)
    {
        if (request.File != null)
        {
            if (request.File.RollingFileSizeMb.HasValue)
            {
                if (request.File.RollingFileSizeMb.Value < 1 || request.File.RollingFileSizeMb.Value > 100)
                {
                    return "File.RollingFileSizeMb must be between 1 and 100";
                }
            }

            if (request.File.RetainedFileCountLimit.HasValue)
            {
                if (request.File.RetainedFileCountLimit.Value < 1 || request.File.RetainedFileCountLimit.Value > 365)
                {
                    return "File.RetainedFileCountLimit must be between 1 and 365";
                }
            }

            if (request.File.Path != null && string.IsNullOrWhiteSpace(request.File.Path))
            {
                return "File.Path cannot be empty";
            }
        }

        if (request.Retention != null)
        {
            if (request.Retention.Days.HasValue)
            {
                if (request.Retention.Days.Value < 1 || request.Retention.Days.Value > 3650)
                {
                    return "Retention.Days must be between 1 and 3650";
                }
            }
        }

        return null;
    }
}
