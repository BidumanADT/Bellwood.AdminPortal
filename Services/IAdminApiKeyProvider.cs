namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Provides API key for AdminAPI authentication.
/// WARNING: This is a server-side service only. The API key is NEVER exposed to the browser.
/// Blazor Server runs on the server, so this service executes server-side and adds the
/// API key to HTTP headers when calling AdminAPI.
/// </summary>
/// <remarks>
/// For production deployments, migrate the API key storage from appsettings.json to:
/// - Azure Key Vault
/// - AWS Secrets Manager
/// - Environment Variables
/// - Kubernetes Secrets
/// This ensures the API key is not stored in source control or configuration files.
/// </remarks>
public interface IAdminApiKeyProvider
{
    /// <summary>
    /// Retrieves the AdminAPI key from configuration (server-side only).
    /// </summary>
    /// <returns>The API key, or null if not configured.</returns>
    string? GetApiKey();
}

/// <summary>
/// Default implementation that reads the API key from appsettings.json.
/// This is safe for development but should be replaced with a secure secret store in production.
/// </summary>
public class AdminApiKeyProvider : IAdminApiKeyProvider
{
    private readonly IConfiguration _config;
    
    public AdminApiKeyProvider(IConfiguration config) => _config = config;

    /// <inheritdoc />
    public string? GetApiKey()
        => _config["AdminApi:ApiKey"];
}