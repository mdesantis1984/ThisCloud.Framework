namespace ThisCloud.Framework.Web.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using ThisCloud.Framework.Contracts.Exceptions;
using ThisCloud.Framework.Contracts.Web;
using ThisCloud.Framework.Web.Helpers;
using ThisCloud.Framework.Web.Options;

/// <summary>
/// Middleware que captura excepciones globales y las convierte en respuestas ApiEnvelope estandarizadas.
/// </summary>
public class ExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ThisCloudWebOptions _options;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ExceptionMappingMiddleware"/>.
    /// </summary>
    /// <param name="next">El siguiente middleware en el pipeline.</param>
    /// <param name="options">Opciones del framework.</param>
    public ExceptionMappingMiddleware(RequestDelegate next, IOptions<ThisCloudWebOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Ejecuta el middleware, capturando cualquier excepción y mapeándola a una respuesta estandarizada.
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Mapeo de excepción a código de error y status HTTP
        var (statusCode, errorCode, title) = MapException(exception);

        // Construcción de Meta
        var correlationId = ThisCloudHttpContext.GetCorrelationId(context);
        var requestId = ThisCloudHttpContext.GetRequestId(context);
        var traceId = ThisCloudHttpContext.GetTraceId(context);

        var meta = new Meta(_options.ServiceName ?? "unknown", "v1")
        {
            TimestampUtc = DateTime.UtcNow,
            CorrelationId = correlationId != Guid.Empty ? correlationId : Guid.NewGuid(),
            RequestId = requestId != Guid.Empty ? requestId : Guid.NewGuid(),
            TraceId = traceId ?? "00-00000000000000000000000000000000-0000000000000000-00"
        };

        // Construcción de ErrorItem (ProblemDetailsDto)
        var errorItem = new ErrorItem
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        // Si es ValidationException, agregar validationErrors en extensions
        if (exception is ValidationException validationEx && validationEx.ValidationErrors != null)
        {
            errorItem.AddExtension("errors", validationEx.ValidationErrors);
        }

        // Construcción del envelope
        var envelope = new ApiEnvelope<object?>
        {
            Meta = meta,
            Data = null,
            Errors = new List<ErrorItem> { errorItem }
        };

        // Escribir respuesta
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }

    private static (int statusCode, string errorCode, string title) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException => (400, "VALIDATION_ERROR", "Validation Error"),
            NotFoundException => (404, "NOT_FOUND", "Not Found"),
            ConflictException => (409, "CONFLICT", "Conflict"),
            ForbiddenException => (403, "FORBIDDEN", "Forbidden"),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", "Unauthorized"),
            HttpRequestException => (502, "UPSTREAM_FAILURE", "Bad Gateway"),
            TimeoutException => (504, "UPSTREAM_TIMEOUT", "Gateway Timeout"),
            _ => (500, "UNHANDLED_ERROR", "Internal Server Error")
        };
    }
}
