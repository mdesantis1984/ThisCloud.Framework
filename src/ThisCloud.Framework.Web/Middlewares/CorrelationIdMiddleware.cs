namespace ThisCloud.Framework.Web.Middlewares;

using Microsoft.AspNetCore.Http;
using ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Middleware que gestiona el encabezado X-Correlation-Id para trazabilidad de solicitudes.
/// </summary>
/// <remarks>
/// Lee el encabezado X-Correlation-Id de la solicitud entrante. Si el valor no es un GUID válido o está ausente,
/// genera un nuevo GUID. Siempre escribe el encabezado en la respuesta y almacena el valor en HttpContext.Items.
/// </remarks>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="CorrelationIdMiddleware"/>.
    /// </summary>
    /// <param name="next">El siguiente middleware en el pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Procesa la solicitud HTTP gestionando el Correlation ID.
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        Guid correlationId;

        // Intentar leer el header entrante
        if (context.Request.Headers.TryGetValue(ThisCloudHeaders.CorrelationId, out var headerValue) &&
            Guid.TryParse(headerValue, out var parsedId))
        {
            correlationId = parsedId;
        }
        else
        {
            // Generar nuevo GUID si falta o no es válido
            correlationId = Guid.NewGuid();
        }

        // Almacenar en Items para acceso posterior
        context.Items["CorrelationId"] = correlationId;

        // Escribir header en respuesta
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(ThisCloudHeaders.CorrelationId))
            {
                context.Response.Headers[ThisCloudHeaders.CorrelationId] = correlationId.ToString();
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
