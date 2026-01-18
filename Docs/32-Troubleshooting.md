# Troubleshooting & Common Issues

**Document Type**: Living Document - Operational Guide  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This guide provides solutions to common issues encountered in the Bellwood AdminPortal, organized by category with step-by-step troubleshooting procedures.

**Categories**:
- ?? Authentication & Login Issues
- ?? API Connection Problems
- ??? Real-Time Tracking Issues
- ?? Data & State Management
- ??? Build & Deployment Errors
- ?? Configuration Problems

**Target Audience**: Developers, DevOps, support staff  
**Prerequisites**: Access to logs, browser DevTools, terminal

---

## ?? Critical Issues (Start Here)

### Issue: Blank Page After Login

**Symptom**: Login succeeds but page stays blank or shows loading dots forever

**Root Cause**: Service lifetime issue - authentication state lost when Blazor creates new circuit

**Evidence in Console**:
```
[Login] Auth state updated, navigating to /bookings
[AuthStateProvider] Initialized  ? NEW INSTANCE CREATED!
[AuthStateProvider] GetAuthenticationStateAsync called - IsAuthenticated: False
[Bookings] AuthorizeView: NotAuthorized - redirecting to login
```

**Solution**: Change auth services from Scoped to Singleton

**File**: `Program.cs`

**Before (WRONG)**:
```csharp
builder.Services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
```

**After (CORRECT)**:
```csharp
builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
```

**Why This Works**:
- **Scoped** = one instance per circuit (new circuit = lost state)
- **Singleton** = one instance for entire app (state persists across circuits)
- Blazor Server can create new circuits during navigation
- Singleton ensures same auth instance used everywhere

**Verification**:
```
[AuthStateProvider] Initialized  ? ONCE only at startup
[Login] Auth state updated, navigating to /bookings
[AuthStateProvider] GetAuthenticationStateAsync called - IsAuthenticated: True ?
[Bookings] AuthorizeView: Authorized section rendering ?
```

**Status**: ? Fixed in v1.5

---

### Issue: Application Won't Start

**Symptom**: Error on `dotnet run` or "Failed to build" message

**Common Causes**:

#### 1. Invalid JSON Configuration

**Error Message**:
```
Unhandled exception. System.Text.Json.JsonException: 
',' is invalid after a value. Expected '}'. LineNumber: 7
```

**File**: `appsettings.Development.json`

**Problem**:
```json
{
  "Logging": { ... }
}
{  // ? Extra opening brace!
  "AdminAPI": { ... }
}
```

**Solution**:
```json
{
  "Logging": { ... },
  "AdminAPI": { ... }  // ? Single object
}
```

**Validation**: Use [JSONLint](https://jsonlint.com/) to validate JSON

---

#### 2. Missing Namespace

**Error Message**:
```
CS0116: A namespace does not directly contain members such as fields or methods
```

**File**: `Services/IAdminApiKeyProvider.cs` (or similar)

**Problem**:
```csharp
{  // File starts with brace!
    public interface IAdminApiKeyProvider { ... }
}
```

**Solution**:
```csharp
namespace Bellwood.AdminPortal.Services;

public interface IAdminApiKeyProvider { ... }
```

---

#### 3. Missing Dependencies

**Error Message**:
```
The command could not be loaded, possibly because:
  * You intended to execute a .NET application
```

**Solution**:
```bash
dotnet restore
dotnet build
```

---

## ?? Authentication Issues

### Issue: "Invalid username or password"

**Symptom**: Login fails with error message

**Checklist**:
- [ ] Is AuthServer running on `https://localhost:5001`?
- [ ] Are credentials correct? (`alice` / `password`)
- [ ] Check browser console for errors
- [ ] Check AuthServer logs for authentication attempts

**Test AuthServer**:
```powershell
# Manual test
$body = @{
    username = "alice"
    password = "password"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

**Expected Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "..."
}
```

---

### Issue: Stuck on Login Page After Successful Login

**Symptom**: Login succeeds (no error) but stays on login page

**Cause**: Navigation issue or auth state not updated

**Debug Steps**:

1. **Check Browser Console**:
```
[Login] Login successful ?
[Login] Token received, length: 184 ?
[AuthStateProvider] MarkUserAsAuthenticatedAsync called ?
[Login] Auth state updated, navigating to /main ? Should see this
```

2. **Verify Navigation**:
```csharp
// In Login.razor
Navigation.NavigateTo("/main", forceLoad: false); // Not true!
```

3. **Check Auth State**:
```csharp
// Should call:
await AuthStateProvider.MarkUserAsAuthenticatedAsync(username, token);
```

**Solution**: Ensure `forceLoad: false` and auth state updated before navigation

---

### Issue: Logout Doesn't Work

**Symptom**: Click Logout but stays logged in

**Checklist**:
- [ ] Does `Logout.razor` exist?
- [ ] Does it call `MarkUserAsLoggedOutAsync()`?
- [ ] Does it navigate to `/login`?

**Expected Code**:
```csharp
// Logout.razor
protected override async Task OnInitializedAsync()
{
    await AuthStateProvider.MarkUserAsLoggedOutAsync();
    Navigation.NavigateTo("/login", forceLoad: true);
}
```

---

## ?? API Connection Issues

### Issue: "Failed to load bookings"

**Symptom**: Error message on Bookings page

**Debug Steps**:

1. **Check AdminAPI Status**:
```bash
# Should be running
curl https://localhost:5206/health
```

2. **Check Browser Network Tab**:
   - Filter by "Fetch/XHR"
   - Look for request to `/bookings/list`
   - Check status code (200, 401, 404, 500)

3. **Check Request Headers**:
```
X-Admin-ApiKey: dev-secret-123  ? Must match appsettings
Authorization: Bearer eyJ...     ? Should be present
```

4. **Check AdminAPI Logs**:
```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 GET https://localhost:5206/bookings/list?take=100
```

**Common Causes**:

#### 1. AdminAPI Not Running

**Solution**:
```bash
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```

**Verify**: Should see `Now listening on: https://localhost:5206`

---

#### 2. API Key Mismatch

**Error**: `401 Unauthorized`

**Check**:
```json
// AdminPortal: appsettings.Development.json
{
  "AdminAPI": {
    "ApiKey": "dev-secret-123"  ? Must match AdminAPI
  }
}

// AdminAPI: appsettings.Development.json
{
  "AdminApiKey": "dev-secret-123"  ? Same value!
}
```

**Fix**: Ensure keys match, restart both apps

---

#### 3. CORS Issues

**Error**: `CORS policy: No 'Access-Control-Allow-Origin' header`

**Solution** (AdminAPI `Program.cs`):
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminPortal", policy =>
    {
        policy.WithOrigins("https://localhost:7257")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors("AllowAdminPortal");
```

---

### Issue: SSL/TLS Certificate Errors

**Symptom**: "Unable to connect" or "Certificate is not trusted"

**Solution**:
```bash
# Trust dev certificates
dotnet dev-certs https --trust
```

**For Development Only** (HttpClientHandler):
```csharp
#if DEBUG
    handler.ServerCertificateCustomValidationCallback = 
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
```

**?? Never use in production!**

---

## ??? Real-Time Tracking Issues

### Issue: SignalR Connection Failed

**Symptom**: Live Tracking shows "Disconnected" (red badge)

**Debug Steps**:

1. **Check Browser Console**:
```
[DriverTrackingService] Connecting to SignalR hub...
Error: Failed to start the connection: Error: WebSocket failed
```

2. **Check AdminAPI SignalR Hub**:
```bash
# Should see LocationHub registered
info: Microsoft.AspNetCore.SignalR[1]
      Hub 'LocationHub' registered
```

3. **Check WebSocket Support**:
   - Open DevTools ? Network ? WS filter
   - Should see WebSocket connection to `/hubs/location`

**Common Causes**:

#### 1. WebSocket Blocked

**Firewall/Proxy**: WebSocket connections blocked

**Solution**: Enable WebSocket in firewall or use polling fallback

**Polling Fallback** (automatic):
```
[DriverTrackingService] SignalR connection failed
[DriverTrackingService] Falling back to polling mode
[DriverTrackingService] Polling interval: 15 seconds
```

---

#### 2. JWT Token Not Included

**Check**: SignalR connection should include token

```csharp
// DriverTrackingService.cs
_hubConnection = new HubConnectionBuilder()
    .WithUrl($"{_adminApiBaseUrl}/hubs/location?access_token={token}")
    //                                           ^^^^^^^^^^^^^^^^^^^^^^^^
    .Build();
```

**Verify**: Check browser DevTools ? Network ? WS ? Headers ? Query String Parameters ? `access_token`

---

#### 3. SignalR Hub Not Registered

**Check AdminAPI** (`Program.cs`):
```csharp
app.MapHub<LocationHub>("/hubs/location");
```

---

### Issue: Map Doesn't Display

**Symptom**: Blank map or error message

**Checklist**:
- [ ] Is Google Maps API key configured?
- [ ] Is `tracking-map.js` loaded?
- [ ] Check browser console for errors

**Google Maps API Key**:
```json
// appsettings.Development.json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

**Get API Key**: [Google Cloud Console](https://console.cloud.google.com/) ? APIs & Services ? Credentials

**Fallback**: Map shows placeholder if no API key

---

### Issue: Location Updates Not Appearing

**Symptom**: SignalR connected but markers don't update

**Debug**:

1. **Check DriverApp Sending Updates**:
```
// DriverApp should POST to:
POST /driver/location/update
{
  "rideId": "abc123",
  "latitude": 41.8781,
  "longitude": -87.6298,
  "speed": 55.0
}
```

2. **Check AdminAPI Broadcasts Event**:
```
// AdminAPI logs should show:
[LocationHub] Broadcasting LocationUpdate for ride abc123
```

3. **Check AdminPortal Receives Event**:
```
// AdminPortal console should show:
[DriverTrackingService] LocationUpdate received
  RideId: abc123
  Driver: Charlie
```

**Solution**: Verify entire chain from DriverApp ? AdminAPI ? AdminPortal

---

## ?? Data & State Management

### Issue: Changes Don't Persist After Refresh

**Symptom**: Assign driver, refresh page, assignment lost

**Cause**: AdminAPI not saving to persistent storage

**Check**:
```bash
# AdminAPI should have:
C:\Users\sgtad\source\repos\Bellwood.AdminApi\App_Data\bookings.json
```

**Verify File Updated**:
1. Assign driver
2. Check `bookings.json`
3. Look for `assignedDriverId` and `assignedDriverName`

**Example**:
```json
{
  "id": "abc123",
  "assignedDriverId": "driver-001",
  "assignedDriverName": "Michael Johnson",
  "status": "Scheduled"
}
```

**If Not Persisting**: Check AdminAPI repository implementation

---

### Issue: Duplicate Data After Seeding

**Symptom**: Running seed script multiple times creates duplicates

**Solution**: Clear data first

**Script**: `Scripts/clear-test-data.ps1`

```powershell
# Wipe all data
.\Scripts\clear-test-data.ps1
# Type "YES" to confirm

# Seed fresh data
.\Scripts\seed-admin-api.ps1
```

---

### Issue: Affiliate Creation Fails with JSON Error

**Symptom**: "Failed to create affiliate: JSON serialization error"

**Cause**: Drivers list not initialized

**Bug Fixed in v1.5**

**Verify Fix** (`Models/AffiliateModels.cs`):
```csharp
public class AffiliateDto
{
    // ...
    public List<DriverDto> Drivers { get; set; } = new(); // ? Initialized
}
```

**Before Fix**:
```csharp
public List<DriverDto> Drivers { get; set; } // ? Null reference
```

---

## ??? Build & Deployment Errors

### Issue: Build Fails with Razor Warnings

**Warning**:
```
Warning RZ10012: Found markup element with unexpected name 'AuthorizeView'
```

**Cause**: Missing `@using` directive

**Solution** (`_Imports.razor`):
```razor
@using Microsoft.AspNetCore.Components.Authorization
@using Bellwood.AdminPortal
@using Bellwood.AdminPortal.Components
@using Bellwood.AdminPortal.Services
@using Bellwood.AdminPortal.Models
```

---

### Issue: "The type or namespace name does not exist"

**Error**:
```
CS0246: The type or namespace name 'QuoteService' could not be found
```

**Checklist**:
- [ ] Is namespace correct?
- [ ] Is class public?
- [ ] Is `@using` directive added?
- [ ] Did you run `dotnet restore`?

**Solution**:
```bash
dotnet clean
dotnet restore
dotnet build
```

---

### Issue: Publish Fails

**Error**: `Could not find a part of the path`

**Cause**: Missing `wwwroot` files or incorrect publish profile

**Solution**:
```bash
# Ensure all files exist
dotnet publish --configuration Release -o ./publish

# Check output folder
ls ./publish/wwwroot
```

---

## ?? Configuration Issues

### Issue: Settings Not Loading

**Symptom**: API key is empty or null

**Check**:
```csharp
// AdminApiKeyProvider.cs
var apiKey = _configuration["AdminAPI:ApiKey"];
Console.WriteLine($"API Key loaded: {apiKey}");
```

**Verify**:
```json
// appsettings.Development.json (correct)
{
  "AdminAPI": {
    "ApiKey": "dev-secret-123"
  }
}

// NOT this:
{
  "AdminApi": {  // ? Case-sensitive!
    "ApiKey": "dev-secret-123"
  }
}
```

**Environment-Specific**:
- Development: `appsettings.Development.json`
- Production: `appsettings.Production.json`
- Override with environment variables

---

### Issue: Wrong Environment

**Symptom**: Using production settings in development

**Check**:
```bash
echo $env:ASPNETCORE_ENVIRONMENT
# Should be: Development
```

**Set Environment**:
```bash
# PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Development"

# CMD
set ASPNETCORE_ENVIRONMENT=Development

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Development
```

---

## ?? Performance Issues

### Issue: Slow Page Load

**Symptom**: Pages take > 5 seconds to load

**Debug**:

1. **Browser DevTools ? Performance**
   - Record page load
   - Look for long tasks

2. **Network Tab**:
   - Check large payloads
   - Look for slow API calls

**Common Causes**:

#### 1. Large Data Sets

**Problem**: Loading 1000+ bookings at once

**Solution**: Implement pagination
```csharp
// GET /bookings/list?skip=0&take=50
```

---

#### 2. No Caching

**Problem**: Re-fetching same data repeatedly

**Solution**: Add response caching
```csharp
[ResponseCache(Duration = 60)]
public IActionResult GetBookings() { ... }
```

---

#### 3. Excessive SignalR Events

**Problem**: Too many real-time updates

**Solution**: Throttle events
```csharp
// Send updates max every 5 seconds
```

---

## ?? Testing Issues

### Issue: Seed Data Script Fails

**Error**: `401 Unauthorized`

**Checklist**:
- [ ] Is AuthServer running?
- [ ] Are test accounts created (alice, bob)?
- [ ] Is AdminAPI running?

**Manual Test**:
```powershell
# Test authentication
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"alice","password":"password"}'

$token = $response.accessToken
Write-Host "Token: $token"
```

---

### Issue: Tests Pass Locally But Fail in CI/CD

**Common Causes**:
- Different .NET version
- Missing environment variables
- Certificate trust issues

**Solution**:
```yaml
# GitHub Actions example
- name: Trust dev certificates
  run: dotnet dev-certs https --trust
  
- name: Set environment
  run: echo "ASPNETCORE_ENVIRONMENT=Development" >> $GITHUB_ENV
```

---

## ?? Emergency Procedures

### Complete System Reset

**When**: Everything is broken, start fresh

**Steps**:
```bash
# 1. Stop all services (Ctrl+C in each terminal)

# 2. Clear all data
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
.\Scripts\clear-test-data.ps1

# 3. Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# 4. Start services
# Terminal 1: AuthServer
cd ..\BellwoodAuthServer
dotnet run

# Terminal 2: AdminAPI
cd ..\Bellwood.AdminApi
dotnet run

# Terminal 3: AdminPortal
cd ..\Bellwood.AdminPortal
dotnet run

# 5. Seed data
.\Scripts\seed-admin-api.ps1

# 6. Test login
# Navigate to https://localhost:7257
# Login: alice / password
```

---

### Rollback to Last Working Version

**Git Workflow**:
```bash
# View recent commits
git log --oneline -10

# Rollback to specific commit
git checkout <commit-hash>

# Or rollback last commit
git reset --hard HEAD~1

# Rebuild
dotnet clean
dotnet build
```

---

## ?? Getting Help

### Before Asking for Help

**Gather Information**:
1. Browser console logs (F12)
2. AdminAPI terminal logs
3. AuthServer terminal logs
4. Network tab (DevTools)
5. Steps to reproduce
6. Expected vs actual behavior

### Log Collection

**Enable Detailed Logging** (`appsettings.Development.json`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

**Export Logs**:
```bash
dotnet run > logs.txt 2>&1
```

---

## ?? Related Documentation

- [Testing Guide](02-Testing-Guide.md) - Comprehensive test procedures
- [Deployment Guide](30-Deployment-Guide.md) - Production deployment
- [System Architecture](01-System-Architecture.md) - How components interact
- [Security Model](23-Security-Model.md) - Authentication flow

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This troubleshooting guide captures solutions to all major issues encountered during development. When you encounter a new issue, document the solution here for future reference.* ???
