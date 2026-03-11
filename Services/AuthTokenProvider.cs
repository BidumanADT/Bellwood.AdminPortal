using Microsoft.AspNetCore.Components.Authorization;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Provides JWT access and refresh tokens to outbound API services.
///
/// Token source priority:
/// 1. In-memory override (set by TokenRefreshService when tokens are refreshed
///    during an interactive circuit).
/// 2. Claims stored in the auth cookie (set at login via SignInAsync).
///
/// Registered as scoped — each Blazor circuit gets its own instance.
/// No cross-user leakage is possible.
/// No dependency on browser sessionStorage.
/// </summary>
public class AuthTokenProvider : IAuthTokenProvider
{
    private readonly AuthenticationStateProvider _authStateProvider;

    // In-memory overrides set by token refresh within the current circuit.
    // These take priority over cookie claims because the cookie can't be
    // rewritten during a SignalR circuit (no HttpContext).
    private string? _accessTokenOverride;
    private string? _refreshTokenOverride;

    public AuthTokenProvider(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_accessTokenOverride != null)
            return _accessTokenOverride;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst("access_token")?.Value;
    }

    public Task SetTokenAsync(string token)
    {
        _accessTokenOverride = token;
        return Task.CompletedTask;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        if (_refreshTokenOverride != null)
            return _refreshTokenOverride;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst("refresh_token")?.Value;
    }

    public Task SetRefreshTokenAsync(string refreshToken)
    {
        _refreshTokenOverride = refreshToken;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync()
    {
        _accessTokenOverride = null;
        _refreshTokenOverride = null;
        return Task.CompletedTask;
    }
}
