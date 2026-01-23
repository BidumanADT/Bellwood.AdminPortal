using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Authentication handler that bridges Blazor Server authentication with ASP.NET Core's [Authorize] attribute
/// </summary>
public class BlazorAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public BlazorAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthenticationStateProvider authenticationStateProvider)
        : base(options, logger, encoder)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Get the authentication state from Blazor's AuthenticationStateProvider
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // If user is authenticated, return success
            if (user?.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            // User is not authenticated
            return AuthenticateResult.NoResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating user via Blazor authentication handler");
            return AuthenticateResult.Fail(ex);
        }
    }
}
