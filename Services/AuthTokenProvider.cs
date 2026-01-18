using System.Threading.Tasks;

namespace Bellwood.AdminPortal.Services;

public class AuthTokenProvider : IAuthTokenProvider
{
    private string? _cachedToken;
    private string? _cachedRefreshToken;

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
        _cachedRefreshToken = null;
        return Task.CompletedTask;
    }

    // Phase 2.2: Refresh token support
    public Task<string?> GetRefreshTokenAsync()
        => Task.FromResult(_cachedRefreshToken);

    public Task SetRefreshTokenAsync(string refreshToken)
    {
        _cachedRefreshToken = refreshToken;
        return Task.CompletedTask;
    }
}
