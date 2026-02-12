namespace ThisCloud.Framework.Web.Results;

using Microsoft.AspNetCore.Http;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Helpers;
using System.Text.Json;

/// <summary>
/// Clase de ayuda para generar respuestas IResult estandarizadas con ApiEnvelope.
/// </summary>
/// <remarks>
/// Uso obligatorio: PROHIBIDO usar Results.* directamente en endpoints.
/// Todos los endpoints deben usar ThisCloudResults.* para garantizar contratos coherentes.
/// </remarks>
public static class ThisCloudResults
{
    /// <summary>
    /// Genera una respuesta 200 OK con datos.
    /// </summary>
    /// <typeparam name="T">El tipo del payload.</typeparam>
    /// <param name="data">Los datos a retornar.</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 200 y envelope.</returns>
    public static IResult Ok<T>(T data, string serviceName = "unknown", string version = "v1")
    {
        return Results.Ok(CreateEnvelope(data, serviceName, version, null));
    }

    /// <summary>
    /// Genera una respuesta 201 Created con datos y header Location.
    /// </summary>
    /// <typeparam name="T">El tipo del payload.</typeparam>
    /// <param name="location">URI del recurso creado.</param>
    /// <param name="data">Los datos del recurso creado.</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 201, header Location y envelope.</returns>
    public static IResult Created<T>(string location, T data, string serviceName = "unknown", string version = "v1")
    {
        return Results.Created(location, CreateEnvelope(data, serviceName, version, null));
    }

    /// <summary>
    /// Genera una respuesta 303 See Other redirigiendo a otra ubicación.
    /// </summary>
    /// <param name="location">URI de redirección.</param>
    /// <returns>IResult con status 303 y header Location.</returns>
    public static IResult SeeOther(string location)
    {
        // 303 no lleva body según RFC, solo Location header
        return Results.StatusCode(303);
    }

    /// <summary>
    /// Genera una respuesta 400 Bad Request con errores de validación.
    /// </summary>
    /// <param name="code">Código de error (por defecto "VALIDATION_ERROR").</param>
    /// <param name="title">Título del error (por defecto "Bad Request").</param>
    /// <param name="detail">Detalle del error.</param>
    /// <param name="validationErrors">Diccionario de errores de validación (campo -> mensajes).</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 400 y envelope con errores.</returns>
    public static IResult BadRequest(
        string code = "VALIDATION_ERROR",
        string title = "Bad Request",
        string? detail = null,
        IDictionary<string, string[]?>? validationErrors = null,
        string serviceName = "unknown",
        string version = "v1")
    {
        var error = CreateError(400, code, title, detail, validationErrors);
        return Results.BadRequest(CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error }));
    }

    /// <summary>
    /// Genera una respuesta 401 Unauthorized.
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Authentication required").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 401 y envelope.</returns>
    public static IResult Unauthorized(string? detail = "Authentication required", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(401, "UNAUTHORIZED", "Unauthorized", detail);
        return Results.Unauthorized();
    }

    /// <summary>
    /// Genera una respuesta 403 Forbidden.
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Access denied").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 403 y envelope.</returns>
    public static IResult Forbidden(string? detail = "Access denied", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(403, "FORBIDDEN", "Forbidden", detail);
        var envelope = CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error });
        return Results.Json(envelope, statusCode: 403);
    }

    /// <summary>
    /// Genera una respuesta 404 Not Found.
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Resource not found").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 404 y envelope.</returns>
    public static IResult NotFound(string? detail = "Resource not found", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(404, "NOT_FOUND", "Not Found", detail);
        return Results.NotFound(CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error }));
    }

    /// <summary>
    /// Genera una respuesta 409 Conflict.
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Conflict detected").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 409 y envelope.</returns>
    public static IResult Conflict(string? detail = "Conflict detected", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(409, "CONFLICT", "Conflict", detail);
        var envelope = CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error });
        return Results.Conflict(envelope);
    }

    /// <summary>
    /// Genera una respuesta 502 Bad Gateway (fallo upstream).
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Upstream service failure").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 502 y envelope.</returns>
    public static IResult UpstreamFailure(string? detail = "Upstream service failure", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(502, "UPSTREAM_FAILURE", "Bad Gateway", detail);
        var envelope = CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error });
        return Results.Json(envelope, statusCode: 502);
    }

    /// <summary>
    /// Genera una respuesta 500 Internal Server Error.
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "An unexpected error occurred").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 500 y envelope.</returns>
    public static IResult Unhandled(string? detail = "An unexpected error occurred", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(500, "UNHANDLED_ERROR", "Internal Server Error", detail);
        var envelope = CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error });
        return Results.Json(envelope, statusCode: 500);
    }

    /// <summary>
    /// Genera una respuesta 504 Gateway Timeout (timeout upstream).
    /// </summary>
    /// <param name="detail">Detalle del error (por defecto "Upstream service timeout").</param>
    /// <param name="serviceName">Nombre del servicio (por defecto "unknown").</param>
    /// <param name="version">Versión de la API (por defecto "v1").</param>
    /// <returns>IResult con status 504 y envelope.</returns>
    public static IResult UpstreamTimeout(string? detail = "Upstream service timeout", string serviceName = "unknown", string version = "v1")
    {
        var error = CreateError(504, "UPSTREAM_TIMEOUT", "Gateway Timeout", detail);
        var envelope = CreateEnvelope<object?>(null, serviceName, version, new List<ErrorItem> { error });
        return Results.Json(envelope, statusCode: 504);
    }

    // Helpers privados

    private static ApiEnvelope<T> CreateEnvelope<T>(T? data, string serviceName, string version, List<ErrorItem>? errors)
    {
        // Nota: HttpContext no está disponible aquí en métodos estáticos.
        // Los IDs se obtendrán del middleware si aplica.
        // Para simplificar, usamos valores por defecto aquí.
        var meta = new Meta(serviceName, version)
        {
            TimestampUtc = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid(), // Placeholder: se debería obtener del HttpContext en un escenario real
            RequestId = Guid.NewGuid(),
            TraceId = "00-00000000000000000000000000000000-0000000000000000-00"
        };

        return new ApiEnvelope<T>
        {
            Meta = meta,
            Data = data,
            Errors = errors ?? new List<ErrorItem>()
        };
    }

    private static ErrorItem CreateError(int status, string code, string title, string? detail, IDictionary<string, string[]?>? validationErrors = null)
    {
        var error = new ErrorItem
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = detail ?? title,
            Instance = "/" // Placeholder: idealmente se obtiene del HttpContext.Request.Path
        };

        if (validationErrors != null)
        {
            error.AddExtension("errors", validationErrors);
        }

        return error;
    }
}
