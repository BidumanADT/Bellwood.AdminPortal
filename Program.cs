using Bellwood.AdminPortal.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // Enables Server-side Blazor

// HTTP Client for calling AdminAPI
builder.Services.AddHttpClient("AdminAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5206/");
    client.Timeout = TimeSpan.FromSeconds(30);

#if DEBUG
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
   {
       ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
   });
#else
});
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

// Map Razor Components with Server interactivity
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();