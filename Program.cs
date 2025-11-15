using Bellwood.AdminPortal.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // Enables Server-side Blazor

// HTTP Client for calling AdminAPI
builder.Services.AddHttpClient("AdminAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5007/"); // Your AdminAPI URL
    client.Timeout = TimeSpan.FromSeconds(30);
});

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