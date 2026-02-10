namespace ThisCloud.Framework.Web.Options;

/// <summary>
/// Opciones de configuración para CORS (Cross-Origin Resource Sharing).
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Indica si CORS está habilitado.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Lista de orígenes permitidos. Requerido si <see cref="Enabled"/> es <c>true</c> en Production.
    /// </summary>
    /// <remarks>
    /// No debe contener "*" si <see cref="AllowCredentials"/> es <c>true</c>.
    /// </remarks>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Indica si se permiten credenciales en solicitudes CORS.
    /// </summary>
    public bool AllowCredentials { get; set; }
}
