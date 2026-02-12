using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;

namespace ThisCloud.Framework.Web.Options;

/// <summary>
/// Opciones de configuración para políticas de cookies.
/// </summary>
public class CookiesOptions
{
    /// <summary>
    /// Política de seguridad para cookies (Secure flag).
    /// </summary>
    /// <remarks>
    /// En Production debe ser <see cref="CookieSecurePolicy.Always"/>.
    /// </remarks>
    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.SameAsRequest;

    /// <summary>
    /// Política SameSite para cookies.
    /// </summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// Indica si las cookies deben tener la flag HttpOnly (no accesibles desde JavaScript).
    /// </summary>
    public bool HttpOnly { get; set; } = true;
}
