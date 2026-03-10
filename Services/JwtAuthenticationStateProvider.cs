using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bellwood.AdminPortal.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    // Whether we have successfully read from browser storage in the
    // interactive circuit.  Never set to true during prerender.
    private bool _storageRestored;

    public JwtAuthenticationStateProvider(
        IAuthTokenProvider tokenProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _tokenProvider = tokenProvider;
        _httpContextAccessor = httpContextAccessor;
        Console.WriteLine("[AuthStateProvider] Initialized");
    }

    /// <summary>
    /// True during static SSR (prerender).  ProtectedSessionStorage must
    /// not be called in this context — it would deadlock.
    /// </summary>
    private bool IsPrerendering => _httpContextAccessor.HttpContext is not null;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // During prerender: return anonymous immediately.  Do not touch
        // browser storage — there is no JS-interop channel and the call
        // would deadlock.  The interactive circuit will call us again
        // with a fresh scoped instance where IsPrerendering == false.
        if (!_storageRestored && !IsPrerendering)
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
            // Do NOT set _storageRestored = true.  Retry on next call.
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
