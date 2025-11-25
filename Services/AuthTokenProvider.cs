using System.Threading.Tasks;

namespace Bellwood.AdminPortal.Services;

public class AuthTokenProvider : IAuthTokenProvider
{
    private string? _cachedToken;

    public Task<string?> GetTokenAsync()
        => Task.FromResult(_cachedToken);

    public Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync()
    {
        _cachedToken = null;
        return Task.CompletedTask;
    }
}
