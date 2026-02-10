namespace ThisCloud.Framework.Web.Middlewares;

using Microsoft.AspNetCore.Http;
using ThisCloud.Framework.Contracts.Web;

/// <summary>
/// Middleware que gestiona el encabezado X-Request-Id para identificación única de solicitudes.
/// </summary>
/// <remarks>
/// Lee el encabezado X-Request-Id de la solicitud entrante. Si el valor no es un GUID válido o está ausente,
/// genera un nuevo GUID. Siempre escribe el encabezado en la respuesta y almacena el valor en HttpContext.Items.
/// </remarks>
public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="RequestIdMiddleware"/>.
    /// </summary>
    /// <param name="next">El siguiente middleware en el pipeline.</param>
    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Procesa la solicitud HTTP gestionando el Request ID.
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        Guid requestId;

        // Intentar leer el header entrante
        if (context.Request.Headers.TryGetValue(ThisCloudHeaders.RequestId, out var headerValue) &&
            Guid.TryParse(headerValue, out var parsedId))
        {
            requestId = parsedId;
        }
        else
        {
            // Generar nuevo GUID si falta o no es válido
            requestId = Guid.NewGuid();
        }

        // Almacenar en Items para acceso posterior
        context.Items["RequestId"] = requestId;

        // Escribir header en respuesta
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(ThisCloudHeaders.RequestId))
            {
                context.Response.Headers[ThisCloudHeaders.RequestId] = requestId.ToString();
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
