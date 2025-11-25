namespace Bellwood.AdminPortal.Services;

public interface IAdminApiKeyProvider
{
    string? GetApiKey();
}

public class AdminApiKeyProvider : IAdminApiKeyProvider
{
    private readonly IConfiguration _config;
    public AdminApiKeyProvider(IConfiguration config) => _config = config;

    public string? GetApiKey()
        => _config["AdminApi:ApiKey"];
}