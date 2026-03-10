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
/// exist during the static prerender pass.  GetAsync throws (or returns a
/// failure result) during that phase.  We detect this by inspecting whether
/// the read actually succeeded: if the result is not Success we leave the
/// "read" flag unset so the first interactive-circuit call retries.
/// </summary>
public class AuthTokenProvider : IAuthTokenProvider
{
    private const string AccessTokenKey  = "bw_access_token";
    private const string RefreshTokenKey = "bw_refresh_token";

    private readonly ProtectedSessionStorage _sessionStorage;

    private string? _cachedToken;
    private string? _cachedRefreshToken;

    // These flags are only set to true after a *successful* storage read
    // (i.e. after the interactive circuit is live).  A prerender attempt
    // that throws / returns !Success leaves the flag false so the interactive
    // circuit can retry.
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
            try
            {
                var result = await _sessionStorage.GetAsync<string>(AccessTokenKey);
                if (result.Success)
                {
                    // Storage is reachable and returned a definitive answer
                    // (even if the answer is "no token stored").
                    _storageReadForAccess = true;
                    _cachedToken = result.Value;
                }
                // If !result.Success we leave _storageReadForAccess = false so
                // the next call (on the real interactive circuit) retries.
            }
            catch
            {
                // JS interop unavailable (prerender).  Leave flag unset so the
                // interactive circuit reads storage properly.
            }
        }
        return _cachedToken;
    }

    public async Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        _storageReadForAccess = true;   // Written by an interactive event — lock in.
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
            try
            {
                var result = await _sessionStorage.GetAsync<string>(RefreshTokenKey);
                if (result.Success)
                {
                    _storageReadForRefresh = true;
                    _cachedRefreshToken = result.Value;
                }
            }
            catch { /* prerender — leave flag unset */ }
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
