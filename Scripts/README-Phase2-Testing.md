# Phase 2 Testing Scripts - README

**Directory**: `Scripts/`  
**Purpose**: Automated and manual testing for Phase 2 implementation  
**PowerShell Version**: 5.1 Compatible

---

## ?? Available Test Scripts

### Automated Tests

| Script | Description | Estimated Time |
|--------|-------------|----------------|
| `test-phase2-jwt-decoding.ps1` | Tests JWT token parsing and claim extraction | 1 minute |
| `test-phase2-token-refresh.ps1` | Tests automatic token refresh functionality | 2 minutes |
| `test-phase2-user-management.ps1` | Tests user management API and role assignment | 2 minutes |
| `test-phase2-complete.ps1` | **Master test runner** - runs all tests | 5-10 minutes |

### Manual Test Guides

| Script | Description | Estimated Time |
|--------|-------------|----------------|
| `test-phase2-role-ui.ps1` | Guided manual testing for role-based UI | 5 minutes |
| `test-phase2-403-handling.ps1` | Guided manual testing for 403 error handling | 5 minutes |
| `ManualTestGuide-Phase2.md` | Complete manual testing guide (Markdown) | 15-20 minutes |

---

## ?? Quick Start

### Prerequisites

1. **Start all three servers**:
   ```powershell
   # Terminal 1: AuthServer
   cd path\to\AuthServer
   dotnet run
   
   # Terminal 2: AdminAPI
   cd path\to\AdminAPI
   dotnet run
   
   # Terminal 3: AdminPortal
   cd path\to\Bellwood.AdminPortal
   dotnet run
   ```

2. **Verify servers are running**:
   - AuthServer: https://localhost:5001
   - AdminAPI: https://localhost:5206
   - AdminPortal: https://localhost:7257

---

## ?? Recommended Testing Sequence

### Option 1: Run All Tests (Recommended)

```powershell
cd Scripts
.\test-phase2-complete.ps1
```

This will:
1. Verify all servers are running
2. Run all automated tests
3. Prompt for manual tests
4. Display comprehensive summary

---

### Option 2: Run Individual Tests

**Step 1: JWT Decoding**
```powershell
.\test-phase2-jwt-decoding.ps1
```

**Step 2: Token Refresh**
```powershell
.\test-phase2-token-refresh.ps1
```

**Step 3: User Management**
```powershell
.\test-phase2-user-management.ps1
```

**Step 4: Role-Based UI (Manual)**
```powershell
.\test-phase2-role-ui.ps1
```

**Step 5: 403 Handling (Manual)**
```powershell
.\test-phase2-403-handling.ps1
```

---

### Option 3: Automated Tests Only

```powershell
.\test-phase2-complete.ps1 -AutomatedOnly
```

Skips manual tests for quick validation.

---

### Option 4: Manual Tests Only

```powershell
.\test-phase2-complete.ps1 -ManualOnly
```

Runs only manual test guides.

---

## ?? Test Script Parameters

### test-phase2-complete.ps1

```powershell
.\test-phase2-complete.ps1 `
    -AuthServerUrl "https://localhost:5001" `
    -AdminAPIUrl "https://localhost:5206" `
    -AdminPortalUrl "https://localhost:7257" `
    -AutomatedOnly  # Optional: skip manual tests
```

### Individual Test Scripts

All automated scripts accept `-AuthServerUrl` parameter:

```powershell
.\test-phase2-jwt-decoding.ps1 -AuthServerUrl "https://custom-url:5001"
```

Manual test scripts accept `-AdminPortalUrl` parameter:

```powershell
.\test-phase2-role-ui.ps1 -AdminPortalUrl "https://custom-url:7257"
```

---

## ? Expected Test Results

### All Tests Pass

```
========================================================
  PHASE 2 TEST SUITE - FINAL SUMMARY
========================================================

Total Tests Run: 6
Passed:          6
Failed:          0
Skipped:         0

? ALL TESTS PASSED! Phase 2 implementation is successful!
```

### Some Tests Fail

Review the detailed output for each test to identify issues.

Common failure reasons:
- Servers not running
- Test user accounts missing
- JWT missing role/userId claims
- Authorization policies not configured

---

## ?? Test Coverage

### Phase 2.1: JWT Decoding & Role Extraction
- ? Admin JWT contains `role: "admin"`
- ? Dispatcher JWT contains `role: "dispatcher"`
- ? All JWTs contain `userId` (GUID)
- ? All JWTs contain `sub` (username)
- ? Refresh token returned on login

### Phase 2.2: Token Refresh
- ? Refresh token captured on login
- ? Refresh token can obtain new access token
- ? Auto-refresh timer starts on main page
- ? New token differs from original

### Phase 2.3: Role-Based UI
- ? Admin sees all navigation items
- ? Admin sees ADMINISTRATION section
- ? Dispatcher sees operational items only
- ? Dispatcher cannot see admin section
- ? Username and role badge displayed correctly

### Phase 2.4: User Management
- ? Admin can list all users
- ? Admin can filter users by role
- ? Admin can change user roles
- ? Dispatcher denied access (403)
- ? Role changes persist

### Phase 2.5: 403 Error Handling
- ? Dispatcher blocked from admin pages
- ? User-friendly error messages displayed
- ? No raw HTTP errors exposed
- ? Admin has full access to all pages

---

## ?? Troubleshooting

### Test Scripts Don't Run

**Issue**: "Execution policy" error

**Solution**:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

### SSL Certificate Errors

**Issue**: "The remote certificate is invalid"

**Solution**: Scripts include SSL bypass for development. If still failing:
1. Verify HTTPS URLs are correct
2. Check server certificates
3. Use `-SkipCertificateCheck` parameter (if available)

### Test User Not Found

**Issue**: "User 'diana' not found"

**Solution**:
1. Verify diana account exists in AuthServer
2. Confirm password is "password"
3. Check AuthServer seed data

### 403 Tests Not Triggering

**Issue**: Dispatcher has access when 403 expected

**Solution**:
1. AdminAPI may allow dispatcher access (by design)
2. Review AdminAPI authorization policies
3. If dispatcher should have access, test is passing (no 403 expected)

---

## ?? Test Metrics

| Metric | Target | Actual |
|--------|--------|--------|
| Automated Tests | 15+ | 15+ |
| Manual Tests | 10+ | 10+ |
| Code Coverage | 80%+ | TBD |
| Pass Rate | 100% | TBD |

---

## ?? Support

For issues with test scripts:
1. Review `Docs/32-Troubleshooting.md`
2. Check PowerShell version: `$PSVersionTable.PSVersion`
3. Verify server logs for errors
4. Contact development team

---

## ?? Continuous Testing

### After Code Changes

Run quick automated tests:
```powershell
.\test-phase2-complete.ps1 -AutomatedOnly
```

### Before Deployment

Run full test suite:
```powershell
.\test-phase2-complete.ps1
```

### Integration Testing

Combine with AdminAPI and AuthServer tests for complete validation.

---

**Last Updated**: January 18, 2026  
**Version**: 1.0  
**Status**: ? Ready for Use

---

*For detailed manual testing procedures, see `ManualTestGuide-Phase2.md`* ??
