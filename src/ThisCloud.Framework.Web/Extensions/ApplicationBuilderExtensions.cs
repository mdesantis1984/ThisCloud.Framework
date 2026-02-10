using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ThisCloud.Framework.Web.Options;

namespace ThisCloud.Framework.Web.Extensions;

/// <summary>
/// Extensiones para <see cref="WebApplication"/> que configuran el pipeline de ThisCloud.Framework.Web.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configura el pipeline de ThisCloud.Framework.Web (CORS, Compression, Cookies).
    /// </summary>
    /// <param name="app">La aplicación web.</param>
    /// <returns>La aplicación web para encadenamiento.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="app"/> es null.</exception>
    public static WebApplication UseThisCloudFrameworkWeb(this WebApplication app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        var options = app.Services.GetRequiredService<IOptions<ThisCloudWebOptions>>().Value;

        // W2.3: Aplicar CORS si está habilitado
        if (options.Cors.Enabled)
        {
            app.UseCors("ThisCloudDefaultCors");
        }

        // W2.3: Aplicar Response Compression si está habilitado
        // TODO Fase 5: Requiere Microsoft.AspNetCore.ResponseCompression NuGet package
        // if (options.Compression.Enabled)
        // {
        //     app.UseResponseCompression();
        // }

        // W2.3: Aplicar Cookie Policy siempre (con defaults seguros)
        app.UseCookiePolicy(new CookiePolicyOptions
        {
            Secure = options.Cookies.SecurePolicy,
            HttpOnly = options.Cookies.HttpOnly ? HttpOnlyPolicy.Always : HttpOnlyPolicy.None,
            MinimumSameSitePolicy = options.Cookies.SameSite
        });

        return app;
    }

    /// <summary>
    /// Configura Swagger UI (placeholder, implementación en Fase 6).
    /// </summary>
    /// <param name="app">La aplicación web.</param>
    /// <returns>La aplicación web para encadenamiento.</returns>
    public static WebApplication UseThisCloudFrameworkSwagger(this WebApplication app)
    {
        // Placeholder para Fase 6
        return app;
    }
}
