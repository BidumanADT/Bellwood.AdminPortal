using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Stores the JWT access token and refresh token in ProtectedSessionStorage.
///
/// Security properties:
/// - Encrypted server-side by ASP.NET Core Data Protection.  The raw token
///   is never visible in browser DevTools.
/// - Scoped to the browser session (sessionStorage): closing the tab clears
///   the data automatically.
/// - Completely isolated per browser / device / incognito window.
/// - Registered AddScoped in Program.cs, so each Blazor circuit gets its own
///   instance with no sharing between users.
///
/// Prerender safety:
/// ProtectedSessionStorage requires an active JS-interop channel that does not
/// exist during the static SSR prerender pass.  Calling it during prerender
/// can deadlock (the await hangs waiting for a circuit that doesn't exist).
/// We detect prerender by checking IHttpContextAccessor: during static SSR
/// there is an active HttpContext; during an interactive circuit there is not.
/// When HttpContext is present we skip all storage calls entirely.
/// </summary>
public class AuthTokenProvider : IAuthTokenProvider
{
    private const string AccessTokenKey  = "bw_access_token";
    private const string RefreshTokenKey = "bw_refresh_token";

    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private string? _cachedToken;
    private string? _cachedRefreshToken;

    // These flags are only set to true after a *successful* storage read
    // (i.e. after the interactive circuit is live).  A prerender attempt
    // is skipped entirely so these flags never get poisoned.
    private bool _storageReadForAccess;
    private bool _storageReadForRefresh;

    public AuthTokenProvider(
        ProtectedSessionStorage sessionStorage,
        IHttpContextAccessor httpContextAccessor)
    {
        _sessionStorage = sessionStorage;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Returns true when we are inside a static SSR render (prerender).
    /// During SSR there is always an active HttpContext.  During an
    /// interactive Blazor Server circuit, HttpContext is null.
    /// ProtectedSessionStorage must NEVER be called during SSR — it will
    /// deadlock because there is no JS-interop channel.
    /// </summary>
    private bool IsPrerendering => _httpContextAccessor.HttpContext is not null;

    public async Task<string?> GetTokenAsync()
    {
        if (!_storageReadForAccess && !IsPrerendering)
        {
            try
            {
                var result = await _sessionStorage.GetAsync<string>(AccessTokenKey);
                // Regardless of whether a token was stored, storage was reachable.
                _storageReadForAccess = true;
                _cachedToken = result.Success ? result.Value : null;
            }
            catch
            {
                // Unexpected failure — leave flag unset so next call retries.
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
        if (!_storageReadForRefresh && !IsPrerendering)
        {
            try
            {
                var result = await _sessionStorage.GetAsync<string>(RefreshTokenKey);
                _storageReadForRefresh = true;
                _cachedRefreshToken = result.Success ? result.Value : null;
            }
            catch { /* leave flag unset */ }
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
