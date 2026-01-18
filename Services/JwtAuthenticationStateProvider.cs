using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Bellwood.AdminPortal.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenProvider _tokenProvider;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public JwtAuthenticationStateProvider(IAuthTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
        Console.WriteLine("[AuthStateProvider] Initialized");
        
        // Try to restore auth state from token provider on init
        InitializeAuthStateAsync();
    }

    private async void InitializeAuthStateAsync()
    {
        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            Console.WriteLine("[AuthStateProvider] Found existing token on initialization, restoring auth state");
            
            // Decode JWT to get actual claims
            var claims = DecodeJwtToken(token);
            if (claims.Any())
            {
                _currentUser = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, authenticationType: "jwt"));
                
                Console.WriteLine("[AuthStateProvider] Auth state restored from existing token");
                
                // Log the decoded claims for debugging
                var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var userId = claims.FirstOrDefault(c => c.Type == "userId")?.Value;
                Console.WriteLine($"[AuthStateProvider] User: {username}, Role: {role}, UserId: {userId}");
            }
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var isAuthenticated = _currentUser.Identity?.IsAuthenticated ?? false;
        Console.WriteLine($"[AuthStateProvider] GetAuthenticationStateAsync called - IsAuthenticated: {isAuthenticated}");
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public async Task MarkUserAsAuthenticatedAsync(string username, string token)
    {
        Console.WriteLine($"[AuthStateProvider] MarkUserAsAuthenticatedAsync called for user: {username}");
        
        await _tokenProvider.SetTokenAsync(token);

        // Decode JWT to extract actual claims
        var claims = DecodeJwtToken(token);
        
        if (!claims.Any())
        {
            // Fallback if JWT decoding fails
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

        Console.WriteLine($"[AuthStateProvider] User authenticated - IsAuthenticated: {_currentUser.Identity?.IsAuthenticated}");
        
        // Log the decoded claims for debugging
        var role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        var userId = claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        Console.WriteLine($"[AuthStateProvider] Decoded - User: {username}, Role: {role}, UserId: {userId}");
        
        Console.WriteLine($"[AuthStateProvider] Notifying authentication state changed");

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

    /// <summary>
    /// Decode JWT token and extract claims for ClaimsPrincipal
    /// </summary>
    private List<Claim> DecodeJwtToken(string token)
    {
        var claims = new List<Claim>();
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Check if token is valid JWT format
            if (!handler.CanReadToken(token))
            {
                Console.WriteLine("[AuthStateProvider] Token is not a valid JWT format");
                return claims;
            }
            
            var jsonToken = handler.ReadJwtToken(token);
            
            // Extract standard claims
            var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var role = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userId = jsonToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var uid = jsonToken.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            
            // Add standard claims
            if (!string.IsNullOrEmpty(username))
            {
                claims.Add(new Claim(ClaimTypes.Name, username));
            }
            
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            else
            {
                // Fallback role if not present in JWT
                claims.Add(new Claim(ClaimTypes.Role, "Staff"));
                Console.WriteLine("[AuthStateProvider] No role claim in JWT, using fallback 'Staff'");
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim("userId", userId));
            }
            
            if (!string.IsNullOrEmpty(uid))
            {
                claims.Add(new Claim("uid", uid));
            }
            
            // Store the token itself for API calls
            claims.Add(new Claim("access_token", token));
            
            Console.WriteLine($"[AuthStateProvider] Successfully decoded JWT token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateProvider] Error decoding JWT: {ex.Message}");
        }
        
        return claims;
    }
}
