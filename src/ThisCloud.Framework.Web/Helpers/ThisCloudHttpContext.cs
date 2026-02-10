namespace ThisCloud.Framework.Web.Helpers;

using Microsoft.AspNetCore.Http;
using System.Diagnostics;

/// <summary>
/// Clase de ayuda para acceder a metadatos de solicitud almacenados en HttpContext.
/// </summary>
public static class ThisCloudHttpContext
{
    /// <summary>
    /// Obtiene el Correlation ID de la solicitud actual.
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    /// <returns>El Correlation ID como GUID, o Guid.Empty si no está disponible.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="context"/> es null.</exception>
    public static Guid GetCorrelationId(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Items.TryGetValue("CorrelationId", out var value) && value is Guid guid)
        {
            return guid;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Obtiene el Request ID de la solicitud actual.
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    /// <returns>El Request ID como GUID, o Guid.Empty si no está disponible.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="context"/> es null.</exception>
    public static Guid GetRequestId(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Items.TryGetValue("RequestId", out var value) && value is Guid guid)
        {
            return guid;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Obtiene el Trace ID de la actividad actual (W3C TraceId).
    /// </summary>
    /// <param name="context">El contexto HTTP de la solicitud.</param>
    /// <returns>El Trace ID como string, o null si no hay actividad activa.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="context"/> es null.</exception>
    public static string? GetTraceId(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var activity = Activity.Current;
        return activity?.TraceId.ToString();
    }
}
