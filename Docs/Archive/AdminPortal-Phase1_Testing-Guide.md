# AdminPortal - Phase 1 Testing Guide

**Initiative:** User-Specific Data Access Enforcement  
**Component:** Admin Portal (Blazor)  
**Phase:** Phase 1 - Testing & Validation  
**Date:** January 11, 2026  
**Status:** ? **READY FOR TESTING**

---

## ?? Overview

This document provides step-by-step testing instructions for verifying Phase 1 implementation in the AdminPortal. Phase 1 focuses on:

1. ? **DTO Updates** - Audit fields added to all API-facing models
2. ? **403 Error Handling** - User-friendly messages for unauthorized access
3. ?? **Audit Display** - Deferred to Phase 2
4. ?? **JWT Decoding** - Deferred to Phase 2

---

## ?? What Changed in Phase 1

### Backend (AdminAPI & AuthServer)
- ? AuthServer now includes `userId` claim in JWT tokens
- ? AdminAPI tracks ownership via `createdByUserId`, `modifiedByUserId`, `modifiedOnUtc`
- ? AdminAPI filters data by user role (admin sees all, booker sees only their records)
- ? AdminAPI returns 403 Forbidden for unauthorized access attempts

### Frontend (AdminPortal)
- ? Added audit fields to all DTOs (not displayed, but ready for Phase 2)
- ? Added 403 error handling with clear user messages
- ?? No role-based UI changes yet (Phase 2)
- ?? No JWT decoding yet (Phase 2)

---

## ?? Test Environment Setup

### Prerequisites

Ensure all three services are running:

| Service | Port | Check URL |
|---------|------|-----------|
| **AuthServer** | 5001 | `https://localhost:5001/health` |
| **AdminAPI** | 5206 | `https://localhost:5206/health` |
| **AdminPortal** | 7257 | `https://localhost:7257` |

### Test Accounts

| Username | Password | Role | Purpose |
|----------|----------|------|---------|
| **alice** | password | admin | Full access to all data |
| **bob** | password | admin | Full access to all data |
| **testbooker** | password | booker | Limited access (if created) |

### Seed Test Data

Run the seeding script to ensure fresh test data:

```powershell
cd C:\Users\sgtad\source\repos\Bellwood.AdminPortal
.\seed-admin-api.ps1
```

**Expected Output**:
- Affiliates created
- Drivers added
- Sample bookings and quotes created

---

## ? Test Scenario 1: Admin User (Full Access)

### Purpose
Verify that admin users can see all data and that new audit fields are received from API.

### Steps

#### 1.1 Login as Admin
1. Navigate to `https://localhost:7257`
2. Should auto-redirect to `/login`
3. Enter credentials:
   - Username: `alice`
   - Password: `password`
4. Click **Login**

**Expected Result**: ? Redirects to `/main` dashboard

#### 1.2 View All Bookings
1. Click **Bookings** in navigation or on dashboard
2. Wait for data to load

**Expected Result**:
- ? All bookings display (should see all bookings in system)
- ? No "Access denied" error messages
- ? Status badges show correctly

**Console Check** (F12 ? Console):
```
[Bookings] Loaded {N} bookings
```

#### 1.3 View Booking Detail
1. Click on any booking card
2. Wait for detail page to load

**Expected Result**:
- ? Booking details display fully
- ? Driver assignment section visible
- ? No "Access denied" error

**Console Check**:
```
[BookingDetail] Booking loaded: {details}
```

#### 1.4 View All Quotes
1. Navigate to `/quotes`
2. Wait for data to load

**Expected Result**:
- ? All quotes display
- ? Can filter by status (Submitted, InReview, Priced, etc.)
- ? No access errors

#### 1.5 Update a Quote
1. Click on any quote card
2. Enter a quoted price (e.g., `150.00`)
3. Select status: "Priced"
4. Click **Save Changes**

**Expected Result**:
- ? Success message: "Quote updated successfully!"
- ? No "Access denied" error

---

## ?? Test Scenario 2: Restricted User Access (403 Handling)

### Purpose
Verify that 403 Forbidden responses are handled gracefully with user-friendly messages.

**Note**: This scenario requires creating a booker user account in AuthServer. If not available, you can simulate 403 responses by temporarily modifying AdminAPI to reject certain requests.

### Option A: Using Booker Account (If Available)

#### 2.1 Create Booker Account (One-Time Setup)
1. Use AuthServer admin endpoints or direct database seeding to create a booker user
2. Username: `testbooker`, Password: `password`, Role: `booker`

#### 2.2 Login as Booker
1. Logout from admin account
2. Login with `testbooker` / `password`

#### 2.3 View Bookings
1. Navigate to `/bookings`
2. Observe what displays

**Expected Result**:
- ? Only bookings created by `testbooker` display (may be empty if none exist)
- ? No bookings created by other users show
- ? No error messages (empty result is valid)

#### 2.4 Attempt Unauthorized Access
1. Manually navigate to `/bookings/{id}` where `{id}` is a booking created by alice
2. Observe the response

**Expected Result**:
- ? Error message displays: "Access denied. You don't have permission to view this booking."
- ? Booking details do NOT display
- ? User-friendly error (not a raw 403 status code)

**Console Check**:
```
[BookingDetail] 403 Forbidden for booking {id}
```

### Option B: Simulated 403 (For Quick Testing)

If booker account is not available, you can test error handling by:

1. **Temporarily break authorization** in AdminAPI:
   - Comment out authorization checks for one endpoint
   - Have it return `403 Forbidden` unconditionally
   
2. **Test in Portal**:
   - Trigger the API call
   - Verify error message displays correctly

**Do NOT deploy this change** - revert after testing!

---

## ?? Test Scenario 3: Audit Fields Verification

### Purpose
Verify that new audit fields are received from API and stored in DTOs (even though not displayed).

### Steps

#### 3.1 Inspect API Response
1. Login as `alice`
2. Navigate to `/bookings`
3. Open browser DevTools (F12)
4. Go to **Network** tab
5. Click **Refresh** button
6. Find the request to `/bookings/list`
7. Click on it and view **Response** tab

**Expected Response Structure**:
```json
[
  {
    "id": "BK-123",
    "passengerName": "John Doe",
    "status": "Confirmed",
    "createdUtc": "2026-01-10T10:00:00Z",
    "createdByUserId": "a1b2c3d4-...",       // ? NEW
    "modifiedByUserId": "x9y8z7w6-...",      // ? NEW
    "modifiedOnUtc": "2026-01-10T15:30:00Z", // ? NEW
    // ... other fields
  }
]
```

**Expected Result**:
- ? Response includes `createdByUserId` (GUID or null for legacy)
- ? Response includes `modifiedByUserId` (GUID or null)
- ? Response includes `modifiedOnUtc` (DateTime or null)

#### 3.2 Verify DTO Deserialization
1. Add a breakpoint or console log in `Bookings.razor` after loading data:
   ```csharp
   allBookings = await client.GetFromJsonAsync<List<BookingListItem>>(...) ?? new();
   
   // Add this logging:
   foreach (var b in allBookings.Take(3))
   {
       Console.WriteLine($"Booking {b.Id}: CreatedBy={b.CreatedByUserId}, ModifiedBy={b.ModifiedByUserId}");
   }
   ```

2. Refresh the page
3. Check console output

**Expected Result**:
```
Booking BK-123: CreatedBy=a1b2c3d4-..., ModifiedBy=x9y8z7w6-...
Booking BK-124: CreatedBy=a1b2c3d4-..., ModifiedBy=null
Booking BK-125: CreatedBy=null, ModifiedBy=null  // Legacy record
```

---

## ?? Test Scenario 4: Error Handling Edge Cases

### Purpose
Verify that various error scenarios are handled gracefully.

### 4.1 Network Failure
1. Login as `alice`
2. Navigate to `/bookings`
3. **Stop the AdminAPI service**
4. Click **Refresh** button

**Expected Result**:
- ? Error message displays (connection error)
- ? No crash or blank page
- ? "Retry" button appears
- ? Console shows error details

5. **Restart AdminAPI**
6. Click **Retry** button

**Expected Result**:
- ? Data loads successfully

### 4.2 Invalid Booking ID
1. Manually navigate to `/bookings/invalid-id-999`

**Expected Result**:
- ? "Booking not found" message displays
- ? No crash
- ? Back button works

### 4.3 Null Audit Fields (Legacy Data)
1. If any legacy bookings exist (created before Phase 1):
2. View them in the list or detail page

**Expected Result**:
- ? Page renders without errors
- ? Null values handled gracefully in DTOs
- ? No "null reference exception" crashes

---

## ?? Test Results Checklist

### DTOs Updated ?

- [ ] `BookingListItem` includes `CreatedByUserId`, `ModifiedByUserId`, `ModifiedOnUtc`
- [ ] `BookingInfo` (detail) includes audit fields
- [ ] `QuoteListItem` includes audit fields
- [ ] `QuoteDetailDto` includes audit fields
- [ ] All DTOs deserialize API responses without errors

### 403 Error Handling ?

- [ ] Bookings list: 403 ? "Access denied" message (not raw error)
- [ ] Booking detail: 403 ? "Access denied" message
- [ ] Quotes list: 403 ? "Access denied" message
- [ ] Quote detail: 403 ? "Access denied" message (caught from QuoteService)
- [ ] Quote update: 403 ? "Access denied" message

### Console Logging ?

- [ ] 403 errors logged to console with context (page name, booking ID, etc.)
- [ ] Error messages user-friendly (not stack traces shown to user)
- [ ] Debug logs helpful for troubleshooting

### User Experience ?

- [ ] Error messages display in red alert boxes
- [ ] Retry buttons available where appropriate
- [ ] No blank pages or crashes
- [ ] Loading spinners show during data fetch
- [ ] Navigation remains functional after errors

---

## ?? Known Issues & Limitations

### Phase 1 Limitations (By Design)

1. **No Role-Based UI** - All admin users see the same interface
   - Dispatcher vs admin distinction not implemented yet
   - Wait for Phase 2

2. **No Audit Display** - Audit fields received but not shown in UI
   - GUIDs not resolved to usernames yet
   - Wait for Phase 2

3. **No JWT Decoding** - Portal doesn't extract userId from token
   - Current auth state just uses hardcoded "Staff" role
   - Wait for Phase 2

### Temporary Workarounds

**If AdminAPI hasn't fully implemented audit fields**:
- Portal will receive `null` values
- No errors will occur (DTOs allow null)
- Wait for AdminAPI deployment

**If AuthServer hasn't added `userId` claim**:
- Portal continues to work (backward compatible)
- Wait for AuthServer deployment

---

## ?? Troubleshooting

### Issue: No data displays, but no error message

**Check**:
1. Are all 3 services running?
2. Check browser Network tab - is API request succeeding?
3. Check API response - does it return empty array `[]`?
4. If empty, data may just not exist - run seed script

### Issue: "Access denied" for admin user

**Check**:
1. Verify user role in AuthServer database (should be "admin")
2. Check JWT token contains `role: admin` claim (view in jwt.io)
3. Check AdminAPI logs - is it filtering data incorrectly?

### Issue: Audit fields are null

**Check**:
1. Check API response in Network tab - are fields present?
2. If present in API but null in portal, check DTO property names match (case-sensitive!)
3. If not present in API, AdminAPI may not have deployed Phase 1 yet

### Issue: 403 error not showing user-friendly message

**Check**:
1. Verify catch block exists in the affected page/service
2. Check console - is the error being logged?
3. Ensure `errorMessage` variable is bound to UI `<div class="alert">` element

---

## ? Success Criteria

**Phase 1 is considered successful when**:

1. ? All DTOs include audit fields without breaking deserialization
2. ? 403 Forbidden responses display user-friendly messages (not raw errors)
3. ? Admin users can access all data
4. ? Restricted users (bookers) see filtered data or access denied errors
5. ? No crashes or blank pages when receiving new API response structure
6. ? Error handling works for network failures, 404s, and 403s
7. ? Console logs provide helpful debugging information

---

## ?? Test Report Template

Use this template to document your test results:

```markdown
## AdminPortal Phase 1 Test Report

**Tester**: {Your Name}  
**Date**: {Date}  
**Environment**: Dev/Staging/Local  
**Build**: {Git Commit Hash}

### Test Results

| Test Scenario | Status | Notes |
|---------------|--------|-------|
| 1.1 Admin Login | ? / ? | |
| 1.2 View All Bookings | ? / ? | |
| 1.3 View Booking Detail | ? / ? | |
| 1.4 View All Quotes | ? / ? | |
| 1.5 Update Quote | ? / ? | |
| 2.4 403 on Unauthorized Booking | ? / ? | |
| 3.1 Audit Fields in API Response | ? / ? | |
| 3.2 DTO Deserialization | ? / ? | |
| 4.1 Network Failure Handling | ? / ? | |
| 4.2 Invalid ID Handling | ? / ? | |

### Issues Found

1. {Description of issue}
   - **Severity**: High/Medium/Low
   - **Steps to Reproduce**: ...
   - **Expected**: ...
   - **Actual**: ...

### Recommendations

- {Any suggestions for improvements}

---

**Overall Status**: ? PASS / ?? PASS WITH ISSUES / ? FAIL
```

---

## ?? Related Documentation

- `Docs/AdminPortal-Phase1_Implementation.md` - Reference for backend changes
- `Docs/Planning-DataAccessEnforcement.md` - Overall platform strategy
- `Docs/Phase1-QuickReference.md` - Quick developer reference (if available)

---

**Status**: ? **READY FOR TESTING**  
**Version**: 1.0  
**Last Updated**: January 11, 2026

---

*This testing guide covers all Phase 1 changes in the AdminPortal. Phase 2 testing will cover role-based UI and JWT decoding.* ???
