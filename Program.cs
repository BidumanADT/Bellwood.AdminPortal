using Bellwood.AdminPortal.Components;
using Bellwood.AdminPortal.Observability;
using Bellwood.AdminPortal.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

// Ensure scoped CSS bundle (Bellwood.AdminPortal.styles.css) is served
// in non-Development environments. In Development the SDK enables this
// automatically; in Alpha/Production it must be called explicitly.
builder.WebHost.UseStaticWebAssets();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "AdminPortal")
    .Enrich.WithProperty("environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Razor components (Blazor Web App - Server interactivity)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Phase 2 Fix: Add authentication services for [Authorize] attribute support
builder.Services.AddAuthentication()
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        BlazorAuthenticationHandler>("Blazor", options => { });

// Blazor-style auth with policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("admin", "dispatcher"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
builder.Services.AddTransient<CorrelationIdPropagationHandler>();

// Token store + auth state provider - MUST BE SINGLETON to persist across circuits
builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddSingleton<IAdminApiKeyProvider, AdminApiKeyProvider>();

// Business services
builder.Services.AddScoped<IAffiliateService, AffiliateService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IProfileApiService, ProfileApiService>();

// Driver tracking service - scoped to allow per-circuit SignalR connections
builder.Services.AddScoped<IDriverTrackingService, DriverTrackingService>();

// Phase 2.2: Token refresh service - scoped per user session
builder.Services.AddScoped<ITokenRefreshService, TokenRefreshService>();

// Phase 2.4: User management service
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// Phase 3.1: Audit log service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Register the concrete provider as singleton so it persists
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();

// Expose it as the AuthenticationStateProvider used by Router/AuthorizeView
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// Give components access to the auth state cascade
builder.Services.AddCascadingAuthenticationState();

// Resolve URLs from configuration (supports per-environment appsettings overrides)
var adminApiBaseUrl = builder.Configuration["AdminAPI:BaseUrl"]
    ?? throw new InvalidOperationException("AdminAPI:BaseUrl is not configured.");

var authServerBaseUrl = builder.Configuration["AuthServer:BaseUrl"]
    ?? throw new InvalidOperationException("AuthServer:BaseUrl is not configured.");

/// Auth Server HTTP Client
builder.Services.AddHttpClient("AuthServer", client =>
{
    client.BaseAddress = new Uri(authServerBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<CorrelationIdPropagationHandler>()
.AddHttpMessageHandler(sp => new OutboundHttpLoggingHandler(
    sp.GetRequiredService<ILogger<OutboundHttpLoggingHandler>>(),
    sp.GetRequiredService<ICorrelationContextAccessor>(),
    "AuthServer"))
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = builder.Environment.IsDevelopment()
        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        : null
});

// AdminAPI HTTP Client (with token attachment)
builder.Services.AddHttpClient("AdminAPI", client =>
{
    client.BaseAddress = new Uri(adminApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<CorrelationIdPropagationHandler>()
.AddHttpMessageHandler(sp => new OutboundHttpLoggingHandler(
    sp.GetRequiredService<ILogger<OutboundHttpLoggingHandler>>(),
    sp.GetRequiredService<ICorrelationContextAccessor>(),
    "AdminAPI"))
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = builder.Environment.IsDevelopment()
        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        : null
});

var app = builder.Build();

// Startup diagnostics - print resolved config so environment issues are obvious
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== AdminPortal Starting ===");
logger.LogInformation("Environment   : {Env}", app.Environment.EnvironmentName);
logger.LogInformation("AdminAPI URL  : {Url}", adminApiBaseUrl);
logger.LogInformation("AuthServer URL: {Url}", authServerBaseUrl);
logger.LogInformation("============================");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseMiddleware<CorrelationLoggingMiddleware>();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Phase 2 Fix: Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var userId = context.User?.FindFirst("sub")?.Value
        ?? context.User?.FindFirst("uid")?.Value
        ?? context.User?.Identity?.Name
        ?? "anonymous";

    using (LogContext.PushProperty("userId", userId))
    {
        await next();
    }
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
