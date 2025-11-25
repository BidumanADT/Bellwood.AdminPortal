using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

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
            // Restore auth state from existing token
            // In a real app, you'd decode the JWT to get the username
            // For now, just mark as authenticated with a generic identity
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "User"), // You could decode from JWT
                new(ClaimTypes.Role, "Staff"),
                new("access_token", token)
            };

            _currentUser = new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "jwt"));
            
            Console.WriteLine("[AuthStateProvider] Auth state restored from existing token");
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Staff"),
            new("access_token", token)
        };

        _currentUser = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "jwt"));

        Console.WriteLine($"[AuthStateProvider] User authenticated - IsAuthenticated: {_currentUser.Identity?.IsAuthenticated}");
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
}
