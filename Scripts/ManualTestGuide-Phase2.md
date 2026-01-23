# Phase 2 Manual Testing Guide

**Document Type**: Testing Guide  
**Phase**: Phase 2 - Role-Aware UI & Credential Management  
**Date**: January 18, 2026  
**Status**: ? Ready for Testing

---

## ?? Overview

This guide provides step-by-step manual testing procedures for Phase 2 of the Bellwood AdminPortal. Use this guide in conjunction with the automated PowerShell test scripts.

**Prerequisites**:
- ? AuthServer running on `https://localhost:5001`
- ? AdminAPI running on `https://localhost:5206`
- ? AdminPortal running on `https://localhost:7257`
- ? Test accounts available: alice, bob, diana, charlie

---

## ?? Test Scenarios

### Test Scenario 1: JWT Decoding & Role Extraction

**Objective**: Verify JWT tokens are decoded correctly and role/userId claims are extracted

**Steps**:

1. **Open AdminPortal** in browser: `https://localhost:7257`

2. **Login as Admin**:
   - Username: `alice`
   - Password: `password`

3. **Open Browser Developer Console** (F12)

4. **Check Console Logs**:
   - Look for: `[AuthStateProvider] Decoded - User: alice, Role: admin, UserId: <guid>`
   - ? **PASS**: Log shows correct username, role, and userId
   - ? **FAIL**: Missing log or incorrect values

5. **Verify Navigation Header**:
   - Username "alice" displayed
   - Role badge "admin" with RED background
   - ? **PASS**: Username and role badge visible
   - ? **FAIL**: Missing or incorrect

6. **Logout and Login as Dispatcher**:
   - Username: `diana`
   - Password: `password`

7. **Check Console Logs Again**:
   - Look for: `[AuthStateProvider] Decoded - User: diana, Role: dispatcher, UserId: <guid>`
   - ? **PASS**: Log shows correct dispatcher role
   - ? **FAIL**: Missing or incorrect

8. **Verify Navigation Header**:
   - Username "diana" displayed
   - Role badge "dispatcher" with BLUE background
   - ? **PASS**: Correct username and badge
   - ? **FAIL**: Incorrect or missing

---

### Test Scenario 2: Token Refresh

**Objective**: Verify automatic token refresh starts and works

**Steps**:

1. **Login as Admin** (alice/password)

2. **Navigate to Main Page** (`/main`)

3. **Open Browser Console** (F12)

4. **Look for Token Refresh Logs**:
   - `[Main] Token auto-refresh started`
   - `[TokenRefresh] Token will be refreshed in XX minutes`
   - ? **PASS**: Both logs present
   - ? **FAIL**: Logs missing

5. **Wait 1-2 minutes and refresh the page**

6. **Verify Console Still Shows**:
   - Auto-refresh restarted on page load
   - ? **PASS**: Refresh timer restarts
   - ? **FAIL**: No refresh activity

**Note**: Full token refresh (55 minutes) testing is optional due to time required.

---

### Test Scenario 3: Role-Based Navigation (Admin)

**Objective**: Verify admin users see all navigation items

**Steps**:

1. **Login as Admin** (alice/password)

2. **Verify Navigation Sidebar** shows:
   - ? Home
   - ? Bookings
   - ? Live Tracking
   - ? Quotes
   - ? Affiliates
   - ? **--- ADMINISTRATION ---** (divider)
   - ? User Management
   - ? OAuth Credentials
   - ? Billing Reports

3. **Click Each Navigation Item**:
   - All pages should load successfully
   - No 403 or access denied errors
   - ? **PASS**: All pages accessible
   - ? **FAIL**: Any page blocked

4. **Verify Admin Pages Load**:
   - User Management: Shows user list with filter dropdown
   - OAuth Credentials: Shows "Coming Soon" placeholder
   - Billing Reports: Shows "Coming Soon" placeholder
   - ? **PASS**: All admin pages load
   - ? **FAIL**: Any page fails to load

---

### Test Scenario 4: Role-Based Navigation (Dispatcher)

**Objective**: Verify dispatchers see only operational items (no admin section)

**Steps**:

1. **Logout from admin account**

2. **Login as Dispatcher** (diana/password)

3. **Verify Navigation Sidebar** shows:
   - ? Home
   - ? Bookings
   - ? Live Tracking
   - ? Quotes
   - ? Affiliates

4. **Verify Navigation Does NOT Show**:
   - ? ADMINISTRATION divider
   - ? User Management
   - ? OAuth Credentials
   - ? Billing Reports

5. **Result**:
   - ? **PASS**: Only operational items visible, no admin section
   - ? **FAIL**: Admin section visible to dispatcher

---

### Test Scenario 5: Direct URL Access Control (Dispatcher)

**Objective**: Verify dispatchers cannot access admin pages via direct URL

**While logged in as dispatcher (diana)**:

1. **Try to access User Management**:
   - Navigate to: `https://localhost:7257/admin/users`
   - Expected: Page blocked or error message
   - ? **PASS**: Access denied
   - ? **FAIL**: Page loads

2. **Try to access OAuth Credentials**:
   - Navigate to: `https://localhost:7257/admin/credentials`
   - Expected: Page blocked or error message
   - ? **PASS**: Access denied
   - ? **FAIL**: Page loads

3. **Try to access Billing Reports**:
   - Navigate to: `https://localhost:7257/admin/billing`
   - Expected: Page blocked or error message
   - ? **PASS**: Access denied
   - ? **FAIL**: Page loads

---

### Test Scenario 6: User Management

**Objective**: Verify user management functionality works correctly

**Steps**:

1. **Login as Admin** (alice/password)

2. **Navigate to User Management** (`/admin/users`)

3. **Verify User List Displays**:
   - Should show at least: alice, bob, diana, charlie
   - Each user shows: username, email, role badge, status
   - ? **PASS**: User list displays correctly
   - ? **FAIL**: Empty list or missing users

4. **Test Role Filter**:
   - Select "Dispatchers" from filter dropdown
   - List should show only diana
   - ? **PASS**: Filter works
   - ? **FAIL**: Filter not working

5. **Change User Role**:
   - Find user "charlie" (driver)
   - Click "Change Role" button
   - Modal dialog appears
   - Select role: "dispatcher"
   - Click "Update Role"
   - Success message appears
   - ? **PASS**: Role changed successfully
   - ? **FAIL**: Error or no change

6. **Verify Role Change**:
   - Refresh the page
   - charlie's role badge should show "dispatcher"
   - ? **PASS**: Role persisted
   - ? **FAIL**: Role reverted

7. **Change Role Back**:
   - Change charlie back to "driver"
   - Verify success
   - ? **PASS**: Role changed back
   - ? **FAIL**: Cannot change role

---

### Test Scenario 7: User Management 403 (Dispatcher)

**Objective**: Verify dispatchers cannot access user management

**Steps**:

1. **Logout and Login as Dispatcher** (diana/password)

2. **Try to Navigate to User Management**:
   - Direct URL: `https://localhost:7257/admin/users`
   - Expected: Access denied or page blocked
   - ? **PASS**: 403 or access denied
   - ? **FAIL**: Page loads

3. **Verify Error Message** (if shown):
   - Should be user-friendly
   - Should NOT show raw exception details
   - ? **PASS**: Friendly error message
   - ? **FAIL**: Technical error exposed

---

### Test Scenario 8: 403 Error Handling (API Level)

**Objective**: Verify API 403 errors display user-friendly messages

**Note**: This test assumes AdminAPI enforces role restrictions. If dispatcher role has full operational access, this test may not trigger 403 errors.

**Steps**:

1. **Login as Dispatcher** (diana/password)

2. **Navigate to Various Pages**:
   - Bookings
   - Quotes
   - Affiliates
   - Live Tracking

3. **If 403 Occurs**:
   - Verify error message is user-friendly
   - Example: "Access denied. You don't have permission to view these records."
   - No raw HTTP status codes or stack traces shown
   - ? **PASS**: Friendly error message
   - ? **FAIL**: Technical error exposed

4. **If No 403 Occurs**:
   - Dispatcher has operational access (as designed)
   - Pages load normally
   - ? **PASS**: Expected behavior
   - ? **FAIL**: Unexpected errors

---

### Test Scenario 9: OAuth Credentials Page (Placeholder)

**Objective**: Verify OAuth Credentials placeholder page displays correctly

**Steps**:

1. **Login as Admin** (alice/password)

2. **Navigate to OAuth Credentials** (`/admin/credentials`)

3. **Verify Page Displays**:
   - "Coming Soon" message
   - Description of planned features
   - Placeholder UI preview (disabled inputs)
   - Developer notice about required API endpoints
   - ? **PASS**: Placeholder displays correctly
   - ? **FAIL**: Page error or missing content

---

### Test Scenario 10: Billing Reports Page (Placeholder)

**Objective**: Verify Billing Reports placeholder page displays correctly

**Steps**:

1. **Login as Admin** (alice/password)

2. **Navigate to Billing Reports** (`/admin/billing`)

3. **Verify Page Displays**:
   - "Coming Soon" message
   - Description of planned features
   - Placeholder dashboard with mock statistics
   - Mock report generation form (disabled)
   - Developer notice about required API endpoints
   - ? **PASS**: Placeholder displays correctly
   - ? **FAIL**: Page error or missing content

---

## ? Test Results Checklist

Use this checklist to track test results:

### JWT Decoding & Role Extraction
- [ ] JWT contains role claim (admin)
- [ ] JWT contains role claim (dispatcher)
- [ ] JWT contains userId claim
- [ ] JWT contains username (sub) claim
- [ ] Navigation header shows username
- [ ] Navigation header shows role badge (correct color)

### Token Refresh
- [ ] Auto-refresh starts on login
- [ ] Auto-refresh log shows timing
- [ ] Refresh token returned on login

### Role-Based Navigation
- [ ] Admin sees all navigation items
- [ ] Admin sees ADMINISTRATION section
- [ ] Dispatcher sees operational items only
- [ ] Dispatcher does NOT see admin section
- [ ] All admin pages accessible to admin
- [ ] All admin pages blocked for dispatcher

### User Management
- [ ] User list displays correctly
- [ ] Role filter works
- [ ] Change user role succeeds
- [ ] Role change persists after refresh
- [ ] Dispatcher blocked from user management (403)
- [ ] Friendly error message on 403

### Placeholder Pages
- [ ] OAuth Credentials placeholder displays
- [ ] Billing Reports placeholder displays
- [ ] Both pages admin-only

### 403 Error Handling
- [ ] All 403 errors show friendly messages
- [ ] No raw error details exposed to users

---

## ?? Issue Reporting

If any test fails, please document:

1. **Test Scenario**: Which test failed
2. **Expected Result**: What should have happened
3. **Actual Result**: What actually happened
4. **Steps to Reproduce**: How to trigger the issue
5. **Screenshots**: Browser console logs or error messages
6. **User Role**: Which user account (alice, diana, etc.)

---

## ?? Support

For issues or questions:
- Check browser console for error logs
- Check AdminPortal application logs
- Review `Docs/32-Troubleshooting.md`
- Contact development team

---

**Last Updated**: January 18, 2026  
**Test Version**: Phase 2 Complete

---

*Happy Testing! ??*
