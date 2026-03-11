using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Cookie-based authentication state provider for Blazor Server.
///
/// How it works:
/// - During SSR (prerender), ASP.NET Core's cookie auth middleware has already
///   parsed the auth cookie and populated HttpContext.User with the claims.
/// - The base class (RevalidatingServerAuthenticationStateProvider →
///   ServerAuthenticationStateProvider) captures HttpContext.User during SSR
///   and caches it for the interactive circuit.
/// - During the interactive circuit (SignalR), HttpContext is null, but the
///   cached auth state is returned. Periodic revalidation checks that the
///   session is still valid.
///
/// This replaces JwtAuthenticationStateProvider, which relied on
/// ProtectedSessionStorage (browser sessionStorage) and suffered from
/// SSR/circuit timing mismatches.
/// </summary>
public class CookieAuthStateProvider : RevalidatingServerAuthenticationStateProvider
{
    public CookieAuthStateProvider(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }

    /// <summary>
    /// How often to revalidate the authentication state.
    /// The cookie middleware handles actual expiration; this is a secondary check.
    /// </summary>
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    /// <summary>
    /// Called periodically to check whether the authentication state is still valid.
    /// Returns true if the user should remain authenticated.
    /// </summary>
    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        // The cookie middleware handles expiration and sliding renewal.
        // Here we just confirm the identity is still marked as authenticated.
        return Task.FromResult(
            authenticationState.User?.Identity?.IsAuthenticated == true);
    }
}
