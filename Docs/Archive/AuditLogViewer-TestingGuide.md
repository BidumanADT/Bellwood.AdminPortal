# Audit Log Viewer - Testing Guide

**Document Type**: Testing Procedures  
**Created**: January 20, 2026  
**Status**: Ready for Testing  
**Target**: Phase 3 Audit Log Viewer

---

## 📋 Overview

This guide provides step-by-step procedures for testing the Audit Log Viewer feature.

**Test Duration**: ~30 minutes  
**Tester Role**: Admin user required  
**Prerequisites**: AdminAPI with audit logs populated

---

## 🎯 Prerequisites

### Required Services

**1. AuthServer** (`https://localhost:5001`)
```powershell
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
```
✅ Wait for: `Now listening on: https://localhost:5001`

**2. AdminAPI** (`https://localhost:5206`)
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```
✅ Wait for: `Now listening on: https://localhost:5206`

**3. AdminPortal** (`https://localhost:7257`)
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
dotnet run
```
✅ Wait for: `Now listening on: https://localhost:7257`

---

### Test Accounts

**Admin Account** (for testing):
- Username: `alice`
- Password: `password`
- Role: `admin`

**Dispatcher Account** (for 403 testing):
- Username: `diana`
- Password: `password`
- Role: `dispatcher`

---

### Verify AdminAPI Audit Endpoint

**Test AdminAPI Endpoint**:
```powershell
# Login to get admin token
$loginResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" `
    -Method Post `
    -Body (@{username="alice";password="password"} | ConvertTo-Json) `
    -ContentType "application/json" `
    -SkipCertificateCheck

$token = $loginResponse.accessToken

# Test audit logs endpoint
$auditLogs = Invoke-RestMethod -Uri "https://localhost:5206/api/admin/audit-logs?take=10" `
    -Method Get `
    -Headers @{
        "Authorization" = "Bearer $token"
        "X-Admin-ApiKey" = "dev-secret-123"
    } `
    -SkipCertificateCheck

Write-Host "✅ Audit logs endpoint working!" -ForegroundColor Green
Write-Host "Total logs: $($auditLogs.pagination.total)" -ForegroundColor Cyan
Write-Host "Returned: $($auditLogs.logs.Count)" -ForegroundColor Cyan
```

**Expected Output**:
```
✅ Audit logs endpoint working!
Total logs: 1234
Returned: 10
```

**If Error**: Verify AdminAPI is running and has audit logs populated

---

## 🧪 Test Suite

### Test 1: Load Audit Log Viewer (Default View)

**Objective**: Verify page loads with default filters

**Steps**:
1. Open browser: `https://localhost:7257`
2. Login: `alice` / `password`
3. Navigate to: **Admin → Audit Logs**
4. Wait for page to load

**Expected Results**:
- ✅ Page title: "Audit Logs - Bellwood Admin Portal"
- ✅ Filters section visible
- ✅ Start Date: 30 days ago
- ✅ End Date: Today
- ✅ Action dropdown: "All Actions"
- ✅ Entity dropdown: "All Entities"
- ✅ User field: Empty
- ✅ Logs table visible (if logs exist)
- ✅ Pagination controls (if > 100 logs)
- ✅ "Export to CSV" button visible

**Console Check**:
```
[AuditLogs] Initializing audit log viewer
[AuditLogs] Loading logs - Page: 1
[AuditLog] Querying audit logs - Skip: 0, Take: 100
[AuditLog] Request URL: /api/admin/audit-logs?startDate=...
[AuditLog] Retrieved X logs (Total: Y, Page: 1)
[AuditLogs] Loaded X logs (Total: Y)
```

**Screenshot**: 📸 Save as `test1-default-view.png`

---

### Test 2: Filter by Action Type

**Objective**: Verify action filtering works correctly

**Steps**:
1. On Audit Logs page
2. Click "Action" dropdown
3. Select: **"User.RoleChanged"**
4. Click: **"Apply Filters"**
5. Wait for results

**Expected Results**:
- ✅ Loading spinner appears briefly
- ✅ Table updates with filtered results
- ✅ Only "User.RoleChanged" entries shown
- ✅ Result count updates
- ✅ Pagination resets to page 1 (if applicable)

**Verify Table**:
- All visible entries have Action = "User.RoleChanged"
- Action badges are colored (yellow for role changes)

**Console Check**:
```
[AuditLogs] Loading logs - Page: 1
[AuditLog] Request URL: /api/admin/audit-logs?...&action=User.RoleChanged
[AuditLogs] Loaded X logs (Total: Y)
```

**Screenshot**: 📸 Save as `test2-action-filter.png`

---

### Test 3: Filter by Entity Type

**Objective**: Verify entity type filtering

**Steps**:
1. Click "Clear" button (reset filters)
2. Click "Entity" dropdown
3. Select: **"Booking"**
4. Click: **"Apply Filters"**

**Expected Results**:
- ✅ Only booking-related logs shown
- ✅ Entity Type column shows "Booking" for all entries
- ✅ Count updates appropriately

**Verify**:
- Check 5-10 random entries
- All should have EntityType = "Booking"

**Screenshot**: 📸 Save as `test3-entity-filter.png`

---

### Test 4: Filter by Date Range

**Objective**: Verify date range filtering

**Steps**:
1. Click "Clear" button
2. Set Start Date: **7 days ago** (e.g., 01/12/2026 if today is 01/19)
3. Set End Date: **Today** (01/19/2026)
4. Click: **"Apply Filters"**

**Expected Results**:
- ✅ Only logs from selected date range shown
- ✅ Timestamps fall within range
- ✅ Count may be lower than default (30 days)

**Verify Timestamps**:
- Check first entry timestamp
- Check last entry timestamp
- Both should be within selected range

**Console Check**:
```
[AuditLog] Request URL: /api/admin/audit-logs?startDate=2026-01-12T00:00:00Z&endDate=2026-01-19T23:59:59Z
```

**Screenshot**: 📸 Save as `test4-date-filter.png`

---

### Test 5: Filter by User

**Objective**: Verify user filtering

**Steps**:
1. Click "Clear" button
2. In "User" field, type: **"alice"**
3. Click: **"Apply Filters"**

**Expected Results**:
- ✅ Only logs for user "alice" shown
- ✅ Username column shows "alice" for all entries
- ✅ User Role might be "admin"

**Alternate Test**:
- Try with userId (GUID) instead of username
- Should also work

**Screenshot**: 📸 Save as `test5-user-filter.png`

---

### Test 6: Pagination

**Objective**: Verify pagination works correctly

**Prerequisites**: Ensure > 100 audit logs exist

**Steps**:
1. Click "Clear" button (default view)
2. Verify pagination controls visible
3. Note: "Showing X of Y logs (Page 1 of Z)"
4. Click: **"Next"** button
5. Wait for page 2 to load
6. Click: **"Previous"** button
7. Click specific page number (e.g., "3")

**Expected Results**:
- ✅ "Next" loads page 2 (logs 101-200)
- ✅ "Previous" returns to page 1 (logs 1-100)
- ✅ Direct page click loads that page
- ✅ Loading spinner shows during navigation
- ✅ Table updates with new logs
- ✅ Pagination indicator updates ("Page X of Y")

**Edge Cases**:
- "Previous" disabled on page 1
- "Next" disabled on last page

**Screenshot**: 📸 Save as `test6-pagination.png`

---

### Test 7: CSV Export

**Objective**: Verify CSV export functionality

**Steps**:
1. Click "Clear" button
2. Set a narrow date range (e.g., last 7 days)
3. Click: **"Export to CSV"** button
4. Wait for export to complete

**Expected Results**:
- ✅ Button shows spinner: "Exporting..."
- ✅ Button disabled during export
- ✅ File downloads automatically
- ✅ Filename format: `audit-logs-2026-01-19-HHMMSS.csv`
- ✅ Toast notification: "Exported X audit logs to [filename]"
- ✅ Export completes within 5 seconds

**Verify Downloaded File**:
1. Open CSV in Excel or text editor
2. Check header row:
   ```
   Timestamp,Username,User Role,Action,HTTP Method,Endpoint,Entity Type,Entity ID,Result,IP Address,Details
   ```
3. Check data rows have all fields populated
4. Count rows (should match "Showing X of Y" count)

**Console Check**:
```
[AuditLogs] Exporting to CSV
[AuditLog] Exporting audit logs to CSV
[AuditLog] Exported X logs to CSV
[AuditLogs] CSV exported: audit-logs-2026-01-19-143052.csv
[Toast] success: Exported X audit logs to audit-logs-...
```

**Screenshot**: 📸 Save as `test7-csv-export.png` + Excel preview

---

### Test 8: Empty State

**Objective**: Verify empty state message when no logs found

**Steps**:
1. Set Start Date: **01/01/2020** (very old date)
2. Set End Date: **01/31/2020**
3. Click: **"Apply Filters"**

**Expected Results**:
- ✅ No logs shown in table
- ✅ Message displayed: "No audit logs found for the selected filters. Try adjusting your date range or removing filters."
- ✅ Info icon visible
- ✅ No pagination controls
- ✅ Export button still visible (but would export empty CSV)

**Screenshot**: 📸 Save as `test8-empty-state.png`

---

### Test 9: Clear Filters

**Objective**: Verify clear functionality resets filters

**Steps**:
1. Apply multiple filters:
   - Action: "Booking.Created"
   - Entity: "Booking"
   - User: "alice"
   - Date: Last 7 days
2. Click: **"Clear"** button

**Expected Results**:
- ✅ Start Date: Resets to 30 days ago
- ✅ End Date: Resets to today
- ✅ Action: "All Actions"
- ✅ Entity: "All Entities"
- ✅ User: Empty
- ✅ Logs reload with default filters
- ✅ Pagination resets to page 1

**Screenshot**: 📸 Save as `test9-clear-filters.png`

---

### Test 10: 403 Forbidden (Dispatcher Access)

**Objective**: Verify non-admin users are denied access

**Steps**:
1. Logout alice
2. Login as: **diana** / **password** (dispatcher)
3. Navigate to: `Admin → Audit Logs` (if visible in menu)
4. OR directly access: `https://localhost:7257/admin/audit-logs`

**Expected Results**:
- ✅ One of the following:
  - **Option A**: Menu item not visible (admin section hidden)
  - **Option B**: 403 error page shown
  - **Option C**: Error message: "Access denied. You do not have permission to view audit logs. Admin role required."
- ✅ Toast error notification (red)
- ✅ Logs do NOT display
- ✅ User cannot bypass security

**Console Check**:
```
[AuditLog] Access denied (403 Forbidden)
[AuditLogs] Access denied: Access denied. You do not have permission...
[Toast] danger: Access denied. You do not have permission...
```

**Screenshot**: 📸 Save as `test10-403-forbidden.png`

---

### Test 11: Loading State

**Objective**: Verify loading spinners display correctly

**Steps**:
1. Logout diana, login as alice
2. Navigate to Audit Logs
3. Click "Clear" button
4. Click "Apply Filters"
5. **Quickly** observe the page during API call

**Expected Results**:
- ✅ Loading spinner appears (centered)
- ✅ Message: "Loading audit logs..."
- ✅ Table/content hidden during load
- ✅ Spinner disappears when data loads
- ✅ Smooth transition to loaded state

**Tip**: Use browser DevTools Network tab → Throttle to "Slow 3G" to see spinner more clearly

**Screenshot**: 📸 Save as `test11-loading-state.png`

---

### Test 12: Toast Notifications

**Objective**: Verify toast notifications work for all scenarios

**Test Cases**:

**A. Success Toast (CSV Export)**:
1. Click "Export to CSV"
2. **Expected**: Green toast, auto-dismiss after 3 seconds
3. Message: "Exported X audit logs to [filename]"

**B. Error Toast (Network Failure)**:
1. Stop AdminAPI (Ctrl+C)
2. Click "Apply Filters"
3. **Expected**: Red toast, stays until dismissed
4. Message: "Failed to load audit logs: ..."

**C. Error Toast (403 Forbidden)**:
1. Login as diana (dispatcher)
2. Try to access audit logs
3. **Expected**: Red toast, stays until dismissed
4. Message: "Access denied. You do not have permission..."

**Verify Toast Features**:
- ✅ Positioned top-right
- ✅ Icon matches type (check/error/warning/info)
- ✅ Close button (X) works
- ✅ Auto-dismiss for success (3 seconds)
- ✅ Manual dismiss for errors

**Screenshot**: 📸 Save as `test12-toast-notifications.png`

---

## 📊 Test Results Summary

### Test Results Template

| # | Test | Status | Notes |
|---|------|--------|-------|
| 1 | Load Default View | ⏳ | |
| 2 | Filter by Action | ⏳ | |
| 3 | Filter by Entity | ⏳ | |
| 4 | Filter by Date Range | ⏳ | |
| 5 | Filter by User | ⏳ | |
| 6 | Pagination | ⏳ | |
| 7 | CSV Export | ⏳ | |
| 8 | Empty State | ⏳ | |
| 9 | Clear Filters | ⏳ | |
| 10 | 403 Forbidden | ⏳ | |
| 11 | Loading State | ⏳ | |
| 12 | Toast Notifications | ⏳ | |

**Legend**:
- ⏳ Not Started
- ✅ Pass
- ❌ Fail
- ⚠️ Pass with Issues

---

## 🐛 Issue Reporting Template

If you find issues during testing, document them as follows:

```markdown
### Issue #X: [Brief Description]

**Test**: Test #Y - [Test Name]

**Severity**: Critical / High / Medium / Low

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. Step 3

**Expected Result**:
[What should happen]

**Actual Result**:
[What actually happened]

**Screenshots**:
[Attach screenshot]

**Console Logs**:
```
[Paste relevant console output]
```

**Browser**: Chrome 120 / Firefox 121 / Edge 120

**Environment**:
- OS: Windows 11
- .NET Version: 8.0.1
- Date Tested: 2026-01-19
```

---

## ✅ Acceptance Criteria

**All tests must pass** before marking Phase 3 complete:

- [ ] Default view loads successfully
- [ ] All filter types work correctly
- [ ] Pagination navigates correctly
- [ ] CSV export downloads valid file
- [ ] Empty state displays appropriately
- [ ] Clear button resets all filters
- [ ] 403 Forbidden prevents non-admin access
- [ ] Loading spinners display during async operations
- [ ] Toast notifications appear for success/error
- [ ] No console errors (except expected 403s)
- [ ] Responsive design works on different screen sizes
- [ ] No crashes or unhandled exceptions

---

## 📞 Support

**If Tests Fail**:
1. Check AdminAPI is running and has audit logs
2. Verify JWT token is valid (re-login)
3. Check browser console for errors
4. Review [Troubleshooting Guide](32-Troubleshooting.md)
5. Contact development team with issue report

**AdminAPI Test Data**:
If AdminAPI has no audit logs, you may need to:
1. Perform actions (create bookings, change roles) to generate logs
2. Or seed audit logs in AdminAPI database

---

**Created**: January 19, 2026  
**Status**: Ready for Testing  
**Estimated Time**: 30-45 minutes

---

*Follow this guide step-by-step to thoroughly test the Audit Log Viewer before alpha deployment. Document all results and issues for the development team.* 🧪✨
