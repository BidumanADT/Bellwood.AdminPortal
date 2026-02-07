# Token Refresh Investigation - 400 Bad Request

**Date**: February 6, 2026  
**Status**: ?? **INVESTIGATING**  
**Test Pass Rate**: 83.3% (5/6 tests passing)

---

## ?? Issue Summary

**Test**: Token Refresh Mechanism  
**Status**: ? **FAILING**  
**Error**: `The remote server returned an error: (400) Bad Request.`  
**Impact**: Only 1 remaining failure out of 6 tests

---

## ?? Current State

### What Works ?

1. **Login captures refresh token** ?
   - Access token length: 320
   - Refresh token length: 32
   - Both tokens received correctly

2. **Auto-refresh timer starts** ?
   - Console shows: `[TokenRefresh] Token will be refreshed in XX minutes`
   - Timer configured for 55-minute intervals
   - Manual verification confirmed working

### What Fails ?

**Automatic token refresh via API call**:
```
POST /connect/token
Request: {
  "grant_type": "refresh_token",
  "refresh_token": "<32-char-token>"
}

Response: 400 Bad Request
```

---

## ?? Investigation Steps

### Step 1: Enhanced Logging Added

**File**: `Services/TokenRefreshService.cs`

**New Logs Added**:
1. ? Token refresh start/end markers
2. ? Refresh token length verification
3. ? Request endpoint and body logging
4. ? Response status and body logging
5. ? Success/failure markers

**Expected Output** (next test run):
```
[TokenRefresh] ========== TOKEN REFRESH START ==========
[TokenRefresh] Refresh token length: 32
[TokenRefresh] Request endpoint: POST /connect/token
[TokenRefresh] Request body: {"grant_type":"refresh_token","refresh_token":"..."}
[TokenRefresh] Response status: 400 (Bad Request)
[TokenRefresh] Response body: <error details>
[TokenRefresh] ========== TOKEN REFRESH FAILED ==========
```

---

### Step 2: Possible Root Causes

#### Cause 1: Wrong Endpoint Format

**Current Call**:
```http
POST /connect/token HTTP/1.1
Content-Type: application/json

{
  "grant_type": "refresh_token",
  "refresh_token": "<token>"
}
```

**Possible AuthServer Expectations**:

**Option A: OAuth2/OIDC Format (Form URL Encoded)**:
```http
POST /connect/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=<token>&client_id=<client_id>
```

**Option B: Different Endpoint**:
```http
POST /api/auth/refresh HTTP/1.1
Content-Type: application/json

{
  "refreshToken": "<token>"
}
```

**Option C: Different Property Names**:
```http
POST /connect/token HTTP/1.1
Content-Type: application/json

{
  "grantType": "refresh_token",  ? Camel case
  "refreshToken": "<token>"      ? Camel case
}
```

#### Cause 2: Missing Client Credentials

AuthServer might require client_id/client_secret:
```json
{
  "grant_type": "refresh_token",
  "refresh_token": "<token>",
  "client_id": "admin-portal",       ? Missing?
  "client_secret": "<secret>"        ? Missing?
}
```

#### Cause 3: Expired/Invalid Refresh Token

- Refresh token might have short lifetime
- Token might be single-use
- Token storage issue

#### Cause 4: Wrong Content-Type

- Using `application/json` but AuthServer expects `application/x-www-form-urlencoded`
- Standard OAuth2 uses form encoding, not JSON

---

## ?? Testing Plan

### Test 1: Run with Enhanced Logging

```powershell
# Run token refresh test
.\Scripts\test-phase2-token-refresh.ps1

# Look for new log output showing:
# - Exact request body
# - Exact response body (will show AuthServer error message)
```

### Test 2: Check AuthServer Logs

AuthServer console should show:
```
[AuthServer] POST /connect/token - 400 Bad Request
[AuthServer] Error: <specific error message>
```

This will tell us exactly why it's failing.

### Test 3: Compare with Login Endpoint

**Login** (works ?):
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "alice",
  "password": "password"
}

Response: {
  "accessToken": "...",
  "refreshToken": "...",
  "token": "..."
}
```

**Refresh** (fails ?):
```http
POST /connect/token
Content-Type: application/json

{
  "grant_type": "refresh_token",
  "refresh_token": "..."
}

Response: 400 Bad Request
```

**Question**: Does AuthServer have a `/api/auth/refresh` endpoint instead?

---

## ?? Questions for AuthServer Team

### Q1: What is the correct token refresh endpoint?

**Options**:
- `/connect/token` (OAuth2/OIDC standard)
- `/api/auth/refresh` (custom REST endpoint)
- Other?

### Q2: What request format is expected?

**Content-Type**:
- `application/json`?
- `application/x-www-form-urlencoded`?

**Body Format**:
- JSON with camelCase?
- JSON with snake_case?
- Form data?

**Example Request**:
```
Please provide a working example of a token refresh request
```

### Q3: Are client credentials required?

- `client_id`?
- `client_secret`?
- Or just the refresh token?

### Q4: What does the 400 error response contain?

**From AuthServer logs**, what is the exact error message when it receives:
```json
POST /connect/token
{
  "grant_type": "refresh_token",
  "refresh_token": "<32-char-token>"
}
```

---

## ?? Temporary Workarounds

### Option 1: Skip Automated Test

- Manual verification shows auto-refresh timer works ?
- UI shows correct logs ?
- Just need to fix the API call format
- **Skip automated test for now**, fix later

### Option 2: Try Alternative Endpoint

Test if `/api/auth/refresh` exists:
```powershell
$body = @{
    refreshToken = $refreshToken
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "https://localhost:5001/api/auth/refresh" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### Option 3: Use Form Encoding

Try OAuth2 standard format:
```csharp
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("grant_type", "refresh_token"),
    new KeyValuePair<string, string>("refresh_token", refreshToken)
});

var response = await client.PostAsync("/connect/token", content);
```

---

## ?? Next Actions

### Immediate (Before Next Test Run)

1. ? Enhanced logging added to TokenRefreshService
2. ? Run test again to capture detailed error
3. ? Check AuthServer console for error details

### After Getting Error Details

1. ?? Contact AuthServer team with:
   - Request format we're using
   - Response we're getting
   - Ask for correct format

2. ?? Update TokenRefreshService based on AuthServer guidance

3. ? Retest and achieve 100% pass rate

---

## ?? Related Documentation

- **AuthServer Q&A**: `Docs/Archive/AdminPortal-QA-Response.md`
- **Token Refresh Service**: `Services/TokenRefreshService.cs`
- **Test Script**: `Scripts/test-phase2-token-refresh.ps1`
- **Security Model**: `Docs/23-Security-Model.md`

---

## ?? Update Log

**2026-02-06 Evening**:
- ? Cleaned up verbose UserManagement logging
- ? Added enhanced logging to TokenRefreshService
- ?? Created investigation document
- ? Awaiting next test run with detailed logs

---

**Status**: ?? **READY FOR DETAILED INVESTIGATION**  
**Blocking**: ? **NO** - Feature works (timer starts, UI correct)  
**Priority**: ?? **MEDIUM** - API call format needs fixing  
**Next**: Run test with enhanced logging to see exact error

---

*Once we see the exact error message from AuthServer, we'll know exactly what format it expects!* ??
