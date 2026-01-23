# Security Model & Authorization

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready (Phase 2 Complete)

---

## ?? Overview

This document describes the authentication and authorization implementation in the Bellwood AdminPortal, including JWT token management, authorization policies, and security best practices.

**Authentication Method**: JWT Bearer tokens from AuthServer  
**Authorization Strategy**: Role-Based Access Control (RBAC)  
**Token Storage**: In-memory singleton service

**Target Audience**: Developers, security engineers, DevOps  
**Prerequisites**: Understanding of JWT, ASP.NET Core Identity, Blazor authentication

---

## ?? Authentication Architecture

### Authentication Flow

```
????????????????????????????????????????????????????????????????
? 1. User Login Request                                        ?
????????????????????????????????????????????????????????????????
                        ?
                        ?
????????????????????????????????????????????????????????????????
? Login.razor                                                   ?
?   - User enters username/password                            ?
?   - HTTP POST to AuthServer /api/auth/login                  ?
????????????????????????????????????????????????????????????????
                        ?
                        ?
????????????????????????????????????????????????????????????????
? AuthServer                                                    ?
?   - Validates credentials (ASP.NET Core Identity)            ?
?   - Generates JWT token with claims                          ?
?   - Returns: { accessToken, refreshToken }                   ?
????????????????????????????????????????????????????????????????
                        ?
                        ?
????????????????????????????????????????????????????????????????
? JwtAuthenticationStateProvider                                ?
?   - MarkUserAsAuthenticatedAsync(username, token)            ?
?   - Store token ? AuthTokenProvider                           ?
?   - Create ClaimsPrincipal with user claims                  ?
?   - NotifyAuthenticationStateChanged()                       ?
????????????????????????????????????????????????????????????????
                        ?
                        ?
????????????????????????????????????????????????????????????????
? Blazor Authentication System                                  ?
?   - <AuthorizeView> components update                        ?
?   - <AuthorizeRouteView> allows protected routes             ?
?   - User identity available in @context                      ?
????????????????????????????????????????????????????????????????
```

---

## ?? JWT Token Structure

### Token Format

**Header**:
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload** (Claims):
```json
{
  "sub": "alice",                    // Username (subject)
  "uid": "a1b2c3d4-5e6f-...",        // User unique identifier
  "userId": "a1b2c3d4-5e6f-...",     // Identity GUID (Phase 1)
  "email": "alice@bellwood.com",     // User email
  "role": "admin",                   // User role (admin, dispatcher, booker, driver)
  "exp": 1735689600,                 // Expiration timestamp (Unix epoch)
  "iss": "BellwoodAuthServer",       // Issuer
  "aud": "BellwoodAPI"               // Audience
}
```

**Signature**:
```
HMACSHA256(
  base64UrlEncode(header) + "." +
  base64UrlEncode(payload),
  secret
)
```

---

### Required Claims

| Claim | Type | Description | Example |
|-------|------|-------------|---------|
| `sub` | string | Username (login identifier) | "alice" |
| `uid` | string | User unique ID | "a1b2c3d4-..." |
| `userId` | string | Identity GUID (Phase 1+) | "a1b2c3d4-..." |
| `role` | string | User role | "admin", "dispatcher", "booker", "driver" |
| `exp` | int | Expiration (Unix timestamp) | 1735689600 |

**Optional Claims**:
- `email` - User email address
- `iss` - Token issuer
- `aud` - Token audience

---

## ?? Token Management

### AuthTokenProvider (Singleton)

**Purpose**: In-memory storage for JWT token

**File**: `Services/AuthTokenProvider.cs`

```csharp
public class AuthTokenProvider : IAuthTokenProvider
{
    private string? _cachedToken;

    public Task<string?> GetTokenAsync()
        => Task.FromResult(_cachedToken);

    public Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync()
    {
        _cachedToken = null;
        return Task.CompletedTask;
    }
}
```

**Registration** (Program.cs):
```csharp
builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
```

**Why Singleton?**
- ? Token persists across Blazor circuits during navigation
- ? Same token instance shared across all components
- ? No token loss during circuit recreation
- ? Scoped would create new instances per circuit = lost token

**See**: [01-System-Architecture.md](01-System-Architecture.md#service-lifetime-critical-design-decision) for detailed rationale

---

### JwtAuthenticationStateProvider (Singleton)

**Purpose**: Bridge between JWT tokens and Blazor authentication system

**File**: `Services/JwtAuthenticationStateProvider.cs`

```csharp
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenProvider _tokenProvider;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    // Called by Blazor to get current auth state
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    // Called after successful login
    public async Task MarkUserAsAuthenticatedAsync(string username, string token)
    {
        // 1. Store token
        await _tokenProvider.SetTokenAsync(token);

        // 2. Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Staff"),     // TODO: Extract from JWT in Phase 2
            new("access_token", token)
        };

        // 3. Create authenticated principal
        _currentUser = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "jwt"));

        // 4. Notify Blazor of auth state change
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }

    // Called on logout
    public async Task MarkUserAsLoggedOutAsync()
    {
        await _tokenProvider.ClearTokenAsync();
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }
}
```

**Registration** (Program.cs):
```csharp
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>()
);
```

---

## ?? User Roles

### Role Definitions

| Role | Access Level | Permissions | Status |
|------|--------------|-------------|--------|
| **admin** | Full system access | All features, including billing and user management | ? Implemented |
| **dispatcher** | Operational access | Bookings, quotes, driver assignment (billing masked) | ?? Phase 2 |
| **booker** | Limited access | Create bookings/quotes, view own data only | ? Implemented (backend) |
| **driver** | Driver-specific | Assigned rides, GPS updates, status changes | ? Implemented |

---

### Role Hierarchy

```
???????????????????????????????????????????????????????????????
?                        admin                                 ?
?  ?? Full system access                                      ?
?  ?? User management                                         ?
?  ?? Billing and financial data                              ?
?  ?? All operational features                                ?
?  ?? System configuration                                    ?
???????????????????????????????????????????????????????????????
                            ?
                            ?
???????????????????????????????????????????????????????????????
?                     dispatcher (Phase 2)                     ?
?  ?? Operational access (bookings, quotes, drivers)          ?
?  ?? Driver assignment                                       ?
?  ?? Real-time tracking                                      ?
?  ?? ? No billing/financial data                           ?
???????????????????????????????????????????????????????????????
                            ?
                            ?
???????????????????????????????????????????????????????????????
?                         booker                               ?
?  ?? Create bookings and quotes                              ?
?  ?? View own bookings only                                  ?
?  ?? ? No admin or operational features                    ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
?                         driver                               ?
?  ?? View assigned rides                                     ?
?  ?? Update ride status                                      ?
?  ?? Send GPS updates                                        ?
?  ?? ? No access to portal (mobile app only)               ?
???????????????????????????????????????????????????????????????
```

---

## ??? Authorization Policies

### Policy Definitions (Program.cs)

```csharp
// Current implementation
builder.Services.AddAuthorizationCore();

// Phase 2: Add policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("admin", "dispatcher"));

    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());
});
```

---

### Policy Matrix

| Policy | Roles | Use Case | Status |
|--------|-------|----------|--------|
| **AdminOnly** | admin | User management, billing, system config | ?? Phase 2 |
| **StaffOnly** | admin, dispatcher | Operational features (bookings, quotes) | ?? Phase 2 |
| **Authenticated** | All authenticated users | Basic portal access | ? Current |

---

### Applying Policies

**Route-Level Authorization** (Not currently used):
```csharp
// Phase 2: Apply to pages
@attribute [Authorize(Policy = "AdminOnly")]
```

**Component-Level Authorization**:
```razor
<!-- Current implementation -->
<AuthorizeView>
    <Authorized>
        <!-- Content for authenticated users -->
    </Authorized>
    <NotAuthorized>
        @{ Navigation.NavigateTo("/login"); }
    </NotAuthorized>
</AuthorizeView>

<!-- Phase 2: Role-based -->
<AuthorizeView Roles="admin">
    <Authorized>
        <button @onclick="ManageUsers">Manage Users</button>
    </Authorized>
</AuthorizeView>

<AuthorizeView Roles="admin,dispatcher">
    <Authorized>
        <button @onclick="AssignDriver">Assign Driver</button>
    </Authorized>
</AuthorizeView>
```

---

## ?? Securing API Calls

### Adding Authorization Headers

**Pattern Used in Services**:

```csharp
// QuoteService.cs
private async Task<HttpClient> GetAuthorizedClientAsync()
{
    var client = _httpFactory.CreateClient("AdminAPI");
    
    // Add API key
    var apiKey = _apiKeyProvider.GetApiKey();
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "X-Admin-ApiKey", apiKey);
    
    // Add JWT token
    var token = await _tokenProvider.GetTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    return client;
}
```

**Request Headers**:
```http
GET /bookings/list?take=100 HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## ?? Security Best Practices

### Current Implementation

**? Implemented**:
1. **HTTPS Only** - All connections use HTTPS in production
2. **Token Storage** - JWT stored in memory (not localStorage)
3. **Singleton Services** - Prevents token loss during navigation
4. **Authorization Checks** - `<AuthorizeView>` protects UI elements
5. **API Key + JWT** - Dual authentication for AdminAPI calls
6. **403 Handling** - User-friendly error messages for unauthorized access

**?? Phase 2 Enhancements**:
1. **JWT Decoding** - Extract roles from token for client-side checks
2. **Role-Based Policies** - AdminOnly, StaffOnly policies enforced
3. **Field Masking** - Hide billing data from dispatchers
4. **Token Refresh** - Automatic refresh before expiration

---

### Token Security

**Current Approach**:
- ? Tokens stored in memory (Singleton service)
- ? Cleared on logout
- ? Not persisted to browser storage
- ? HTTPS prevents interception

**Risks Mitigated**:
- ? XSS attacks cannot steal token from localStorage
- ? Token not visible in browser DevTools Application tab
- ? Refresh doesn't expose token

**Trade-offs**:
- ?? Token lost on browser close (must re-login)
- ?? Tab refresh requires re-login
- ? Acceptable for admin portal (security over convenience)

---

### API Key Security

**Current Implementation**:

**File**: `Services/AdminApiKeyProvider.cs`

```csharp
public class AdminApiKeyProvider : IAdminApiKeyProvider
{
    private readonly IConfiguration _configuration;

    public AdminApiKeyProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetApiKey()
    {
        return _configuration["AdminAPI:ApiKey"] ?? string.Empty;
    }
}
```

**Configuration** (appsettings.Development.json):
```json
{
  "AdminAPI": {
    "ApiKey": "dev-secret-123"
  }
}
```

**Production** (appsettings.Production.json):
```json
{
  "AdminAPI": {
    "ApiKey": ""  // Load from Azure Key Vault
  }
}
```

**Best Practices**:
- ? Never commit production keys to source control
- ? Use environment variables or Key Vault
- ? Rotate keys regularly (quarterly)
- ? Different keys per environment (dev, staging, prod)

---

## ?? Common Security Pitfalls

### Pitfall 1: Storing JWT in localStorage

**Problem**: XSS attacks can steal tokens from localStorage

**Bad Practice**:
```javascript
// ? DON'T DO THIS
localStorage.setItem('authToken', token);
```

**Our Solution**:
```csharp
// ? Store in memory (Singleton service)
private string? _cachedToken;
```

**Impact**: Token only exists in server memory, not accessible to JavaScript

---

### Pitfall 2: Using Scoped Services for Auth

**Problem**: Blazor Server creates new circuits during navigation, losing scoped instances

**Bad Practice**:
```csharp
// ? This causes token loss
builder.Services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();
```

**Our Solution**:
```csharp
// ? Singleton persists across circuits
builder.Services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();
```

**Impact**: Fixed "blank page after login" critical bug

**See**: [32-Troubleshooting.md](32-Troubleshooting.md#service-lifetime-issue) for details

---

### Pitfall 3: Trusting Client-Side Checks Only

**Problem**: UI authorization can be bypassed by manipulating browser

**Bad Practice**:
```razor
<!-- ? Only hiding UI, not securing API -->
@if (isAdmin)
{
    <button @onclick="DeleteUser">Delete User</button>
}
```

**Our Solution**:
```csharp
// ? Backend enforces authorization
[Authorize(Policy = "AdminOnly")]
public IActionResult DeleteUser(string userId)
{
    // Backend verifies JWT role claim
}
```

**Defense in Depth**:
- ? Hide UI elements based on role (UX)
- ? Backend enforces authorization (Security)

---

### Pitfall 4: Not Handling Token Expiration

**Problem**: Expired tokens cause 401 errors without clear feedback

**Current Limitation**:
- Token expiration not handled automatically
- User sees generic "Unauthorized" errors

**Phase 2 Enhancement**:
```csharp
// Planned: Automatic token refresh
private async Task<string?> GetValidTokenAsync()
{
    var token = await _tokenProvider.GetTokenAsync();
    
    // Decode JWT to check expiration
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
    var exp = jsonToken?.ValidTo;
    
    // Refresh if expiring soon (< 5 minutes)
    if (exp.HasValue && exp.Value < DateTime.UtcNow.AddMinutes(5))
    {
        token = await RefreshTokenAsync();
    }
    
    return token;
}
```

---

## ?? Login & Logout Flows

### Login Flow (Detailed)

**File**: `Components/Pages/Login.razor`

```csharp
private async Task HandleLogin()
{
    isLoggingIn = true;
    errorMessage = null;

    try
    {
        // 1. Create HTTP client for AuthServer
        var client = HttpFactory.CreateClient("AuthServer");

        // 2. Prepare login request
        var loginRequest = new { username, password };
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // 3. Check response
        if (response.IsSuccessStatusCode)
        {
            // 4. Parse JWT token from response
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result?.AccessToken != null)
            {
                Console.WriteLine("[Login] Login successful, updating auth state");

                // 5. Update Blazor auth state
                await AuthStateProvider.MarkUserAsAuthenticatedAsync(username, result.AccessToken);

                Console.WriteLine("[Login] Auth state updated, navigating to /main");

                // 6. Navigate to dashboard
                Navigation.NavigateTo("/main", forceLoad: false);
            }
        }
        else
        {
            errorMessage = "Invalid username or password.";
        }
    }
    catch (Exception ex)
    {
        errorMessage = "Login failed. Please try again.";
        Console.WriteLine($"[Login] Error: {ex.Message}");
    }
    finally
    {
        isLoggingIn = false;
    }
}
```

---

### Logout Flow

**File**: `Components/Pages/Logout.razor`

```csharp
protected override async Task OnInitializedAsync()
{
    Console.WriteLine("[Logout] Logging out user");

    // 1. Clear authentication state
    await AuthStateProvider.MarkUserAsLoggedOutAsync();

    Console.WriteLine("[Logout] Auth state cleared, redirecting to login");

    // 2. Redirect to login page
    Navigation.NavigateTo("/login", forceLoad: true);
}
```

**What Happens**:
1. `MarkUserAsLoggedOutAsync()` clears token from `AuthTokenProvider`
2. `_currentUser` set to empty `ClaimsPrincipal`
3. `NotifyAuthenticationStateChanged()` triggers UI update
4. `<AuthorizeView>` components show `<NotAuthorized>` content
5. User redirected to login page

---

## ?? Security Checklist

### Development

- [x] JWT tokens not stored in localStorage
- [x] Authentication services use Singleton lifetime
- [x] HTTPS enforced in production
- [x] API keys in configuration (not hardcoded)
- [x] 403 errors handled with user-friendly messages
- [x] JWT decoding for role extraction (Phase 2)
- [x] Token refresh logic (Phase 2)

### Production

- [ ] SSL certificates installed and valid
- [ ] Production API keys in Azure Key Vault
- [ ] Secrets not committed to source control
- [ ] Rate limiting configured on API endpoints
- [ ] CORS policies properly configured
- [ ] Security headers configured (HSTS, CSP, etc.)
- [ ] Logging configured for security events

---

## ?? Phase 2 Enhancements

### JWT Decoding

**Planned Implementation**:

```csharp
using System.IdentityModel.Tokens.Jwt;

public async Task MarkUserAsAuthenticatedAsync(string username, string token)
{
    await _tokenProvider.SetTokenAsync(token);

    // Decode JWT to extract claims
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

    var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
    var role = jsonToken?.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
    var email = jsonToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

    // Create claims from JWT
    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, username),
        new(ClaimTypes.Role, role ?? "User"),
        new(ClaimTypes.Email, email ?? ""),
        new("userId", userId ?? ""),
        new("access_token", token)
    };

    _currentUser = new ClaimsPrincipal(
        new ClaimsIdentity(claims, authenticationType: "jwt"));

    NotifyAuthenticationStateChanged(
        Task.FromResult(new AuthenticationState(_currentUser)));
}
```

**Benefits**:
- Extract actual role from JWT (not hardcoded "Staff")
- Enable role-based UI logic
- Access userId for audit logging

**Library**: `System.IdentityModel.Tokens.Jwt` NuGet package

---

### Token Refresh

**Planned Implementation**:

```csharp
public async Task<string?> RefreshTokenIfNeededAsync()
{
    var token = await _tokenProvider.GetTokenAsync();
    if (string.IsNullOrEmpty(token))
        return null;

    // Check if token is expiring soon
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);
    var exp = jsonToken?.ValidTo;

    if (exp.HasValue && exp.Value < DateTime.UtcNow.AddMinutes(5))
    {
        // Call AuthServer refresh endpoint
        var client = _httpFactory.CreateClient("AuthServer");
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { token });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RefreshResponse>();
            if (result?.AccessToken != null)
            {
                await _tokenProvider.SetTokenAsync(result.AccessToken);
                return result.AccessToken;
            }
        }
    }

    return token;
}

````````markdown
## ? Phase 2 Security Enhancements (IMPLEMENTED - January 18, 2026)

### JWT Token Decoding

**Status**: ? **IMPLEMENTED**

**Implementation**: `Services/JwtAuthenticationStateProvider.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;

private List<Claim> DecodeJwtToken(string token)
{
    var claims = new List<Claim>();
    var handler = new JwtSecurityTokenHandler();
    
    if (!handler.CanReadToken(token))
    {
        Console.WriteLine("[AuthStateProvider] Token is not readable");
        return claims;
    }
    
    try
    {
        var jsonToken = handler.ReadJwtToken(token);
        
        // Extract claims
        var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var role = jsonToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = jsonToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        
        // Add to ClaimsPrincipal
        if (!string.IsNullOrEmpty(username))
            claims.Add(new Claim(ClaimTypes.Name, username));
        
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));
        else
            claims.Add(new Claim(ClaimTypes.Role, "Staff")); // Fallback
        
        if (!string.IsNullOrEmpty(userId))
            claims.Add(new Claim("userId", userId));
        
        claims.Add(new Claim("access_token", token));
        
        Console.WriteLine($"[AuthStateProvider] Decoded - User: {username}, Role: {role}, UserId: {userId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AuthStateProvider] Failed to decode JWT: {ex.Message}");
    }
    
    return claims;
}
```

**Benefits**:
- ? Extracts actual role from JWT (admin, dispatcher, booker, driver)
- ? Enables role-based UI logic via `<AuthorizeView Roles="admin">`
- ? Access userId for audit logging
- ? ClaimsPrincipal populated with real JWT claims

**Library**: `System.IdentityModel.Tokens.Jwt` v8.0.0

---

### Automatic Token Refresh

**Status**: ? **IMPLEMENTED**

**Implementation**: `Services/TokenRefreshService.cs`

**Features**:
- ? Captures refresh token on login
- ? Stores refresh token in memory (singleton)
- ? Auto-refreshes 5 minutes before expiry
- ? Refresh timer runs every 55 minutes
- ? Updates authentication state with new token

**Token Refresh Flow**:
```csharp
public async Task<bool> RefreshTokenAsync()
{
    var refreshToken = await _tokenProvider.GetRefreshTokenAsync();
    if (string.IsNullOrEmpty(refreshToken))
    {
        _logger.LogWarning("[TokenRefresh] No refresh token available");
        return false;
    }
    
    var client = _httpFactory.CreateClient("AuthServer");
    
    var response = await client.PostAsJsonAsync("/connect/token", new
    {
        grant_type = "refresh_token",
        refresh_token = refreshToken
    });
    
    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning("[TokenRefresh] Failed to refresh token");
        return false;
    }
    
    var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
    
    // Update stored tokens
    await _tokenProvider.SetTokenAsync(result.AccessToken);
    if (!string.IsNullOrEmpty(result.RefreshToken))
        await _tokenProvider.SetRefreshTokenAsync(result.RefreshToken);
    
    // Update authentication state
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(result.AccessToken);
    var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "Unknown";
    
    await _authStateProvider.MarkUserAsAuthenticatedAsync(username, result.AccessToken);
    
    _logger.LogInformation("[TokenRefresh] Token refreshed successfully");
    return true;
}
```

**Auto-Refresh Timer**:
```csharp
public async Task StartAutoRefreshAsync()
{
    var token = await _tokenProvider.GetTokenAsync();
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);
    var expiresAt = jsonToken.ValidTo;
    
    // Refresh 5 minutes before expiry
    var refreshAt = expiresAt.AddMinutes(-5);
    var timeUntilRefresh = refreshAt - DateTime.UtcNow;
    
    _logger.LogInformation($"[TokenRefresh] Token will be refreshed in {timeUntilRefresh.TotalMinutes:F1} minutes");
    
    _refreshTimer = new Timer(
        async _ => await RefreshTokenAsync(),
        null,
        timeUntilRefresh,
        TimeSpan.FromMinutes(55) // Refresh every 55 minutes thereafter
    );
}
```

**Security Benefits**:
- ? Users don't lose session during long portal sessions
- ? Token rotation reduces window of opportunity for token theft
- ? Refresh tokens can be revoked server-side
- ? Seamless user experience (no unexpected logouts)

**Storage**: Refresh tokens stored in memory (singleton), cleared on logout

---

### Blazor Authentication Integration

**Status**: ? **IMPLEMENTED**

**Implementation**: `Services/BlazorAuthenticationHandler.cs`

**Purpose**: Bridges Blazor Server's `AuthenticationStateProvider` with ASP.NET Core's authentication middleware, enabling `[Authorize]` attribute support on Razor pages.

```csharp
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
```

**Registration** (`Program.cs`):
```csharp
// Add authentication services
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BlazorAuthenticationHandler>("Blazor", options => { });

// Add authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("admin", "dispatcher"));
});

// ...

var app = builder.Build();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();
```

**Benefits**:
- ? `[Authorize(Roles = "admin")]` attribute works on Razor pages
- ? Proper 403 Forbidden responses for unauthorized access
- ? ASP.NET Core authorization policies enforced
- ? Blazor authentication integrated with HTTP context

**Impact**: Fixed critical "InvalidOperationException: Unable to find IAuthenticationService" error

---

### Authorization Policies (Phase 2)

**Status**: ? **IMPLEMENTED**

**Policy Definitions**:

```csharp
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("admin", "dispatcher"));
});
```

**Policy Matrix** (Updated):

| Policy | Roles | Use Case | Status |
|--------|-------|----------|--------|
| **AdminOnly** | admin | User management, billing, OAuth credentials | ? Implemented |
| **StaffOnly** | admin, dispatcher | Operational features (bookings, quotes, affiliates) | ? Implemented |
| **Authenticated** | All authenticated users | Basic portal access | ? Implemented |

**Usage Examples**:

**Page-Level**:
```csharp
@page "/admin/users"
@attribute [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
```

**Component-Level**:
```razor
<AuthorizeView Roles="admin">
    <Authorized>
        <NavLink href="admin/users">User Management</NavLink>
    </Authorized>
</AuthorizeView>

<AuthorizeView Roles="admin,dispatcher">
    <Authorized>
        <NavLink href="bookings">Bookings</NavLink>
    </Authorized>
</AuthorizeView>
```

---

### Enhanced 403 Forbidden Handling

**Status**: ? **IMPLEMENTED** (All Services)

**Services with 403 Handling**:
- ? `AffiliateService` - All methods
- ? `QuoteService` - All methods
- ? `UserManagementService` - All methods
- ? `DriverTrackingService` - All methods

**Pattern**:
```csharp
public async Task<List<UserDto>> GetAllUsersAsync(string? roleFilter = null)
{
    var client = await GetAuthorizedClientAsync();
    var response = await client.GetAsync("/api/admin/users");
    
    // Phase 2: Handle 403 Forbidden
    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
        _logger.LogWarning("[Service] Access denied");
        throw new UnauthorizedAccessException("Access denied. You do not have permission to access this resource.");
    }
    
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();
}
```

**Benefits**:
- ? User-friendly error messages
- ? No raw HTTP status codes exposed
- ? Consistent error handling across all services
- ? Errors logged for debugging

---

### User Management Security

**Status**: ? **IMPLEMENTED**

**Features**:
- ? Admin-only user list access
- ? Admin-only role assignment
- ? Role changes require confirmation
- ? Dispatcher blocked from user management (403)

**Access Control**:

**Admin Users** (alice, bob):
```
? GET /api/admin/users - List all users
? GET /api/admin/users?role=admin - Filter by role
? PUT /api/admin/users/{username}/role - Change user role
```

**Dispatcher Users** (diana):
```
? GET /api/admin/users - 403 Forbidden
? PUT /api/admin/users/{username}/role - 403 Forbidden
```

**Authorization Enforcement**:
```csharp
// UserManagementService.cs
if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    _logger.LogWarning("[UserManagement] Access denied to user list");
    throw new UnauthorizedAccessException("Access denied. You do not have permission to view users. Admin role required.");
}
```

**UI Protection**:
```razor
<!-- UserManagement.razor -->
@attribute [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]

<AuthorizeView Roles="admin">
    <Authorized>
        <!-- User management UI -->
    </Authorized>
    <NotAuthorized>
        <!-- Redirect to login -->
    </NotAuthorized>
</AuthorizeView>
```

---
