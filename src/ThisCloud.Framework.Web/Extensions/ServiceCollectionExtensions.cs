using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using ThisCloud.Framework.Web.Options;

namespace ThisCloud.Framework.Web.Extensions;

/// <summary>
/// Extensiones para <see cref="IServiceCollection"/> que registran servicios de ThisCloud.Framework.Web.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios principales de ThisCloud.Framework.Web.
    /// </summary>
    /// <param name="services">La colección de servicios.</param>
    /// <param name="configuration">La configuración de la aplicación.</param>
    /// <param name="serviceName">Nombre del servicio (requerido en Production).</param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="services"/> o <paramref name="configuration"/> es null.</exception>
    public static IServiceCollection AddThisCloudFrameworkWeb(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Bind options desde "ThisCloud:Web"
        var optionsSection = configuration.GetSection("ThisCloud:Web");
        services.Configure<ThisCloudWebOptions>(optionsSection);

        // Validación con IValidateOptions
        services.AddSingleton<IValidateOptions<ThisCloudWebOptions>, ThisCloudWebOptionsValidator>();

        // Registrar ProblemDetails built-in (compatibilidad)
        services.AddProblemDetails();

        // Determinar si estamos en Production
        var env = services.BuildServiceProvider().GetService<IHostEnvironment>();
        var isProduction = env?.IsProduction() ?? false;

        // Validación eager: bind y validar en startup
        var options = new ThisCloudWebOptions();
        optionsSection.Bind(options);

        // Override ServiceName si se pasa explícitamente
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            options.ServiceName = serviceName;
        }

        // Validar
        var validator = new ThisCloudWebOptionsValidator(env);
        var validationResult = validator.Validate("ThisCloud:Web", options);
        if (validationResult.Failed)
        {
            throw new InvalidOperationException($"Configuration validation failed: {validationResult.FailureMessage}");
        }

        // W2.3: Registrar CORS si está habilitado
        if (options.Cors.Enabled)
        {
            services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy("ThisCloudDefaultCors", policy =>
                {
                    policy.WithOrigins(options.Cors.AllowedOrigins);

                    if (options.Cors.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                    else
                    {
                        policy.DisallowCredentials();
                    }

                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                });
            });
        }

        // W5.2: ResponseCompression NO IMPLEMENTADO
        // Decisión: Microsoft.AspNetCore.ResponseCompression no está disponible en .NET 10
        // Package legacy 2.3.9 genera NU1510 y métodos AddResponseCompression/UseResponseCompression no existen
        // API de compression built-in en .NET 10 requiere investigación adicional fuera del alcance de W5.2
        // CompressionOptions.Enabled permanece como placeholder para futura implementación

        return services;
    }
}

/// <summary>
/// Validador para <see cref="ThisCloudWebOptions"/>.
/// </summary>
internal class ThisCloudWebOptionsValidator : IValidateOptions<ThisCloudWebOptions>
{
    private readonly IHostEnvironment? _environment;

    public ThisCloudWebOptionsValidator(IHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, ThisCloudWebOptions options)
    {
        var isProduction = _environment?.IsProduction() ?? false;

        // Production: ServiceName requerido
        if (isProduction && string.IsNullOrWhiteSpace(options.ServiceName))
        {
            return ValidateOptionsResult.Fail("ServiceName is required in Production environment.");
        }

        // Production + CORS enabled => AllowedOrigins no vacío
        if (isProduction && options.Cors.Enabled)
        {
            if (options.Cors.AllowedOrigins == null || options.Cors.AllowedOrigins.Length == 0)
            {
                return ValidateOptionsResult.Fail("CORS is enabled in Production but AllowedOrigins is empty.");
            }

            // Si AllowCredentials=true, no puede haber "*" en AllowedOrigins
            if (options.Cors.AllowCredentials && options.Cors.AllowedOrigins.Contains("*"))
            {
                return ValidateOptionsResult.Fail("CORS with AllowCredentials cannot use wildcard '*' in AllowedOrigins.");
            }
        }

        // Production => Cookies.SecurePolicy debe ser Always
        if (isProduction && options.Cookies.SecurePolicy != CookieSecurePolicy.Always)
        {
            return ValidateOptionsResult.Fail("Cookies.SecurePolicy must be 'Always' in Production environment.");
        }

        return ValidateOptionsResult.Success;
    }
}
