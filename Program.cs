using Bellwood.AdminPortal.Components;
using Bellwood.AdminPortal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(10); // Staff session duration
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState(); // Enable auth state in Blazor

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

// Custom service to attach tokens to API calls
builder.Services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();