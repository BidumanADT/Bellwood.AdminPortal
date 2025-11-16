using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Bellwood.AdminPortal.Services;

public class AuthTokenProvider : IAuthTokenProvider
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private string? _cachedToken;

    public AuthTokenProvider(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
            return _cachedToken;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _cachedToken = user.FindFirst("access_token")?.Value;
            return _cachedToken;
        }

        return null;
    }

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