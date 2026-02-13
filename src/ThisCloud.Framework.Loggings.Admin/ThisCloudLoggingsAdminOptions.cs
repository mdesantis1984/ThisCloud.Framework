// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

namespace ThisCloud.Framework.Loggings.Admin;

/// <summary>
/// Opciones de configuración para los endpoints de administración de logging.
/// </summary>
/// <remarks>
/// Estas opciones controlan si los endpoints Admin están habilitados, en qué entornos,
/// y si requieren autenticación/autorización.
/// </remarks>
public sealed class ThisCloudLoggingsAdminOptions
{
    /// <summary>
    /// Obtiene o establece si los endpoints de administración están habilitados.
    /// </summary>
    /// <value>
    /// <c>false</c> por defecto. Si <c>false</c>, NO se mapean endpoints.
    /// </value>
    public bool Enabled { get; set; }

    /// <summary>
    /// Obtiene o establece la ruta base para todos los endpoints Admin.
    /// </summary>
    /// <value>
    /// Por defecto: <c>"/api/admin/logging"</c>.
    /// </value>
    public string BasePath { get; set; } = "/api/admin/logging";

    /// <summary>
    /// Obtiene o establece los entornos permitidos para habilitar Admin.
    /// </summary>
    /// <value>
    /// Si <see cref="Enabled"/> es <c>true</c> pero el entorno actual NO está en esta lista,
    /// los endpoints NO se mapean.
    /// </value>
    /// <remarks>
    /// Ejemplo: <c>["Development", "Staging"]</c> (NUNCA incluir "Production" sin protección adicional).
    /// </remarks>
    public string[] AllowedEnvironments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Obtiene o establece si se requiere la policy "Admin" para acceder a los endpoints.
    /// </summary>
    /// <value>
    /// <c>false</c> por defecto. Si <c>true</c>, todos los endpoints requieren autorización con policy "Admin".
    /// </value>
    /// <remarks>
    /// DEBE ser <c>true</c> en entornos donde Admin está habilitado para seguridad.
    /// La policy "Admin" debe configurarse en <c>AddAuthorization()</c>.
    /// </remarks>
    public bool RequireAdmin { get; set; }

    /// <summary>
    /// Obtiene o establece el nombre de la policy de autorización requerida.
    /// </summary>
    /// <value>
    /// Por defecto: <c>"Admin"</c>.
    /// </value>
    /// <remarks>
    /// Solo se usa si <see cref="RequireAdmin"/> es <c>true</c>.
    /// </remarks>
    public string AdminPolicyName { get; set; } = "Admin";
}
