# Testing Guide

**Document Type**: Living Document - Testing & Quality Assurance  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready (Phase 2 Complete)

---

## ?? Overview

This guide provides comprehensive testing procedures for the Bellwood AdminPortal, covering manual testing, integration testing, and end-to-end workflows.

**Testing Levels**:
- ?? Unit Testing - Component and service-level tests
- ?? Integration Testing - Multi-component workflows
- ?? End-to-End Testing - Complete user journeys
- ?? Deployment Testing - Production readiness verification

**Target Audience**: QA engineers, developers, DevOps team  
**Prerequisites**: AdminPortal, AdminAPI, and AuthServer running

---

## ?? Testing Philosophy

### Test Pyramid

```
         ???????????????
         ?   E2E Tests ?  ? Few, high-value
         ???????????????
         ? Integration ?  ? More, key workflows
         ?    Tests    ?
         ???????????????
         ? Unit Tests  ?  ? Many, fast
         ???????????????
```

**Principles**:
- ? Test user workflows, not implementation details
- ? Automate repetitive tests when possible
- ? Manual testing for UX and edge cases
- ? Every bug gets a regression test

---

## ?? Quick Start Testing

### Prerequisites

**Required Services**:
- ? **AuthServer** on `https://localhost:5001`
- ? **AdminAPI** on `https://localhost:5206`
- ? **AdminPortal** on `https://localhost:7257`

**Test Accounts**:
- **alice** / **password** - Admin role
- **bob** / **password** - Admin role  
- **diana** / **password** - Dispatcher role
- **charlie** / **password** - Driver role
- **Charlie** (DriverApp) - Driver with UserUID: charlie-uid-001

---

### Start All Services

**Terminal 1 - AuthServer**:
```powershell
cd C:\Users\sgtad\source\repos\BellwoodAuthServer
dotnet run
```
? Wait for: `Now listening on: https://localhost:5001`

**Terminal 2 - AdminAPI**:
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminApi
dotnet run
```
? Wait for: `Now listening on: https://localhost:5206`

**Terminal 3 - AdminPortal**:
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
dotnet run
```
? Wait for: `Now listening on: https://localhost:7257`

---

### Seed Test Data

**Run seeding script**:
```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
.\Scripts\seed-admin-api.ps1
```

**Expected Output**:
```
========================================
Bellwood AdminPortal - Seed Test Data
========================================

? Authentication successful!
? Seeded 3 bookings
? Seeded 3 quotes
? Seeded 2 affiliates with 3 drivers
? Seeded complete!
```

**Verify**:
- 3 bookings (Taylor Reed, Jordan Chen, Derek James)
- 3 quotes (various customers)
- 2 affiliates (Chicago Limo Service, Suburban Chauffeurs)
- 3 drivers (Michael Johnson, Sarah Lee, Robert Brown)

---

## ?? Smoke Tests (5 Minutes)

**Purpose**: Verify basic functionality before detailed testing

### Smoke Test 1: Login & Authentication

**Steps**:
1. Open browser: `https://localhost:7257`
2. **Verify**: Auto-redirects to `/login`
3. Enter: `alice` / `password`
4. Click "Login"
5. **Verify**: Redirects to `/main`
6. **Verify**: Navigation menu appears

**Console Check**:
```
[AuthStateProvider] Initialized
[Login] Login successful
[AuthStateProvider] User authenticated - IsAuthenticated: True
[Login] Navigating to /main
```

**Expected Result**: ? Successfully logged in, dashboard visible

---

### Smoke Test 2: Data Loading

**Steps**:
1. Click "Bookings" in sidebar
2. **Verify**: Bookings list loads (3 items)
3. Click "Quotes" in sidebar
4. **Verify**: Quotes list loads (3 items)
5. Click "Affiliates" in sidebar
6. **Verify**: Affiliates grid loads (2 items)

**Expected Result**: ? All data loads without errors

---

### Smoke Test 3: Real-Time Connection

**Steps**:
1. Click "Live Tracking" in sidebar
2. **Verify**: Map initializes
3. **Verify**: Connection status shows "Connected" (green)

**Console Check**:
```
[DriverTrackingService] Connecting to SignalR hub...
[DriverTrackingService] Connected successfully
```

**Expected Result**: ? SignalR connection established

---

### Smoke Test 4: Logout

**Steps**:
1. Click "Logout" in navigation
2. **Verify**: Redirects to `/login`
3. Try accessing `/bookings` directly
4. **Verify**: Redirects back to `/login`

**Expected Result**: ? Logout clears auth state, routes protected

---

## ?? Feature Testing

### Booking Management Tests

#### Test BK-1: View Bookings List

**Objective**: Verify bookings display correctly

**Steps**:
1. Login as `alice`
2. Navigate to "Bookings"
3. **Verify**:
   - [ ] 3 booking cards display
   - [ ] Each shows: Passenger, Vehicle, Pickup, Status
   - [ ] Status badges color-coded (Requested=gray, Confirmed=blue, Completed=green)
   - [ ] Driver field shows "Unassigned" (yellow) or driver name (green)

**Expected Data**:
- Taylor Reed - SUV - O'Hare FBO - Requested - Unassigned
- Jordan Chen - Sedan - Langham Hotel - Confirmed - Unassigned
- Derek James - S-Class - O'Hare Intl - Completed - Unassigned

---

#### Test BK-2: Filter Bookings

**Steps**:
1. On Bookings page
2. Click "All" filter (default)
3. **Verify**: Shows 3 bookings
4. Click "Requested" filter
5. **Verify**: Shows 1 booking (Taylor Reed)
6. Click "Confirmed" filter
7. **Verify**: Shows 1 booking (Jordan Chen)
8. Click "Completed" filter
9. **Verify**: Shows 1 booking (Derek James)

**Expected Result**: ? Filters work correctly

---

#### Test BK-3: Search Bookings

**Steps**:
1. Enter "Taylor" in search box
2. **Verify**: Shows only Taylor Reed booking
3. Clear search
4. Enter "O'Hare"
5. **Verify**: Shows 2 bookings (both with O'Hare pickup)
6. Enter "SUV"
7. **Verify**: Shows 1 booking (Taylor Reed)

**Expected Result**: ? Search filters by passenger, location, vehicle

---

#### Test BK-4: View Booking Details

**Steps**:
1. Click on Taylor Reed booking card
2. **Verify**: Navigates to `/bookings/{id}`
3. **Verify**: Displays:
   - [ ] Booking ID
   - [ ] Status badge
   - [ ] Passenger name and phone
   - [ ] Booker information
   - [ ] Pickup/dropoff locations
   - [ ] Pickup date and time
   - [ ] Vehicle class
   - [ ] Passenger/luggage count
4. **Verify**: Right column shows "Driver Assignment" section

**Expected Result**: ? All booking details display correctly

---

### Quote Management Tests

#### Test QT-1: View Quotes List

**Steps**:
1. Navigate to "Quotes"
2. **Verify**:
   - [ ] 3 quote cards display
   - [ ] Each shows: Passenger, Trip, Status
   - [ ] Filter buttons (All, Submitted, InReview, Priced, Rejected)

**Expected Result**: ? Quotes list displays

---

#### Test QT-2: View Quote Details

**Steps**:
1. Click on first quote card
2. **Verify**: Navigates to `/quotes/{id}`
3. **Verify**: Left column shows:
   - [ ] Quote ID and status
   - [ ] Booker contact info
   - [ ] Trip details (pickup, dropoff, vehicle, passengers)
4. **Verify**: Right column shows:
   - [ ] Pricing form
   - [ ] Status dropdown
   - [ ] Admin notes textarea

**Expected Result**: ? Quote detail page loads correctly

---

#### Test QT-3: Price a Quote

**Steps**:
1. On quote detail page
2. Enter price: `150.00`
3. Select status: "Priced"
4. Enter admin notes: "Standard rate for airport transfer"
5. Click "Save Changes"
6. **Verify**:
   - [ ] Success message: "? Quote updated successfully!"
   - [ ] Price displays in form
   - [ ] Status shows "Priced"

**API Verification**:
- Check AdminAPI logs for PUT `/quotes/{id}`
- Verify quote saved to storage

**Expected Result**: ? Quote pricing saved

---

#### Test QT-4: Quick Actions

**Steps**:
1. On quote detail page
2. Click "Mark as Priced" quick action button
3. **Verify**: Status changes to "Priced" without editing form
4. Click "Mark In Review"
5. **Verify**: Status changes to "InReview"
6. Click "Reject Quote"
7. **Verify**: Status changes to "Rejected"

**Expected Result**: ? Quick actions work

---

### Driver Assignment Tests

#### Test DA-1: View Affiliates

**Steps**:
1. Navigate to "Affiliates"
2. **Verify**:
   - [ ] Grid shows 2 affiliates
   - [ ] Each card shows: Name, Contact, Phone, Email, City/State
   - [ ] Driver count displayed
   - [ ] View/Edit/Delete buttons visible

**Expected Data**:
- Chicago Limo Service (2 drivers)
- Suburban Chauffeurs (1 driver)

**Expected Result**: ? Affiliates display correctly

---

#### Test DA-2: Create New Affiliate

**Steps**:
1. Click "+ Create Affiliate"
2. Fill form:
   - Name: "Test Limo Service"
   - Point of Contact: "Test Manager"
   - Phone: "555-1234"
   - Email: "test@limo.com"
   - City: "Chicago"
   - State: "IL"
3. Click "Save"
4. **Verify**:
   - [ ] Success message appears
   - [ ] New affiliate card displays
   - [ ] Shows 0 drivers initially

**Bug Fix Verification**: No JSON serialization error (fixed in v1.5)

**Expected Result**: ? Affiliate created successfully

---

#### Test DA-3: Add Driver to Affiliate

**Steps**:
1. Click "View Details" on "Test Limo Service"
2. Click "+ Add Driver"
3. Fill form:
   - Driver Name: "Test Driver"
   - Phone: "555-9999"
4. Click "Save"
5. **Verify**:
   - [ ] Success message: "Driver 'Test Driver' added successfully!"
   - [ ] Driver appears in table
   - [ ] Driver count updates to 1

**Expected Result**: ? Driver added to affiliate

---

#### Test DA-4: Assign Driver to Booking

**Steps**:
1. Navigate to "Bookings"
2. Click on Taylor Reed booking (unassigned)
3. On detail page, expand "Chicago Limo Service"
4. **Verify**: Shows 2 drivers (Michael Johnson, Sarah Lee)
5. Click "Assign" next to Michael Johnson
6. **Verify**:
   - [ ] Success message: "? Driver assigned successfully! Michael Johnson will handle this booking. Affiliate has been notified via email."
   - [ ] Current Driver updates to "?? Michael Johnson" (green)
   - [ ] Status changes to "Scheduled"

**API Verification**:
- Check AdminAPI logs for POST `/bookings/{id}/assign-driver`
- Verify email sent to dispatch@chicagolimo.com

**Expected Result**: ? Driver assigned, email sent

---

#### Test DA-5: Verify Assignment Persists

**Steps**:
1. After assigning driver, click "Back to Bookings"
2. Find Taylor Reed booking in list
3. **Verify**: Shows "Driver: ?? Michael Johnson" (green text)
4. Click booking again
5. **Verify**: Still shows Michael Johnson as assigned

**Expected Result**: ? Assignment persists across navigation

---

#### Test DA-6: Quick-Add Driver During Assignment

**Steps**:
1. Navigate to Jordan Chen booking (unassigned)
2. Expand "Chicago Limo Service"
3. Click "+ Add Driver" (below existing drivers)
4. **Verify**: Inline form appears
5. Fill:
   - Driver Name: "Quick Driver"
   - Phone: "555-FAST"
6. Click "Save"
7. **Verify**: Driver appears in list immediately
8. Click "Assign" on new driver
9. **Verify**: Assignment successful

**Expected Result**: ? Quick-add works during assignment

---

#### Test DA-7: Reassign Driver

**Steps**:
1. Go to Taylor Reed booking (assigned to Michael Johnson)
2. Expand "Chicago Limo Service"
3. Click "Assign" on Sarah Lee
4. **Verify**:
   - [ ] Previous assignment replaced
   - [ ] Current Driver: Sarah Lee
   - [ ] Email sent to affiliate (both drivers notified)

**Expected Result**: ? Reassignment works

---

#### Test DA-8: Delete Affiliate with Cascade Warning

**Steps**:
1. Navigate to "Affiliates"
2. Click "Delete" on "Test Limo Service"
3. **Verify**: Modal appears with warning:
   ```
   ?? Warning: Deleting this affiliate will also delete 
   1 driver(s) associated with it.
   ```
4. Click "Cancel"
5. **Verify**: Modal closes, affiliate not deleted
6. Click "Delete" again
7. Click "Delete" in modal
8. **Verify**:
   - [ ] Success message appears
   - [ ] Affiliate card removed
   - [ ] Driver also deleted

**Expected Result**: ? Cascade delete works with proper warning

---

### Real-Time Tracking Tests

#### Test RT-1: View Live Tracking Map

**Steps**:
1. Navigate to "Live Tracking"
2. **Verify**:
   - [ ] Google Maps initializes
   - [ ] Connection status: "Connected" (green)
   - [ ] Active rides sidebar visible
   - [ ] Message: "No active drivers at this time" (if none tracking)

**Console Check**:
```
[DriverTrackingService] Connecting...
[DriverTrackingService] Connected successfully
[LiveTracking] Map initialized
```

**Expected Result**: ? Map loads, SignalR connected

---

#### Test RT-2: SignalR Connection Status

**Steps**:
1. On Live Tracking page
2. **Verify**: Status badge shows "Connected" (green)
3. Stop AdminAPI (Ctrl+C)
4. Wait 10 seconds
5. **Verify**: Status badge changes to "Disconnected" (red)
6. **Verify**: Message: "Connection lost. Retrying..."
7. Restart AdminAPI
8. Wait 10 seconds
9. **Verify**: Status badge returns to "Connected" (green)

**Expected Result**: ? Auto-reconnection works

---

#### Test RT-3: Polling Fallback

**Steps**:
1. Disable WebSocket in firewall (or use browser that blocks WebSocket)
2. Navigate to Live Tracking
3. **Verify**: Status shows "Disconnected" (polling mode)
4. **Verify**: Map still updates every 15 seconds

**Console Check**:
```
[DriverTrackingService] SignalR connection failed
[DriverTrackingService] Falling back to polling mode
[DriverTrackingService] Polling interval: 15 seconds
```

**Expected Result**: ? Polling fallback works

---

#### Test RT-4: Real-Time Location Update (Integration with DriverApp)

**Prerequisites**: Driver app running with Charlie logged in

**Steps**:
1. Assign Charlie to a booking
2. Open Live Tracking in AdminPortal
3. In DriverApp (Charlie), click "Start Trip"
4. DriverApp sends GPS update
5. **Verify** in AdminPortal:
   - [ ] Driver marker appears on map
   - [ ] Marker shows car icon
   - [ ] Sidebar shows: Charlie - OnRoute - 55 mph
   - [ ] Last update time shows "Just now"

**Console Check (AdminPortal)**:
```
[DriverTrackingService] LocationUpdate received
  RideId: abc123
  Driver: Charlie
  Location: (41.8781, -87.6298)
  Speed: 55 mph
```

**Expected Result**: ? Real-time update displayed instantly

---

#### Test RT-5: Real-Time Status Update

**Prerequisites**: Charlie assigned to booking, tracking active

**Steps**:
1. In DriverApp, change status from "OnRoute" to "Arrived"
2. **Verify** in AdminPortal (Live Tracking):
   - [ ] Status badge updates to "Arrived"
   - [ ] Sidebar shows updated status
   - [ ] Speed shows 0 mph
3. **Verify** in Bookings page:
   - [ ] Status badge updates to "Arrived"
   - [ ] No manual refresh needed

**Expected Result**: ? Multi-page sync works

---

## ?? Security Testing

### Test SEC-1: Unauthenticated Access

**Steps**:
1. Open browser in incognito mode
2. Navigate to `https://localhost:7257/bookings`
3. **Verify**: Redirects to `/login`
4. Try accessing `/quotes`, `/affiliates`, `/tracking`
5. **Verify**: All redirect to `/login`

**Expected Result**: ? Protected routes require authentication

---

### Test SEC-2: Invalid Login

**Steps**:
1. On login page
2. Enter: `alice` / `wrongpassword`
3. Click "Login"
4. **Verify**:
   - [ ] Error message: "Invalid username or password"
   - [ ] Stays on login page
   - [ ] No redirect

**Expected Result**: ? Invalid credentials rejected

---

### Test SEC-3: Token Expiration (Manual)

**Steps**:
1. Login as `alice`
2. Wait for JWT to expire (30 minutes by default)
3. Try to load bookings
4. **Verify**: 401 Unauthorized error
5. **Verify**: Redirects to login

**Note**: Token expiration handling is planned for Phase 2

---

### Test SEC-4: API Key Validation

**Steps**:
1. In browser DevTools, Network tab
2. Navigate to Bookings
3. Find request to `/bookings/list`
4. **Verify**: Headers include:
   - `X-Admin-ApiKey: dev-secret-123`
   - `Authorization: Bearer {token}`

**Modify API Key**:
1. Edit `appsettings.Development.json`
2. Change `ApiKey` to `wrong-key`
3. Restart AdminPortal
4. Try loading bookings
5. **Verify**: 401 Unauthorized error

**Expected Result**: ? API key required, validated

---

## ?? Error Handling Tests

### Test ERR-1: AdminAPI Offline

**Steps**:
1. Stop AdminAPI (Ctrl+C)
2. In AdminPortal, navigate to Bookings
3. **Verify**:
   - [ ] Error message: "Failed to load bookings: [error]"
   - [ ] No blank page
   - [ ] Retry button available
4. Restart AdminAPI
5. Click "Retry" or refresh
6. **Verify**: Data loads successfully

**Expected Result**: ? Graceful error handling

---

### Test ERR-2: Invalid Booking ID

**Steps**:
1. Navigate to `/bookings/invalid-id-123`
2. **Verify**:
   - [ ] Error message: "Booking not found"
   - [ ] Back button available
   - [ ] No crash

**Expected Result**: ? 404 handled gracefully

---

### Test ERR-3: Network Timeout

**Steps**:
1. In AdminAPI, add artificial delay (Thread.Sleep(60000))
2. In AdminPortal, try to load bookings
3. **Verify**:
   - [ ] Loading spinner shows
   - [ ] Eventually times out (30 seconds)
   - [ ] Error message appears
   - [ ] Can retry

**Expected Result**: ? Timeout handled

---

### Test ERR-4: Invalid Form Submission

**Steps**:
1. Navigate to "Affiliates"
2. Click "+ Create Affiliate"
3. Leave Name field empty
4. Click "Save"
5. **Verify**:
   - [ ] Error message: "Please fill in all required fields"
   - [ ] Form doesn't close
   - [ ] No API call made

**Expected Result**: ? Client-side validation works

---

## ?? Performance Testing

### Test PERF-1: Page Load Times

**Measure**:
- Login page: Target < 1s
- Bookings list (100 items): Target < 2s
- Booking detail: Target < 1s
- Live Tracking map: Target < 3s

**Tools**: Browser DevTools Performance tab

**Expected Result**: All pages load within targets

---

### Test PERF-2: SignalR Message Latency

**Measure**:
- Driver sends GPS update
- AdminPortal receives event
- Target: < 1 second

**Method**:
1. Add timestamp in DriverApp when sending
2. Log timestamp in AdminPortal when receiving
3. Calculate difference

**Expected Result**: ? < 1 second latency

---

## ?? Regression Testing

### Critical Regression Tests

After any code change, run these tests:

- [ ] **REG-1**: Login flow works
- [ ] **REG-2**: Bookings list loads
- [ ] **REG-3**: Driver assignment persists
- [ ] **REG-4**: SignalR connection establishes
- [ ] **REG-5**: Quote pricing saves
- [ ] **REG-6**: Affiliate creation works (no JSON error)

**Time Estimate**: 10 minutes

**Frequency**: Before each deployment

---

## ? Test Execution Checklist

### Pre-Test Checklist

- [ ] All 3 services running (AuthServer, AdminAPI, AdminPortal)
- [ ] Test data seeded
- [ ] Browser cache cleared
- [ ] DevTools console open
- [ ] Network tab recording

### Post-Test Checklist

- [ ] All tests passed
- [ ] No console errors
- [ ] No API exceptions in AdminAPI logs
- [ ] Performance within targets
- [ ] Regression tests passed

---

## ?? Bug Report Template

```markdown
## Bug Report

**Title**: [Brief description]

**Severity**: Critical / High / Medium / Low

**Environment**:
- OS: Windows 11
- Browser: Chrome 120.0
- .NET Version: 8.0.1
- Date: YYYY-MM-DD

**Steps to Reproduce**:
1. Login as alice
2. Navigate to Bookings
3. Click on first booking
4. Click "Assign Driver"

**Expected Result**:
Driver should be assigned and success message displayed.

**Actual Result**:
Error message: "Failed to assign driver: Connection timeout"

**Console Logs**:
```
[ERROR] HttpRequestException: Connection timeout
```

**Screenshots**:
[Attach screenshot]

**Additional Context**:
Only happens when AdminAPI is under load.
```

---

## ?? Test Coverage Summary

| Category | Tests | Coverage | Phase |
|----------|-------|----------|-------|
| Authentication | 4 | 100% | Phase 1 |
| **JWT Decoding & Claims** | **5** | **100%** | **Phase 2** ? |
| **Token Refresh** | **3** | **100%** | **Phase 2** ? |
| **User Management** | **7** | **100%** | **Phase 2** ? |
| **Role-Based UI** | **4** | **100%** | **Phase 2** ? |
| **403 Handling** | **3** | **100%** | **Phase 2** ? |
| Bookings | 4 | 100% | Phase 1 |
| Quotes | 4 | 100% | Phase 1 |
| Driver Assignment | 8 | 100% | Phase 1 |
| Real-Time Tracking | 5 | 100% | Phase 1 |
| Security | 4 | 100% | Phase 1 |
| Error Handling | 4 | 100% | Phase 1 |
| **Total** | **55** | **100%** | **Phases 1 & 2** |

### Phase 2 Additions (January 18, 2026)

**New Test Categories**:
- ? JWT Decoding & Claims (5 tests)
- ? Token Refresh (3 tests)
- ? User Management (7 tests)
- ? Role-Based UI (4 tests)
- ? 403 Forbidden Handling (3 tests)

**Total Phase 2 Tests**: 22 tests (15 automated + 7 manual)  
**Success Rate**: 100% ?

---

## ?? Related Documentation

- [User Access Control](13-User-Access-Control.md) - Complete RBAC implementation
- [Security Model](23-Security-Model.md) - Authentication & authorization
- [System Architecture](01-System-Architecture.md) - Understanding components
- [Troubleshooting](32-Troubleshooting.md) - Common issues & fixes
- [Deployment Guide](30-Deployment-Guide.md) - Production testing
- **Phase 2 Test Scripts**: `Scripts/` folder - Automated test suite
- **Manual Test Guide**: `Scripts/ManualTestGuide-Phase2.md` - Step-by-step procedures

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready (Phase 2 Complete)  
**Version**: 3.0

---

*Comprehensive testing ensures reliability and quality. Run these tests before each deployment to maintain production standards. Phase 2 adds enterprise-grade RBAC testing with automated scripts and detailed manual procedures.* ?
