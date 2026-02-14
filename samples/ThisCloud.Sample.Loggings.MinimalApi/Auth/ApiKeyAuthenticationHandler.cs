// Copyright (c) 2025 Marco Alejandro De Santis. Licensed under the ISC License.
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ThisCloud.Sample.Loggings.MinimalApi.Auth;

/// <summary>
/// Simple API Key authentication handler for sample purposes.
/// NOT for production use - use proper identity provider (Azure AD, IdentityServer, etc.).
/// </summary>
/// <remarks>
/// Reads API key from header "X-Admin-ApiKey" and compares with expected value from configuration.
/// Expected key MUST come from env var or user-secrets (never hardcoded or versioned).
/// </remarks>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-Admin-ApiKey";
    private const string ConfigurationKey = "SAMPLE_ADMIN_APIKEY";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get expected API key from configuration (env var / user-secrets)
        var expectedApiKey = _configuration[ConfigurationKey];

        // If no expected key configured, deny (fail-safe)
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            Logger.LogWarning("API Key authentication failed: {ConfigKey} not configured", ConfigurationKey);
            return Task.FromResult(AuthenticateResult.Fail("API Key not configured"));
        }

        // Get API key from request header
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API Key header"));
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Empty API Key header"));
        }

        // Compare keys (constant-time comparison to prevent timing attacks)
        if (!CryptographicEquals(providedApiKey, expectedApiKey))
        {
            Logger.LogWarning("API Key authentication failed: invalid key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        // Success: create claims principal with Admin role
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "SampleApiKeyUser"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("API Key authentication succeeded");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool CryptographicEquals(string a, string b)
    {
        if (a == null || b == null)
        {
            return a == b;
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        var areEqual = true;
        for (var i = 0; i < a.Length; i++)
        {
            areEqual &= a[i] == b[i];
        }

        return areEqual;
    }
}
