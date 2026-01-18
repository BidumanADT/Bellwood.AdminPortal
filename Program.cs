using Bellwood.AdminPortal.Components;
using Bellwood.AdminPortal.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Razor components (Blazor Web App - Server interactivity)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor-style auth
builder.Services.AddAuthorizationCore();

// Token store + auth state provider - MUST BE SINGLETON to persist across circuits
builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddSingleton<IAdminApiKeyProvider, AdminApiKeyProvider>();

// Business services
builder.Services.AddScoped<IAffiliateService, AffiliateService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

// Driver tracking service - scoped to allow per-circuit SignalR connections
builder.Services.AddScoped<IDriverTrackingService, DriverTrackingService>();

// Phase 2.2: Token refresh service - scoped per user session
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();

// Phase 2.4: User management service
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// Register the concrete provider as singleton so it persists
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();

// Expose it as the AuthenticationStateProvider used by Router/AuthorizeView
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// Give components access to the auth state cascade
builder.Services.AddCascadingAuthenticationState();

// Auth Server HTTP Client
builder.Services.AddHttpClient("AuthServer", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
#if DEBUG
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});
#else
;
#endif

// AdminAPI HTTP Client (with token attachment)
builder.Services.AddHttpClient("AdminAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5206/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
#if DEBUG
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});
#else
;
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();