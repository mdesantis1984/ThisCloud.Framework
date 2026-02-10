namespace ThisCloud.Framework.Web.Options;

/// <summary>
/// Opciones de configuración para Swagger/OpenAPI.
/// </summary>
public class SwaggerOptions
{
    /// <summary>
    /// Indica si Swagger está habilitado.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Indica si se requiere el rol/policy "Admin" para acceder a Swagger UI.
    /// </summary>
    public bool RequireAdmin { get; set; }

    /// <summary>
    /// Lista de ambientes donde Swagger puede estar habilitado.
    /// </summary>
    /// <remarks>
    /// Si está vacío, Swagger puede estar habilitado en cualquier ambiente (depende de <see cref="Enabled"/>).
    /// </remarks>
    public string[] AllowedEnvironments { get; set; } = Array.Empty<string>();
}
