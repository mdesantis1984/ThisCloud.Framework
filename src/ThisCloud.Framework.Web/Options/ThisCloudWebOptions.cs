namespace ThisCloud.Framework.Web.Options;

/// <summary>
/// Opciones de configuración principal para ThisCloud.Framework.Web.
/// </summary>
/// <remarks>
/// Se enlaza desde la sección de configuración "ThisCloud:Web".
/// </remarks>
public class ThisCloudWebOptions
{
    /// <summary>
    /// Nombre del servicio. Requerido en Production.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Opciones de CORS.
    /// </summary>
    public CorsOptions Cors { get; set; } = new();

    /// <summary>
    /// Opciones de Swagger/OpenAPI.
    /// </summary>
    public SwaggerOptions Swagger { get; set; } = new();

    /// <summary>
    /// Opciones de políticas de cookies.
    /// </summary>
    public CookiesOptions Cookies { get; set; } = new();

    /// <summary>
    /// Opciones de compresión de respuestas.
    /// </summary>
    public CompressionOptions Compression { get; set; } = new();
}
