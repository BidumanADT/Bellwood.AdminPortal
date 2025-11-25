# Bellwood Admin Portal - Setup & Usage Guide

## ?? Quick Start

### Prerequisites
- .NET 8 SDK
- Running AuthServer on `https://localhost:5001`
- Running AdminAPI on `https://localhost:5206`

### 1. Seed Test Data

First, seed the AdminAPI with test bookings:

```powershell
# PowerShell
.\seed-admin-api.ps1

# Or manually with curl
curl -X POST https://localhost:5206/bookings/seed -k
```

### 2. Run the Portal

```bash
dotnet run
```

### 3. Login

Navigate to `https://localhost:7257` and login with:
- Username: `alice` / Password: `password`
- Username: `bob` / Password: `password`

## ?? Architecture Overview

### Authentication Flow

```
User ? Login.razor ? AuthServer (/api/auth/login)
       ?
   JWT Token Received
       ?
   JwtAuthenticationStateProvider.MarkUserAsAuthenticatedAsync()
       ?
   Token stored in AuthTokenProvider
       ?
   Blazor AuthenticationState updated
       ?
   Redirect to /bookings
```

### Authorization Flow

```
Components/Pages/Bookings.razor
    ?
<AuthorizeView>
    ?
Checks AuthenticationStateProvider
    ?
If Authorized: Show bookings
If Not: Redirect to /login
```

### API Communication Flow

```
Bookings.razor ? LoadBookingsAsync()
    ?
HttpClient "AdminAPI" configured with:
    - Base URL: https://localhost:5206
    - X-Admin-ApiKey header (from appsettings)
    - Bearer token (from AuthTokenProvider)
    ?
GET /bookings/list?take=100
    ?
AdminAPI validates API key
    ?
Returns JSON array of bookings
    ?
Displayed in UI with filters
```

## ?? Configuration

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AdminApi": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": "dev-secret-123"
  }
}
```

**Important**: The `ApiKey` must match the `Email:ApiKey` setting in your AdminAPI's `appsettings.Development.json`.

## ?? Project Structure

```
Bellwood.AdminPortal/
??? Components/
?   ??? App.razor                    # Root component with Router
?   ??? Layout/
?   ?   ??? MainLayout.razor         # Authenticated pages layout
?   ?   ??? EmptyLayout.razor        # Login/public pages layout
?   ?   ??? NavMenu.razor            # Navigation sidebar
?   ??? Pages/
?       ??? Home.razor               # Root route (redirects based on auth)
?       ??? Login.razor              # Login page (InteractiveServer)
?       ??? Logout.razor             # Logout handler
?       ??? Bookings.razor           # Main bookings dashboard
??? Services/
?   ??? IAuthTokenProvider.cs        # Interface for token storage
?   ??? AuthTokenProvider.cs         # In-memory token storage
?   ??? IAdminApiKeyProvider.cs      # Interface for API key access
?   ??? AdminApiKeyProvider.cs       # Reads API key from config
?   ??? JwtAuthenticationStateProvider.cs  # Blazor auth state management
??? Program.cs                       # DI configuration & middleware
??? appsettings.Development.json     # Development configuration
```

## ?? Key Components Explained

### 1. JwtAuthenticationStateProvider

This is the bridge between your login flow and Blazor's built-in authorization components (`<AuthorizeView>`, `<AuthorizeRouteView>`).

**What it does:**
- Stores the current user's authentication state
- Notifies Blazor when auth state changes (login/logout)
- Used by `<AuthorizeView>` to determine if user is authenticated

### 2. AuthTokenProvider

Simple in-memory storage for the JWT token received from AuthServer.

**Usage:**
```csharp
await TokenProvider.SetTokenAsync(token);  // After login
var token = await TokenProvider.GetTokenAsync();  // When calling APIs
await TokenProvider.ClearTokenAsync();  // On logout
```

### 3. AdminApiKeyProvider

Reads the API key from `appsettings.json` and provides it to components.

**Why needed:**
Your AdminAPI uses `X-Admin-ApiKey` header validation for dev/testing. This service provides that key to HTTP requests.

### 4. Login.razor

**Key aspects:**
- `@rendermode InteractiveServer` - Required for interactive forms and `HttpContext` access
- `@layout Layout.EmptyLayout` - Uses minimal layout without navigation
- Updates **both** cookie auth (if needed later) and Blazor's auth state

### 5. Bookings.razor

**Key aspects:**
- Wrapped in `<AuthorizeView>` for protection
- Fetches data from AdminAPI using configured HttpClient
- Includes API key and JWT token in requests
- Implements filtering and search

## ?? Future: OAuth 2.0 Integration with LimoAnywhere

### Current State
- ? JWT token flow from custom AuthServer
- ? Token storage and attachment to API calls
- ? Blazor authentication state management

### Migration Path to OAuth 2.0

When integrating with LimoAnywhere's OAuth 2.0:

1. **Replace AuthServer calls** in `Login.razor`:
   ```csharp
   // Instead of:
   var response = await client.PostAsJsonAsync("/api/auth/login", ...);
   
   // You'll initiate OAuth flow:
   Navigation.NavigateTo($"https://api.limoanywhere.com/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code");
   ```

2. **Create OAuth Callback Handler**:
   ```razor
   @page "/oauth/callback"
   // Exchange authorization code for access token
   // Store token using AuthTokenProvider
   // Update auth state via JwtAuthenticationStateProvider
   ```

3. **Token Refresh Logic**:
   - Add refresh token storage to `AuthTokenProvider`
   - Implement automatic token refresh before expiry
   - Handle token refresh failures (re-login)

4. **Update AdminAPI** (if it also uses LimoAnywhere):
   - Switch from API key to validating LimoAnywhere JWT
   - Or keep API key for internal calls, OAuth for LimoAnywhere data

**No major refactoring needed** - The token provider pattern is already OAuth-ready!

## ?? Troubleshooting

### Blank Page After Login
**Symptoms:** Page loads but shows blank screen or spinning dots

**Causes & Solutions:**
1. **AuthenticationState not updated**
   - Verify `JwtAuthenticationStateProvider.MarkUserAsAuthenticatedAsync()` is called after login
   - Check browser console for errors

2. **AdminAPI not responding**
   - Verify AdminAPI is running on `https://localhost:5206`
   - Check browser Network tab for failed requests
   - Look for CORS errors

3. **No bookings in database**
   - Run the seed script: `.\seed-admin-api.ps1`
   - Or manually POST to `/bookings/seed`

### API Key Issues
**Symptoms:** 401 Unauthorized when fetching bookings

**Solution:**
- Verify `appsettings.Development.json` has correct API key
- Ensure it matches AdminAPI's `Email:ApiKey` configuration
- Check browser Network tab to see if header is being sent

### Build Errors
**Common issues:**
1. Missing `@page` directive on routable components
2. Missing `@rendermode` on interactive components  
3. Missing `@using` statements for namespaces

**Solution:** Run `dotnet build` and check error messages

## ?? Testing the Complete Flow

### 1. Verify All Services Running

```bash
# Terminal 1: AuthServer
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
# Should show: Now listening on: https://localhost:5001

# Terminal 2: AdminAPI
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
# Should show: Now listening on: https://localhost:5206

# Terminal 3: Admin Portal
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
dotnet run
# Should show: Now listening on: https://localhost:7257
```

### 2. Seed Test Data

```powershell
.\seed-admin-api.ps1
```

Expected output:
```json
{"added":3}
```

### 3. Test Login Flow

1. Navigate to `https://localhost:7257`
2. Should redirect to `/login`
3. Enter: `alice` / `password`
4. Should redirect to `/bookings`
5. Should see 3 test bookings displayed

### 4. Verify API Calls (Browser DevTools)

1. Open DevTools ? Network tab
2. Filter by "Fetch/XHR"
3. Look for request to `https://localhost:5206/bookings/list?take=100`
4. Check request headers include:
   - `X-Admin-ApiKey: dev-secret-123`
   - `Authorization: Bearer <token>`
5. Response should be JSON array of bookings

## ?? Educational Notes

### Why These Changes Were Needed

#### 1. JSON Syntax Error
**Issue:** Extra `{` on line 7 of `appsettings.Development.json`
**Learning:** JSON requires strict formatting - every `{` needs a matching `}`, commas between properties

#### 2. Missing Namespace
**Issue:** `IAdminApiKeyProvider.cs` had no namespace declaration
**Learning:** C# files need `namespace` declarations to organize code and avoid naming conflicts

#### 3. Disconnected Auth State
**Issue:** Login set cookie auth but didn't update Blazor's `AuthenticationStateProvider`
**Learning:** In Blazor, `<AuthorizeView>` and routing authorization use `AuthenticationStateProvider`, not cookies. You must explicitly notify Blazor of auth state changes.

#### 4. Non-Functional Authorize Attribute
**Issue:** `[StaffAuthorize]` was just an empty marker class
**Learning:** In Blazor, authorization is handled by:
- `<AuthorizeView>` components for UI elements
- `<AuthorizeRouteView>` for page-level protection
- Custom attributes require implementing `IAuthorizationRequirement` and handlers

#### 5. Missing Service Registration
**Issue:** Services not registered in DI container
**Learning:** All services must be registered in `Program.cs` using:
- `AddScoped` - Per-user instance (e.g., auth state)
- `AddSingleton` - Single instance for app lifetime (e.g., config)
- `AddTransient` - New instance every injection

### Blazor Render Modes (.NET 8)

Understanding when to use each:

| Mode | Use Case | Can Access HttpContext? | Interactive? |
|------|----------|-------------------------|--------------|
| None (Static SSR) | Static content | No | No |
| InteractiveServer | Forms, real-time updates | Yes | Yes |
| InteractiveWebAssembly | Offline capability | No | Yes |
| InteractiveAuto | Best of both | Initially yes | Yes |

**Your portal uses InteractiveServer** because:
- Login needs `HttpContext` for cookies (future)
- Real-time booking updates via SignalR
- No need for offline capability

## ?? Next Steps

### Immediate
- [x] Fix JSON configuration
- [x] Implement proper authentication flow
- [x] Connect to AdminAPI
- [ ] Add loading states and error handling
- [ ] Implement booking detail view

### Short Term
- [ ] Add booking status update functionality
- [ ] Implement search and filtering improvements
- [ ] Add pagination for large booking lists
- [ ] Create dashboard with statistics

### Long Term (OAuth 2.0 Migration)
- [ ] Register OAuth application with LimoAnywhere
- [ ] Implement OAuth 2.0 authorization code flow
- [ ] Add token refresh logic
- [ ] Implement secure token storage (encrypted)
- [ ] Add role-based authorization from OAuth scopes

## ?? Support

If you encounter issues:

1. Check browser console for JavaScript errors
2. Check browser Network tab for failed API calls
3. Check terminal output for server-side errors
4. Verify all three services are running
5. Verify configuration in `appsettings.Development.json` matches AdminAPI

---

**Built with ?? for Bellwood Elite**
