using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bellwood.AdminPortal.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenProvider _tokenProvider;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    // Lazy initialization task: ensures storage is read exactly once per
    // circuit and that GetAuthenticationStateAsync always awaits the result
    // before returning.  Replaces the previous fire-and-forget async void.
    private readonly Lazy<Task> _initTask;

    public JwtAuthenticationStateProvider(IAuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        _initTask = new Lazy<Task>(RestoreAuthStateFromStorageAsync);
        Console.WriteLine("[AuthStateProvider] Initialized");
    }

    private async Task RestoreAuthStateFromStorageAsync()
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync();
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
            Console.WriteLine($"[AuthStateProvider] Error restoring auth state: {ex.Message}");
        }
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Await the once-per-circuit storage read before answering.
        await _initTask.Value;

        var isAuthenticated = _currentUser.Identity?.IsAuthenticated ?? false;
        Console.WriteLine($"[AuthStateProvider] GetAuthenticationStateAsync - IsAuthenticated: {isAuthenticated}");
        return new AuthenticationState(_currentUser);
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
