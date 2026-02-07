# User Management Debugging Guide - Enhanced Logging

**Date**: February 5, 2026  
**Purpose**: Deep investigation of role change behavior  
**Status**: ? **Enhanced Debugging Ready**

---

## ?? What We Added

### Comprehensive Logging Points

**Service Layer** (`UserManagementService.cs`):
1. ? Role update start/end markers
2. ? Request details (URL, body, JSON serialization)
3. ? Role value inspection (type, length, hex encoding)
4. ? Response status and body
5. ? User fetch logging with role values
6. ? Exception details with full context

**UI Layer** (`UserManagement.razor`):
1. ? Modal open with current role state
2. ? Role selection tracking
3. ? Confirmation flow with all values
4. ? User reload with before/after comparison
5. ? Role normalization tracking

---

## ?? How to Use the Enhanced Logging

### Step 1: Restart AdminPortal

**IMPORTANT**: Stop the current instance first!

```powershell
# In AdminPortal terminal:
# Press Ctrl+C to stop

# Wait for clean shutdown

# Then restart:
dotnet run
```

### Step 2: Open Browser Console

**Before testing**:
1. Open AdminPortal in browser
2. Press **F12** to open DevTools
3. Go to **Console** tab
4. Clear console (trash icon)
5. Keep console visible during test

### Step 3: Perform Role Change Test

**Test Procedure**:
1. Login as `alice`
2. Navigate to User Management
3. **Check console for initial load logs**
4. Click "Edit Roles" for charlie
5. **Check console for modal logs**
6. Select "dispatcher" radio button
7. **Check console for selection logs**
8. Click "Save Roles"
9. **Check console for update flow**
10. Wait for page reload
11. **Check console for reload logs**

---

## ?? Expected Log Flow

### Phase 1: Initial Page Load

**Browser Console**:
```
[UserManagement.UI] ========== LOAD USERS START ==========
[UserManagement.UI] Calling UserService.GetUsersAsync()
[UserManagement.UI] Received 12 users
[UserManagement.UI]   - alice: Role='admin', Roles.Count=1
[UserManagement.UI]   - bob: Role='admin', Roles.Count=1
[UserManagement.UI]   - charlie: Role='driver', Roles.Count=1
[UserManagement.UI]   - diana: Role='dispatcher', Roles.Count=1
[UserManagement.UI] ========== LOAD USERS COMPLETE ==========
```

**Server Console**:
```
[UserManagement] ========== FETCHING USERS ==========
[UserManagement] Users fetch response: 200
[UserManagement] Loaded 12 users
[UserManagement]   - alice: role=admin (property), Role property=admin
[UserManagement]   - charlie: role=driver (property), Role property=driver
[UserManagement] ========== USERS LOADED ==========
```

**? What to Check**:
- Charlie shows `Role='driver'` ?
- No errors or exceptions

---

### Phase 2: Opening Role Modal

**Browser Console**:
```
[UserManagement.UI] ========== SHOW ROLE MODAL ==========
[UserManagement.UI] User: charlie
[UserManagement.UI] Current Role property: 'driver'
[UserManagement.UI] Current Roles list count: 1
[UserManagement.UI] Roles list contents: 'driver'
[UserManagement.UI] NormalizeRoleSelection input: 'driver'
[UserManagement.UI] NormalizeRoleSelection output: 'driver' (matched: true)
[UserManagement.UI] Selected roles after initialization: 'driver'
[UserManagement.UI] Available roles: 'passenger', 'driver', 'dispatcher', 'admin'
[UserManagement.UI] ========== MODAL SHOWN ==========
```

**? What to Check**:
- Current role is 'driver' (lowercase) ?
- Matched successfully ?
- Radio button for 'driver' should be selected

---

### Phase 3: Selecting New Role

**Browser Console**:
```
[UserManagement.UI] SelectSingleRole called with: 'dispatcher'
[UserManagement.UI] Role length: 10, type: String
[UserManagement.UI] Selected roles after change: 'dispatcher'
```

**? What to Check**:
- Role is lowercase 'dispatcher' ?
- No uppercase characters
- Role length matches expected (10 characters)

---

### Phase 4: Confirming Role Change

**Browser Console**:
```
[UserManagement.UI] ========== CONFIRM ROLE CHANGE ==========
[UserManagement.UI] Selected user: charlie
[UserManagement.UI] Current role: 'driver'
[UserManagement.UI] Selected roles count: 1
[UserManagement.UI] Selected roles: 'dispatcher'
[UserManagement.UI] New role to assign: 'dispatcher'
[UserManagement.UI] Calling UpdateUserRoleAsync('charlie', 'dispatcher')
```

**Server Console**:
```
[UserManagement] ========== ROLE UPDATE START ==========
[UserManagement] Updating role for user charlie to dispatcher
[UserManagement] Role value type: String, length: 10
[UserManagement] Role value (hex): 64-69-73-70-61-74-63-68-65-72
[UserManagement] Request URL: /api/admin/users/charlie/role
[UserManagement] Request body: {"role":"dispatcher"}
[UserManagement] Response status: 200 (OK)
[UserManagement] Response body: {"message":"Successfully assigned role...","newRole":"dispatcher"}
[UserManagement] ========== ROLE UPDATE SUCCESS ==========
```

**? What to Check**:
- Role is 'dispatcher' (lowercase) ?
- Hex encoding shows correct ASCII values ?
- Response status is 200 OK ?
- Response confirms newRole='dispatcher' ?

**?? Hex Decoding Reference**:
```
64 = 'd'
69 = 'i'
73 = 's'
70 = 'p'
61 = 'a'
74 = 't'
63 = 'c'
68 = 'h'
65 = 'e'
72 = 'r'
```

---

### Phase 5: Reloading Users

**Browser Console**:
```
[UserManagement.UI] UpdateUserRoleAsync returned - Success: True
[UserManagement.UI] SUCCESS: Role updated for charlie to dispatcher.
[UserManagement.UI] Reloading users...
[UserManagement.UI] ========== LOAD USERS START ==========
[UserManagement.UI] Calling UserService.GetUsersAsync()
[UserManagement.UI] Received 12 users
[UserManagement.UI]   - charlie: Role='dispatcher', Roles.Count=1  ? CHECK THIS!
[UserManagement.UI] ========== LOAD USERS COMPLETE ==========
[UserManagement.UI] Users reloaded, closing modal
[UserManagement.UI] ========== ROLE CHANGE COMPLETE ==========
```

**Server Console**:
```
[UserManagement] ========== FETCHING USERS ==========
[UserManagement] Loaded 12 users
[UserManagement]   - charlie: role=dispatcher (property), Role property=dispatcher  ? CHECK THIS!
[UserManagement] ========== USERS LOADED ==========
```

**? What to Check**:
- Charlie's role shows 'dispatcher' ?
- NOT 'driver' anymore
- Table should update to show "Dispatcher"

---

## ?? Troubleshooting Scenarios

### Scenario 1: Role Reverts to Original

**Symptoms**:
```
Before: charlie: Role='driver'
After:  charlie: Role='driver'  ? Still driver!
```

**Check Logs For**:
1. **AuthServer Response**:
   - Does it say `"newRole":"dispatcher"`? 
   - Or does it say already has role?

2. **Database Persistence**:
   - AuthServer might not be saving to database
   - Check AuthServer logs for SQL errors

3. **Cache Issue**:
   - AuthServer might be caching user roles
   - Try restarting AuthServer

**Action**: Copy server console logs showing:
- Request body
- Response status
- Response body
- Then contact AuthServer team

---

### Scenario 2: Wrong Role Value Sent

**Symptoms**:
```
[UserManagement] Role value: Dispatcher  ? Capitalized!
[UserManagement] Role value (hex): 44-69-73-...  ? 44 = 'D' (capital)
```

**Check Logs For**:
1. `SelectSingleRole` output
2. `availableRoles` list contents
3. Role normalization results

**Action**: If still seeing capitals, check:
- Is browser cache cleared?
- Is old version of JS running?
- Hard refresh: Ctrl+Shift+R

---

### Scenario 3: API Returns 400 Bad Request

**Symptoms**:
```
[UserManagement] Response status: 400 (Bad Request)
[UserManagement] Response body: {"error":"Invalid role 'dispatcher'..."}
```

**Possible Causes**:
1. AuthServer has different valid role names
2. Role name has extra whitespace
3. JSON serialization issue

**Action**: Check hex encoding:
```
Expected: 64-69-73-70-61-74-63-68-65-72
Got:      64-69-73-70-61-74-63-68-65-72-20  ? Extra 20 (space)!
```

---

### Scenario 4: Modal Shows Wrong Current Role

**Symptoms**:
```
Charlie is driver in table
Modal shows: Selected roles: 'admin'  ? Wrong!
```

**Check Logs For**:
1. `ShowRoleChangeModal` current role
2. `NormalizeRoleSelection` input/output
3. `user.Roles` list contents

**Action**: Check if:
- User object has stale data
- Role property vs Roles list mismatch
- Normalization failing

---

## ?? Data Collection Checklist

When reporting issues, collect:

### Browser Console Logs
- [ ] Full console output from page load
- [ ] Modal open logs
- [ ] Role selection logs
- [ ] Confirmation flow logs
- [ ] User reload logs

### Server Console Logs
- [ ] User fetch logs (initial)
- [ ] Role update start logs
- [ ] Request details (URL, body, hex)
- [ ] Response details (status, body)
- [ ] User fetch logs (after update)

### Screenshots
- [ ] User table before change
- [ ] Role modal with selection
- [ ] Success toast message
- [ ] User table after change (before refresh)
- [ ] User table after page refresh

### Test Details
- [ ] Username being modified
- [ ] Original role
- [ ] Target role
- [ ] Timestamp of test
- [ ] AuthServer status (running/not running)

---

## ?? Quick Commands

### Clear Browser Cache
```
Chrome: Ctrl+Shift+Delete ? Clear browsing data
Edge: Same as Chrome
Firefox: Ctrl+Shift+Delete
```

### Hard Refresh Page
```
Ctrl+Shift+R  (Windows/Linux)
Cmd+Shift+R   (Mac)
```

### Restart AdminPortal
```powershell
# Stop:
Ctrl+C

# Wait 3 seconds

# Start:
dotnet run --no-build  # Faster if no code changes
# OR
dotnet run             # Full rebuild
```

### View All Logs Together
```powershell
# Open second terminal
# Run this to tail logs:
dotnet run | Select-String "UserManagement"
```

---

## ?? Success Criteria

**Test is successful when**:

1. ? Logs show role change request sent
2. ? AuthServer returns 200 OK
3. ? Response confirms `newRole: 'dispatcher'`
4. ? User reload shows charlie with 'dispatcher'
5. ? Table displays "Dispatcher"
6. ? Page refresh maintains "Dispatcher"
7. ? Opening modal again shows dispatcher selected

**Test reveals issue when**:

1. ? Role reverts to original after reload
2. ? AuthServer returns error status
3. ? Response body shows "already has role"
4. ? User reload shows old role
5. ? Table doesn't update
6. ? Page refresh loses change

---

## ?? Example Log Collection

### Format for Reporting

```markdown
## Role Change Test Results

**Test Date**: 2026-02-05 19:00:00
**User**: charlie
**From Role**: driver
**To Role**: dispatcher

### Browser Console (Relevant Excerpts)

[Paste SelectSingleRole logs]
[Paste ConfirmRoleChange logs]
[Paste LoadUsers AFTER logs]

### Server Console (Relevant Excerpts)

[Paste ROLE UPDATE START section]
[Paste Request/Response details]
[Paste USERS LOADED section after update]

### Observed Behavior

- [ ] Role changed in table: YES/NO
- [ ] Success toast appeared: YES/NO
- [ ] Modal closed: YES/NO
- [ ] Page refresh maintained change: YES/NO

### Issue

[Describe what didn't work as expected]
```

---

## ?? Next Steps After Testing

**If role change works**:
1. ? Mark User Management test as PASSED
2. ? Update test results
3. ? Remove excessive debug logging (optional)
4. ? Proceed with production testing

**If role change fails**:
1. ? Collect logs using checklist above
2. ? Identify failure point from logs
3. ? Check AuthServer logs if available
4. ? Contact AuthServer team with evidence
5. ? Document issue in bug tracker

---

**Status**: ? **Enhanced Debugging Ready**  
**Build Status**: ? **Rebuild Required** (stop app first)  
**Testing**: ?? **Ready to Investigate**

---

*These comprehensive logs will help us pinpoint exactly where the role change process breaks down!* ???
