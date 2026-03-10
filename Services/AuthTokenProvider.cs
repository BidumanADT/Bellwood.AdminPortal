using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Stores the JWT access token and refresh token in ProtectedSessionStorage.
///
/// Security properties:
/// - Encrypted server-side by ASP.NET Core Data Protection before the opaque
///   blob is handed to the browser.  The raw token is never visible in DevTools.
/// - Scoped to the browser *session* (sessionStorage under the hood): closing
///   the tab or the browser clears the data automatically.
/// - Completely isolated per browser / device / incognito window — different
///   tabs to the same URL each have their own sessionStorage partition.
/// - Registered AddScoped in Program.cs, so each Blazor circuit gets its own
///   instance.  No sharing between users.
///
/// The in-memory fields act as a write-through cache: once the token has been
/// read from storage inside a single circuit lifetime we avoid re-reading on
/// every call.  The cache is populated either on the first GetTokenAsync call
/// (lazy) or by SetTokenAsync / ClearTokenAsync writes.
/// </summary>
public class AuthTokenProvider : IAuthTokenProvider
{
    private const string AccessTokenKey  = "bw_access_token";
    private const string RefreshTokenKey = "bw_refresh_token";

    private readonly ProtectedSessionStorage _sessionStorage;

    private string? _cachedToken;
    private string? _cachedRefreshToken;
    // Track whether we have already read from storage in this circuit instance.
    private bool _storageReadForAccess;
    private bool _storageReadForRefresh;

    public AuthTokenProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!_storageReadForAccess)
        {
            _storageReadForAccess = true;
            try
            {
                var result = await _sessionStorage.GetAsync<string>(AccessTokenKey);
                _cachedToken = result.Success ? result.Value : null;
            }
            catch
            {
                // Storage may be unavailable during pre-render; treat as empty.
                _cachedToken = null;
            }
        }
        return _cachedToken;
    }

    public async Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        _storageReadForAccess = true;
        try
        {
            await _sessionStorage.SetAsync(AccessTokenKey, token);
        }
        catch
        {
            // If storage write fails the in-memory value still works for this
            // circuit, but will not survive a circuit restart.
        }
    }

    public async Task ClearTokenAsync()
    {
        _cachedToken = null;
        _cachedRefreshToken = null;
        _storageReadForAccess = false;
        _storageReadForRefresh = false;
        try
        {
            await _sessionStorage.DeleteAsync(AccessTokenKey);
            await _sessionStorage.DeleteAsync(RefreshTokenKey);
        }
        catch { /* best-effort */ }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        if (!_storageReadForRefresh)
        {
            _storageReadForRefresh = true;
            try
            {
                var result = await _sessionStorage.GetAsync<string>(RefreshTokenKey);
                _cachedRefreshToken = result.Success ? result.Value : null;
            }
            catch
            {
                _cachedRefreshToken = null;
            }
        }
        return _cachedRefreshToken;
    }

    public async Task SetRefreshTokenAsync(string refreshToken)
    {
        _cachedRefreshToken = refreshToken;
        _storageReadForRefresh = true;
        try
        {
            await _sessionStorage.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch { /* best-effort */ }
    }
}
