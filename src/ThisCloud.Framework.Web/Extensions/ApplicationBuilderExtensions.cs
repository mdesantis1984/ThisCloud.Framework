using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ThisCloud.Framework.Web.Middlewares;
using ThisCloud.Framework.Web.Options;

namespace ThisCloud.Framework.Web.Extensions;

/// <summary>
/// Extensiones para <see cref="WebApplication"/> que configuran el pipeline de ThisCloud.Framework.Web.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configura el pipeline de ThisCloud.Framework.Web (Exception Mapping, Correlation/RequestId, CORS, Compression, Cookies).
    /// </summary>
    /// <param name="app">La aplicación web.</param>
    /// <returns>La aplicación web para encadenamiento.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="app"/> es null.</exception>
    public static WebApplication UseThisCloudFrameworkWeb(this WebApplication app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        var options = app.Services.GetRequiredService<IOptions<ThisCloudWebOptions>>().Value;

        // W4.1: Exception mapping global (debe ir primero en el pipeline)
        app.UseMiddleware<ExceptionMappingMiddleware>();

        // W3.1: Correlation ID middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        // W3.2: Request ID middleware
        app.UseMiddleware<RequestIdMiddleware>();

        // W2.3: Aplicar CORS si está habilitado
        if (options.Cors.Enabled)
        {
            app.UseCors("ThisCloudDefaultCors");
        }

        // W5.2: ResponseCompression NO IMPLEMENTADO (ver ServiceCollectionExtensions para detalles)
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
    /// Configura Swagger UI con gating por ambiente y configuración.
    /// </summary>
    /// <param name="app">La aplicación web.</param>
    /// <returns>La aplicación web para encadenamiento.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="app"/> es null.</exception>
    public static WebApplication UseThisCloudFrameworkSwagger(this WebApplication app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        var options = app.Services.GetRequiredService<IOptions<ThisCloudWebOptions>>().Value;

        // W6.4: Gating - Si Swagger no está habilitado, no mapear nada
        if (!options.Swagger.Enabled)
        {
            return app;
        }

        // W6.4: Gating por ambiente - Solo habilitar si env está en AllowedEnvironments
        var currentEnvironment = app.Environment.EnvironmentName;
        var allowedEnvironments = options.Swagger.AllowedEnvironments ?? Array.Empty<string>();

        if (allowedEnvironments.Length > 0 && !allowedEnvironments.Contains(currentEnvironment, StringComparer.OrdinalIgnoreCase))
        {
            return app;
        }

        // W6.3: Proteger /swagger/* si RequireAdmin=true
        if (options.Swagger.RequireAdmin)
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                {
                    var authService = context.RequestServices.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
                    if (authService != null)
                    {
                        var authResult = await authService.AuthorizeAsync(context.User, null, "Admin");
                        if (!authResult.Succeeded)
                        {
                            context.Response.StatusCode = 403;
                            return;
                        }
                    }
                }
                await next(context);
            });
        }

        // W6.1: Aplicar Swagger middleware
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
        });

        return app;
    }
}
