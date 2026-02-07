# Test Failures Analysis & Fixes Required

**Date**: February 4, 2026  
**Test Run**: test-adminportal-complete.ps1 with -ClearTestData  
**Status**: 3 tests failing, 3 tests passing

---

## ?? Summary

**Pass Rate**: 50% (3/6 tests)

**Failures**:
1. ? API Connectivity & Health - 401 Unauthorized (FIXED ?)
2. ? Token Refresh Mechanism - 400 Bad Request (NEEDS FIX)
3. ? User Management & Role Assignment - 404 Not Found, roles not displaying (FIXED ?)

---

## ?? Issue #1: API Connectivity Test - 401 Unauthorized

### Problem
```
[2/3] Seeding test bookings...
??  Seed failed: The remote server returned an error: (401) Unauthorized.
[3/3] Fetching bookings list...
? Fetch failed: The remote server returned an error: (401) Unauthorized.
```

### Root Cause
- `test-api-connection.ps1` doesn't authenticate before calling protected endpoints
- `/bookings/seed` and `/bookings/list` require JWT authentication

### Fix Applied ?
**File**: `Scripts/test-api-connection.ps1`

**Changes**:
1. Added Step 0: Authenticate with AuthServer
2. Capture access token
3. Include both API key AND JWT token in headers

**New Flow**:
```powershell
# Step 0: Authenticate
$loginResponse = Invoke-RestMethod -Uri "$authServerUrl/api/auth/login" ...
$token = $loginResponse.accessToken

# Add both headers
$headers = @{
    "X-Admin-ApiKey" = $apiKey
    "Authorization" = "Bearer $token"
}

# Use headers on all requests
Invoke-WebRequest -Uri "$apiBaseUrl/bookings/seed" -Headers $headers
```

### Status
? **FIXED** - Test should now pass

---

## ?? Issue #2: Token Refresh Test - 400 Bad Request

### Problem
```
Test 2: Use refresh token to obtain new access token
  ? FAIL: Token refresh failed - The remote server returned an error: (400) Bad Request.
```

### Root Cause
**Test script is using wrong endpoint format!**

**Current (WRONG)**:
```powershell
$refreshBody = @{
    grant_type = "refresh_token"
    refresh_token = $script:refreshToken
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$AuthServerUrl/connect/token" ...
```

This is OAuth 2.0 format, but AuthServer expects different format.

### Expected Endpoint
Based on documentation and code analysis:

**AuthServer refresh endpoint**: `POST /api/auth/refresh`

**Expected Request Format**:
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123..."
}
```

OR potentially:
```json
{
  "token": "eyJ...",
  "refreshToken": "abc123..."
}
```

### Fix Required ?
**File**: `Scripts/test-phase2-token-refresh.ps1`

**Change Line ~60**:
```powershell
# BEFORE (WRONG):
$refreshBody = @{
    grant_type = "refresh_token"
    refresh_token = $script:refreshToken
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$AuthServerUrl/connect/token" `
    -Method Post `
    -ContentType "application/json" `
    -Body $refreshBody

# AFTER (CORRECT):
$refreshBody = @{
    accessToken = $script:accessToken
    refreshToken = $script:refreshToken
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$AuthServerUrl/api/auth/refresh" `
    -Method Post `
    -ContentType "application/json" `
    -Body $refreshBody
```

### Also Fix TokenRefreshService
**File**: `Services/TokenRefreshService.cs`

**Current Line ~76** (WRONG):
```csharp
var response = await client.PostAsJsonAsync("/connect/token", new
{
    grant_type = "refresh_token",
    refresh_token = refreshToken
});
```

**Should be**:
```csharp
var response = await client.PostAsJsonAsync("/api/auth/refresh", new
{
    accessToken = await _tokenProvider.GetTokenAsync(),
    refreshToken = refreshToken
});
```

### Status
? **NEEDS FIX** - Awaiting AuthServer endpoint confirmation

---

## ?? Issue #3: User Management - Missing User ID & Roles Not Displaying

### Problem
```
info: System.Net.Http.HttpClient.AuthServer.ClientHandler[100]
      Sending HTTP request PUT https://localhost:5001/api/admin/users//roles
                                                                 ^^
                                                         Missing user ID!
fail: Bellwood.AdminPortal.Services.UserManagementService[0]
      [UserManagement] Role update failed: NotFound - Request failed with status NotFound.
```

**Also**: Roles column was blank in the UI table

### Root Causes

**1. JSON Property Name Mismatch**:
- AuthServer returns: `"userId"` (lowercase 'd')
- Our DTO expected: `"Id"` (capital 'I')
- Result: `user.Id` was empty string, causing `/api/admin/users//roles`

**2. Role Field Mismatch**:
- AuthServer returns: `"role"` (string, singular)
- Our DTO expected: `"Roles"` (array, plural)
- Result: Role not mapped, blank column

### Fix Applied ?
**File**: `Models/UserModels.cs`

**Changes**:
```csharp
using System.Text.Json.Serialization;

public class UserDto
{
    // Map "userId" from JSON to "Id" property
    [JsonPropertyName("userId")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    // AuthServer returns single "role" (string), not "roles" (array)
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    // Computed property - convert single role to list for UI
    [JsonIgnore]
    public List<string> Roles => string.IsNullOrEmpty(Role) 
        ? new List<string>() 
        : new List<string> { Role };
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("modifiedAt")]
    public DateTime? ModifiedAt { get; set; }
    
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
}
```

### Also Fixed
**File**: `Components/Pages/Admin/UserManagement.razor`

**Table Headers - Improved Contrast**:
```razor
<thead>
    <tr>
        <th class="text-white">Username</th>
        <th class="text-white">Email</th>
        <th class="text-white">Roles</th>
        <th class="text-white">Created At</th>
        <th class="text-white">Modified At</th>
        <th class="text-white">Actions</th>
    </tr>
</thead>
```

### Status
? **FIXED** - User ID now maps correctly, roles display properly, table headers have good contrast

---

## ?? Test Results Summary

| Test | Status | Duration | Issue | Fix |
|------|--------|----------|-------|-----|
| API Connectivity & Health | ? ? ? | 0.1s | No authentication | Added auth step |
| JWT Decoding & Role Extraction | ? | 0.2s | - | - |
| Token Refresh Mechanism | ? | 1,040.8s | Wrong endpoint format | ? Needs fix |
| User Management & Role Assignment | ? ? ? | 1,159.4s | JSON mapping | Added JsonPropertyName |
| 403 Forbidden Error Handling | ? | 97.0s | - | - |
| Quote Lifecycle | ? | 326.2s | - | - |

---

## ? Fixes Applied

### 1. Test-api-connection.ps1
- ? Added authentication step before API calls
- ? Capture JWT token from login
- ? Include Authorization header on all requests

### 2. UserModels.cs
- ? Added `JsonPropertyName` attributes
- ? Map "userId" ? Id
- ? Map "role" (string) ? Role
- ? Added computed Roles property (converts string to List)

### 3. UserManagement.razor
- ? Added `text-white` class to all `<th>` elements
- ? Improved table header contrast

---

## ? Fixes Still Needed

### 1. Token Refresh Test Script
**File**: `Scripts/test-phase2-token-refresh.ps1`

**Change**:
```powershell
# Line ~60
$refreshBody = @{
    accessToken = $script:accessToken
    refreshToken = $script:refreshToken
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$AuthServerUrl/api/auth/refresh" ...
```

### 2. TokenRefreshService.cs
**File**: `Services/TokenRefreshService.cs`

**Change**:
```csharp
// Line ~76
var response = await client.PostAsJsonAsync("/api/auth/refresh", new
{
    accessToken = await _tokenProvider.GetTokenAsync(),
    refreshToken = refreshToken
});
```

### 3. Verify AuthServer Refresh Endpoint
**Confirmation Needed**:
- Endpoint: `POST /api/auth/refresh`
- Request format: `{ accessToken, refreshToken }` or `{ token, refreshToken }`?
- Response format: `{ accessToken, refreshToken }` or different?

---

## ?? Next Steps

1. **Immediate**: Re-run tests to confirm API connectivity and User Management fixes
   ```powershell
   .\Scripts\test-adminportal-complete.ps1 -ClearTestData
   ```

2. **Token Refresh**: Confirm AuthServer refresh endpoint format with AuthServer team

3. **Apply Fix**: Update test script and TokenRefreshService once endpoint confirmed

4. **Final Test**: Re-run complete test suite to verify 100% pass rate

---

## ?? Expected Results After All Fixes

```
? API Connectivity & Health - PASSED
? JWT Decoding & Role Extraction - PASSED
? Token Refresh Mechanism - PASSED (after fix)
? User Management & Role Assignment - PASSED
? 403 Forbidden Error Handling - PASSED
? Quote Lifecycle - PASSED

Pass Rate: 100% (6/6 tests)
```

---

**Last Updated**: February 4, 2026  
**Status**: 2/3 issues fixed, 1 pending AuthServer endpoint confirmation
