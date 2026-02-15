# Audit Log Clear Feature - Manual Testing Guide

**Feature**: Admin-only Audit Log Statistics and Clear functionality  
**Date**: February 10, 2026  
**Status**: ? Ready for Alpha Testing  
**Document Type**: Manual Test Procedures

---

## ?? Overview

The Audit Log Clear feature allows administrators to:
- View statistics about audit logs (count, date range, storage size)
- Safely clear all audit logs with typed confirmation
- Export logs before clearing for compliance

**Security**: Admin-only feature (403 Forbidden for non-admin users)

---

## ?? Prerequisites

Before testing, ensure:

- ? **AdminAPI running** on `https://localhost:5206`
- ? **AuthServer running** on `https://localhost:5001`
- ? **AdminPortal running** on `https://localhost:7257`
- ? **Audit logs exist** in database (create some test data if needed)
- ? **Admin user credentials** available (e.g., `alice` / `password`)
- ? **Dispatcher user credentials** available (e.g., `diana` / `password`)

---

## ?? Test Scenarios

### Test 1: View Audit Log Statistics (Admin)

**Purpose**: Verify admin can see audit log statistics

**Steps**:
1. Login to AdminPortal as `alice` (admin)
2. Navigate to **Admin ? Audit Logs** (`/admin/audit-logs`)
3. Observe the statistics card at the top

**Expected Results**:
- ? Stats card displays with 4 metrics:
  - **Total Log Entries**: Shows count (e.g., "1,234")
  - **Oldest Entry**: Shows date (e.g., "01/15/2026")
  - **Newest Entry**: Shows date (e.g., "02/10/2026")
  - **Storage Used**: Shows size (e.g., "2.5 MB" or "N/A")
- ? All values are correctly formatted
- ? No errors in console

**Pass Criteria**:
- ? Stats card renders properly
- ? Values match expected data

---

### Test 2: Access Denied for Non-Admin (Dispatcher)

**Purpose**: Verify dispatchers cannot access audit log management

**Steps**:
1. Logout if logged in
2. Login as `diana` (dispatcher)
3. Navigate to **Admin ? Audit Logs** (`/admin/audit-logs`)

**Expected Results**:
- ? One of the following occurs:
  - Redirected to login page
  - 403 Access Denied error displayed
  - Toast error: "Access denied. Admin role required."
- ? Stats card NOT visible
- ? Clear button NOT visible

**Pass Criteria**:
- ? Dispatcher cannot view audit log management page
- ? Authorization properly enforced

---

### Test 3: Clear Audit Logs - Show Modal

**Purpose**: Verify clear confirmation modal displays properly

**Steps**:
1. Login as `alice` (admin)
2. Navigate to Audit Logs page
3. Click **"Clear Audit Logs"** button (red button, top-right)

**Expected Results**:
- ? Modal appears with:
  - ? Red header: "Clear Audit Logs"
  - ?? Warning icon and text
  - Total count displayed: "ALL X,XXX entries"
  - List of what will be deleted
  - List of pre-deletion checklist
  - Input field with placeholder: "Type CLEAR to confirm"
  - **Cancel** button (gray)
  - **Delete All Audit Logs** button (red, disabled initially)

**Pass Criteria**:
- ? Modal renders correctly
- ? Total count matches stats card
- ? Delete button is disabled initially

---

### Test 4: Clear Audit Logs - Wrong Confirmation

**Purpose**: Verify confirmation validation works

**Steps**:
1. Open clear modal (Test 3)
2. Type `clear` (lowercase) in confirmation field
3. Observe delete button state

**Expected Results**:
- ? Delete button remains **disabled** (case-sensitive)
- ? Input field does not show green checkmark

**Alternative Tests**:
- Type `CLEA` ? Button disabled
- Type `CLEARR` ? Button disabled
- Type ` CLEAR ` (spaces) ? Button disabled

**Pass Criteria**:
- ? Only exact "CLEAR" enables button

---

### Test 5: Clear Audit Logs - Cancel Operation

**Purpose**: Verify cancel works without changes

**Steps**:
1. Open clear modal
2. Type `CLEAR` in confirmation field
3. Click **"Cancel"** button

**Expected Results**:
- ? Modal closes
- ? Audit logs **NOT** deleted (stats unchanged)
- ? No error messages
- ? Page remains functional

**Pass Criteria**:
- ? Cancel aborts operation safely

---

### Test 6: Clear Audit Logs - Successful Clear

**Purpose**: Verify clear operation works correctly

?? **WARNING**: This will delete all audit logs! Export first if needed.

**Pre-Steps**:
1. **EXPORT AUDIT LOGS** using "Export to CSV" button
2. Save CSV file for verification
3. Note the total count from stats card (e.g., 1,234)

**Steps**:
1. Click **"Clear Audit Logs"** button
2. Type **exactly** `CLEAR` in confirmation field
3. Observe delete button becomes enabled (turns solid red)
4. Click **"Delete All Audit Logs"** button
5. Wait for operation to complete

**Expected Results**:
- ? Button shows spinner: "Clearing..."
- ? After completion:
  - ? Success toast: "Successfully deleted X,XXX audit log entries."
  - ? Modal closes automatically
  - ? Stats card updates:
    - Total Log Entries: **0**
    - Oldest Entry: **N/A**
    - Newest Entry: **N/A**
    - Storage Used: **N/A**
  - ? Audit logs table shows: "No audit logs found"
  - ? Console logs: `[AuditLogs] Cleared X logs`

**Pass Criteria**:
- ? All logs deleted
- ? Stats reset to zero
- ? Deleted count matches pre-clear total
- ? No errors

---

### Test 7: Clear Audit Logs - Error Handling

**Purpose**: Verify graceful error handling

**Pre-Steps**: Stop AdminAPI to simulate failure

**Steps**:
1. Stop AdminAPI service
2. In AdminPortal, open clear modal
3. Type `CLEAR` and click delete button

**Expected Results**:
- ? Error toast appears: "Error clearing audit logs: ..."
- ? Modal remains open (so user can retry)
- ? Delete button re-enables after error
- ? Logs unchanged (stats still show original count)

**Post-Steps**: Restart AdminAPI

**Pass Criteria**:
- ? Errors handled gracefully
- ? No crashes or blank screens

---

### Test 8: Clear Audit Logs - 403 Forbidden

**Purpose**: Verify API-level authorization

**Pre-Steps**: This test requires AdminAPI to reject the request (should happen naturally for non-admin)

**Steps**:
1. Attempt to clear logs as dispatcher (if UI allows)
2. OR: Use browser DevTools to manually call the endpoint

**Expected Results**:
- ? 403 Forbidden error from API
- ? Error toast: "Access denied. Admin role required to clear audit logs."
- ? Logs **NOT** deleted

**Pass Criteria**:
- ? Authorization enforced at API level
- ? User-friendly error message

---

### Test 9: Verify Audit Log of Clear Action

**Purpose**: Confirm clear action is itself logged (if implemented)

**Steps**:
1. Clear audit logs (Test 6)
2. Create a new test booking or quote to generate a log
3. Refresh audit logs page

**Expected Results**:
- ? New log entry visible
- ? (Optional) Log entry for clear action:
  - Action: `AuditLog.Cleared` or similar
  - Username: `alice`
  - Details: "Deleted X audit log entries"

**Note**: Clear action logging is optional but recommended

**Pass Criteria**:
- ? System continues logging after clear
- ? (Optional) Clear action is audited

---

## ?? Test Results Summary

**Complete this checklist after testing:**

| Test # | Description | Pass/Fail | Notes |
|--------|-------------|-----------|-------|
| 1 | View Stats (Admin) | [ ] | |
| 2 | Access Denied (Dispatcher) | [ ] | |
| 3 | Show Clear Modal | [ ] | |
| 4 | Wrong Confirmation | [ ] | |
| 5 | Cancel Operation | [ ] | |
| 6 | Successful Clear | [ ] | |
| 7 | Error Handling | [ ] | |
| 8 | 403 Forbidden | [ ] | |
| 9 | Audit of Clear | [ ] | |

**Overall Result**: _____ / 9 tests passed

---

## ?? Known Issues / Limitations

**Current Limitations**:
1. **No Undo**: Once cleared, logs cannot be recovered
2. **No Partial Clear**: Must delete ALL logs (no date range clear)
3. **No Archive**: Logs not automatically archived before deletion
4. **Synchronous Operation**: May timeout for very large datasets (> 1M logs)

**Future Enhancements**:
1. Export before clear prompt
2. Archive to cold storage option
3. Partial clear by date range
4. Async clear with progress indicator

---

## ?? Test Data Setup

If you need to create test audit logs:

### Option 1: Use Application Normally
```
1. Login/logout several times
2. Create a few bookings
3. Update user roles
4. Assign drivers
5. Create quotes
```

### Option 2: Seed Data (if endpoint exists)
```http
POST /api/admin/audit/seed
Authorization: Bearer {admin_token}
```

### Option 3: Direct Database Insert (dev only)
```sql
INSERT INTO AuditLogs (...)
VALUES (...) 
-- Repeat 100+ times
```

---

## ?? Troubleshooting

### Issue: "No stats displayed"

**Symptoms**: Stats card doesn't show or shows N/A

**Possible Causes**:
1. AdminAPI stats endpoint not implemented
2. No audit logs in database
3. API connection failure

**Solutions**:
1. Check browser console for errors
2. Verify AdminAPI `/api/admin/audit/stats` endpoint works
3. Check AdminAPI logs for errors

---

### Issue: "Delete button stays disabled"

**Symptoms**: Button won't enable even when typing CLEAR

**Possible Causes**:
1. Extra spaces in input
2. Autocorrect changed text
3. Copy/paste issue

**Solutions**:
1. Manually type `CLEAR` (all caps)
2. Verify no spaces before/after
3. Check input field value in DevTools

---

### Issue: "Clear succeeds but stats not updated"

**Symptoms**: Success toast shows but count doesn't change

**Possible Causes**:
1. Stats not refreshed after clear
2. Cache issue
3. API returned wrong count

**Solutions**:
1. Hard refresh page (Ctrl+Shift+R)
2. Check browser console for refresh errors
3. Verify API stats endpoint returns updated data

---

## ?? Support

**For Issues**:
1. Check browser console for errors
2. Review AdminAPI logs
3. Verify all services running
4. Contact development team with:
   - Test scenario number
   - Expected vs actual results
   - Console logs
   - Screenshots

---

## ? Sign-Off

**Tester**: ________________  
**Date**: ________________  
**Environment**: [ ] Dev [ ] Staging [ ] Production  
**Build Version**: ________________

**Notes**:
_______________________________________________
_______________________________________________
_______________________________________________

---

**Document Version**: 1.0  
**Last Updated**: February 10, 2026  
**Status**: ? Ready for Alpha Testing

---

*This feature provides administrators with safe, controlled audit log management while maintaining security and preventing accidental data loss through typed confirmation.* ???
