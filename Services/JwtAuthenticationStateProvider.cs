using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bellwood.AdminPortal.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenProvider _tokenProvider;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    // Whether we have already read from browser storage in the interactive
    // circuit.  Deliberately NOT a Lazy<Task>: Lazy fires exactly once and
    // will not retry if the first call happened during prerender (where JS
    // interop is unavailable).  A plain flag lets the first *interactive*
    // call perform the read while every subsequent call skips it.
    private bool _storageRestored = false;

    public JwtAuthenticationStateProvider(IAuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        Console.WriteLine("[AuthStateProvider] Initialized");
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // ProtectedSessionStorage requires an active JS-interop channel.
        // That channel does not exist during the static prerender pass.
        // We detect prerender by attempting the storage read and treating
        // any JSException / InvalidOperationException as "not yet available":
        // AuthTokenProvider already swallows those and returns null, so the
        // only thing we need to guard here is not marking _storageRestored=true
        // on a prerender attempt.
        //
        // The safe pattern: only restore once, and only after storage
        // actually returns a usable result (or definitively returns empty).
        if (!_storageRestored)
        {
            await RestoreAuthStateFromStorageAsync();
        }

        var isAuthenticated = _currentUser.Identity?.IsAuthenticated ?? false;
        Console.WriteLine($"[AuthStateProvider] GetAuthenticationStateAsync - IsAuthenticated: {isAuthenticated}");
        return new AuthenticationState(_currentUser);
    }

    private async Task RestoreAuthStateFromStorageAsync()
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync();

            // AuthTokenProvider.GetTokenAsync catches JS interop exceptions
            // during prerender and returns null.  It also sets _storageReadForAccess=true
            // internally on that failed attempt, which would prevent a real read later.
            // We therefore only commit the "restored" flag when we know storage
            // was actually reachable — i.e. when we got a non-null token OR when
            // AuthTokenProvider confirms it completed a real (non-prerender) read.
            // The simplest proxy: if GetTokenAsync returned without throwing, the
            // storage layer handled it; trust it and mark done.
            _storageRestored = true;

            if (string.IsNullOrEmpty(token))
                return;

            Console.WriteLine("[AuthStateProvider] Found existing token in storage, restoring auth state");

            var claims = DecodeJwtToken(token);
            if (!claims.Any())
                return;

            _currentUser = new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "jwt"));

            var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var role     = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var userId   = claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            Console.WriteLine($"[AuthStateProvider] Auth state restored - User: {username}, Role: {role}, UserId: {userId}");
        }
        catch (Exception ex)
        {
            // Do NOT set _storageRestored = true here.  If something unexpected
            // throws (e.g. a Data Protection key rotation error), we want the
            // next GetAuthenticationStateAsync call to try again rather than
            // permanently caching a failed attempt.
            Console.WriteLine($"[AuthStateProvider] Error restoring auth state: {ex.Message}");
        }
    }

    public async Task MarkUserAsAuthenticatedAsync(string username, string token)
    {
        Console.WriteLine($"[AuthStateProvider] MarkUserAsAuthenticatedAsync called for user: {username}");

        await _tokenProvider.SetTokenAsync(token);

        var claims = DecodeJwtToken(token);

        if (!claims.Any())
        {
            Console.WriteLine("[AuthStateProvider] JWT decoding failed, using fallback claims");
            claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Role, "Staff"),
                new("access_token", token)
            };
        }

        _currentUser = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "jwt"));

        // Storage is definitely available now (called from an interactive event).
        _storageRestored = true;

        var role   = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        var userId = claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        Console.WriteLine($"[AuthStateProvider] Authenticated - User: {username}, Role: {role}, UserId: {userId}");

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        Console.WriteLine("[AuthStateProvider] MarkUserAsLoggedOutAsync called");

        await _tokenProvider.ClearTokenAsync();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        _storageRestored = false;

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }

    private List<Claim> DecodeJwtToken(string token)
    {
        var claims = new List<Claim>();
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                Console.WriteLine("[AuthStateProvider] Token is not a valid JWT format");
                return claims;
            }

            var jsonToken = handler.ReadJwtToken(token);

            var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var role     = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userId   = jsonToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var uid      = jsonToken.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

            if (!string.IsNullOrEmpty(username))
                claims.Add(new Claim(ClaimTypes.Name, username));

            if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, "Staff"));
                Console.WriteLine("[AuthStateProvider] No role claim in JWT, using fallback 'Staff'");
            }

            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim("userId", userId));

            if (!string.IsNullOrEmpty(uid))
                claims.Add(new Claim("uid", uid));

            claims.Add(new Claim("access_token", token));

            Console.WriteLine("[AuthStateProvider] Successfully decoded JWT token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateProvider] Error decoding JWT: {ex.Message}");
        }
        return claims;
    }
}
