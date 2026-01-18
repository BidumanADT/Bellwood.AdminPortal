# User Access Control & RBAC

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 17, 2026  
**Status**: ? Production Ready (Phase 1 Complete) | ?? Phase 2 Planned

---

## ?? Overview

This document describes the **Role-Based Access Control (RBAC)** implementation for the Bellwood AdminPortal, including both the completed Phase 1 (ownership tracking and basic access control) and the planned Phase 2 (role-based UI and full dispatcher restrictions).

**Initiative**: Enforce user-specific data access across Bellwood Global platform  
**Priority**: ?? **CRITICAL** - Required before alpha testing  
**Status**: Phase 1 ? Complete | Phase 2 ?? Planned

**Target Audience**: Developers, security engineers, QA team  
**Prerequisites**: Understanding of JWT authentication, authorization policies, ASP.NET Core Identity

---

## ?? Problem Statement

The Bellwood Global platform initially lacked robust role-based access control and per-user data isolation, creating significant security and privacy risks:

### Critical Issues (Pre-Phase 1)

**Issue 1: No Ownership Tracking**
- Booking and quote records did not track who created them
- No `CreatedByUserId` or `ModifiedByUserId` fields
- Impossible to enforce ownership-based access control

**Issue 2: Broad Data Access**
- Any authenticated user could access any booking or quote
- `GET /bookings/list` returned all bookings regardless of user
- Passengers could see other customers' data
- Drivers could access data beyond their assigned rides

**Issue 3: No Dispatcher Role**
- Only "admin" and "driver" roles existed
- Dispatchers had to use admin credentials
- Dispatchers could see billing information they shouldn't access

**Issue 4: No Audit Trail**
- No tracking of who created or modified records
- No accountability for data changes
- Compliance and security concerns

---

## ??? Solution Architecture

### Two-Phase Implementation

```
Phase 1 (COMPLETE ?)
??? Add audit fields to all DTOs
??? Implement 403 Forbidden error handling
??? Backend: Track ownership (AdminAPI)
??? Backend: Filter data by user role

Phase 2 (PLANNED ??)
??? Decode JWT tokens in portal
??? Role-based UI elements
??? Field masking for dispatchers
??? Audit information display
```

---

## ? Phase 1: Ownership Tracking & Basic Access Control

### Status: **COMPLETE** (January 11, 2026)

Phase 1 establishes the foundation for data access control by adding ownership metadata and implementing basic authorization checks.

### 1.1 Audit Fields Added

All API-facing DTOs now include audit trail fields:

```csharp
// Phase 1: Audit trail fields (added January 2026)
/// <summary>
/// User ID (GUID) of the user who created this record.
/// Null for legacy records created before Phase 1.
/// </summary>
public string? CreatedByUserId { get; set; }

/// <summary>
/// User ID (GUID) of the user who last modified this record.
/// Null if never modified or for legacy records.
/// </summary>
public string? ModifiedByUserId { get; set; }

/// <summary>
/// Timestamp of the last modification to this record.
/// Null if never modified.
/// </summary>
public DateTime? ModifiedOnUtc { get; set; }
```

**Files Modified**:
- `Components/Pages/Bookings.razor` - `BookingListItem` DTO
- `Components/Pages/BookingDetail.razor` - `BookingInfo` DTO
- `Components/Pages/Quotes.razor` - `QuoteListItem` DTO
- `Models/QuoteModels.cs` - `QuoteDetailDto`

**Impact**:
- ? Portal can deserialize AdminAPI Phase 1 responses without errors
- ? Audit data stored (ready for Phase 2 display)
- ? Null values handled gracefully for legacy data
- ? No breaking changes to existing functionality

---

### 1.2 403 Forbidden Error Handling

User-friendly error messages implemented for unauthorized access attempts:

**Before Phase 1**:
```csharp
// Generic error on 403
response.EnsureSuccessStatusCode();  // Throws generic exception
```

**After Phase 1**:
```csharp
// Check for 403 before throwing
if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    errorMessage = "Access denied. You don't have permission to view these records.";
    Console.WriteLine($"[Bookings] 403 Forbidden: {errorMessage}");
    return;
}
```

**Pages Updated**:

| Page | Method | Error Message |
|------|--------|---------------|
| Bookings.razor | LoadBookingsAsync() | "Access denied. You don't have permission to view these records." |
| BookingDetail.razor | LoadBookingAsync() | "Access denied. You don't have permission to view this booking." |
| Quotes.razor | LoadQuotesAsync() | "Access denied. You don't have permission to view these quotes." |
| QuoteDetail.razor | LoadQuoteAsync() | Catches `UnauthorizedAccessException` from service |
| QuoteDetail.razor | SaveQuote() | Catches `UnauthorizedAccessException` from service |

**Services Updated**:
- `QuoteService.GetQuoteAsync()` - Throws `UnauthorizedAccessException` on 403
- `QuoteService.UpdateQuoteAsync()` - Throws `UnauthorizedAccessException` on 403

**User Experience**:
- ? Clear, user-friendly error messages
- ? No raw HTTP status codes shown to users
- ? Errors logged to console for debugging
- ? Retry buttons available where appropriate

---

### 1.3 Backend Changes (AdminAPI)

**AdminAPI Phase 1 Implementation** (completed by AdminAPI team):

**Audit Fields**:
- `createdByUserId` (GUID) - User who created the record
- `modifiedByUserId` (GUID) - User who last modified the record
- `modifiedOnUtc` (DateTime) - Last modification timestamp

**Data Filtering by Role**:
- **Admin users**: See all data (no filtering)
- **Booker/Passenger users**: See only data where `createdByUserId` matches their user ID
- **Driver users**: See only rides where `assignedDriverUid` matches their UID

**403 Forbidden Responses**:
- Returned when user attempts to access data they don't own
- Specific error messages for troubleshooting

**See**: AdminAPI Phase 1 documentation for complete backend implementation details

---

### 1.4 AuthServer Changes

**AuthServer Phase 1 Implementation** (completed by AuthServer team):

**JWT Token Enhancement**:

**Before Phase 1**:
```json
{
  "sub": "alice",
  "uid": "a1b2c3d4-...",
  "role": "admin"
}
```

**After Phase 1**:
```json
{
  "sub": "alice",
  "uid": "a1b2c3d4-...",
  "userId": "a1b2c3d4-...",  // ? NEW
  "role": "admin"
}
```

**Changes**:
- All JWTs now include `userId` claim
- `userId` always contains the Identity GUID
- For drivers with custom UIDs: `uid` is custom, `userId` is the GUID
- Backward compatible (existing tokens work)

**See**: AuthServer Phase 1 documentation for complete details

---

### 1.5 Phase 1 Testing

**Testing Guide Created**: `AdminPortal-Phase1_Testing-Guide.md` (archived)

**Test Scenarios**:
1. Admin user - Full access to all data
2. Restricted user access - 403 handling verification
3. Audit fields verification - API response inspection
4. Error handling edge cases - Network failures, invalid IDs

**Success Criteria** (All Met ?):
- DTOs include audit fields without breaking deserialization
- 403 Forbidden responses display user-friendly messages
- Admin users can access all data
- Restricted users see filtered data or access denied errors
- No crashes with new API response structure
- Error handling works for network failures, 404s, and 403s

**See**: Full testing procedures in archived testing guide

---

## ?? Phase 2: Role-Based UI & Advanced Features

### Status: **PLANNED** (Target: Q1 2026)

Phase 2 builds upon Phase 1 to introduce role-aware UI, field masking, and full audit trail visibility.

### 2.1 New Roles

**Dispatcher Role** (To Be Implemented):

| Field | Value |
|-------|-------|
| Role Name | `dispatcher` |
| Purpose | Operational staff - booking management without billing access |
| Permissions | View/manage bookings, assign drivers, track rides |
| Restrictions | Cannot view billing info, payment details, or financial reports |

**Updated Role Matrix**:

| Role | Users | Access Level | Data Scope |
|------|-------|--------------|------------|
| `admin` | alice, bob | Full system access | All data |
| `dispatcher` | TBD | Operational access | All bookings (billing masked) |
| `booker` | Passengers, concierges | Create bookings/quotes | Own data only |
| `driver` | Active drivers | Assigned rides | Own assigned rides |

---

### 2.2 JWT Decoding (Phase 2)

**Current State** (Phase 1):
```csharp
// Portal doesn't decode JWT - just stores and uses it
await AuthStateProvider.MarkUserAsAuthenticatedAsync(username, token);
```

**Phase 2 Implementation**:
```csharp
using System.IdentityModel.Tokens.Jwt;

// Decode JWT to extract claims
var handler = new JwtSecurityTokenHandler();
var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

var userId = jsonToken?.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
var role = jsonToken?.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
var username = jsonToken?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

// Create ClaimsPrincipal with actual JWT claims
var claims = new List<Claim>
{
    new(ClaimTypes.Name, username ?? "Unknown"),
    new(ClaimTypes.Role, role ?? "User"),
    new("userId", userId ?? ""),
    new("access_token", token)
};
```

**Benefits**:
- Portal can access user's role and userId
- Enable role-based UI logic
- Display "Logged in as {username} ({role})"

**Library**: `System.IdentityModel.Tokens.Jwt` NuGet package

---

### 2.3 Role-Based UI Components

**Planned Components**:

**AdminOnly Component**:
```razor
<!-- AdminOnly.razor -->
<AuthorizeView Roles="admin">
    <Authorized>
        @ChildContent
    </Authorized>
</AuthorizeView>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```

**Usage**:
```razor
<AdminOnly>
    <button @onclick="ViewBilling">View Billing Report</button>
</AdminOnly>
```

**DispatcherOrAdmin Component**:
```razor
<AuthorizeView Roles="admin,dispatcher">
    <Authorized>
        @ChildContent
    </Authorized>
</AuthorizeView>
```

**Usage**:
```razor
<DispatcherOrAdmin>
    <button @onclick="AssignDriver">Assign Driver</button>
</DispatcherOrAdmin>
```

---

### 2.4 Field Masking for Dispatchers

**Scenario**: Dispatcher views booking details

**Admin View** (Full Data):
```json
{
  "bookingId": "BK-123",
  "passengerName": "John Doe",
  "pickupLocation": "O'Hare Airport",
  "quotedPrice": 150.00,              // ? Visible
  "paymentMethod": "Visa *1234",      // ? Visible
  "totalAmount": 165.00,              // ? Visible
  "createdByUserId": "a1b2c3d4-..."
}
```

**Dispatcher View** (Billing Masked):
```json
{
  "bookingId": "BK-123",
  "passengerName": "John Doe",
  "pickupLocation": "O'Hare Airport",
  "quotedPrice": null,                // ? Masked
  "paymentMethod": null,              // ? Masked
  "totalAmount": null,                // ? Masked
  "createdByUserId": "a1b2c3d4-..."
}
```

**Backend Implementation** (AdminAPI Phase 2):
```csharp
// BookingsController.cs
public IActionResult GetBookingDetail(string id)
{
    var booking = _repository.GetById(id);
    var userRole = User.FindFirst("role")?.Value;
    
    if (userRole == "dispatcher")
    {
        // Mask billing fields
        booking.QuotedPrice = null;
        booking.PaymentMethod = null;
        booking.TotalAmount = null;
    }
    
    return Ok(booking);
}
```

**Frontend Implementation** (AdminPortal Phase 2):
```razor
<!-- BookingDetail.razor -->
<AdminOnly>
    <div class="billing-section">
        <h4>Billing Information</h4>
        <p>Quoted Price: @booking.QuotedPrice?.ToString("C")</p>
        <p>Payment Method: @booking.PaymentMethod</p>
        <p>Total Amount: @booking.TotalAmount?.ToString("C")</p>
    </div>
</AdminOnly>
```

---

### 2.5 Audit Information Display

**Current State** (Phase 1):
- Audit fields received but not displayed
- GUIDs stored but not resolved to usernames

**Phase 2 Enhancement**:

**Username Resolution**:
- Add API endpoint: `GET /users/{userId}/display-name`
- Maps GUID ? friendly username
- Called when displaying audit information

**UI Display**:
```razor
<!-- BookingDetail.razor -->
<div class="audit-trail">
    <h5>Audit Trail</h5>
    <p>
        <strong>Created:</strong> 
        @booking.CreatedUtc.ToLocalTime().ToString("g")
        @if (!string.IsNullOrEmpty(booking.CreatedByUserId))
        {
            <text> by @await GetUsername(booking.CreatedByUserId)</text>
        }
    </p>
    @if (booking.ModifiedOnUtc.HasValue)
    {
        <p>
            <strong>Last Modified:</strong>
            @booking.ModifiedOnUtc.Value.ToLocalTime().ToString("g")
            @if (!string.IsNullOrEmpty(booking.ModifiedByUserId))
            {
                <text> by @await GetUsername(booking.ModifiedByUserId)</text>
            }
        </p>
    }
</div>

@code {
    private async Task<string> GetUsername(string userId)
    {
        // Call API to resolve GUID to username
        var response = await HttpClient.GetAsync($"/users/{userId}/display-name");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        return userId; // Fallback to GUID
    }
}
```

---

## ?? Data Access Rules (Complete)

### Access Matrix

| Role | Bookings | Quotes | Drivers | Billing | User Mgmt |
|------|----------|--------|---------|---------|-----------|
| **Admin** | All | All | All | ? Full | ? Full |
| **Dispatcher** | All (billing masked) | All (pricing masked) | All | ? None | ? None |
| **Booker** | Own only | Own only | ? None | ? None | ? None |
| **Driver** | Assigned rides | ? None | ? None | ? None | ? None |

### Ownership Filtering Logic

**Admin Users**:
```sql
-- No filtering - see everything
SELECT * FROM Bookings
```

**Booker/Passenger Users**:
```sql
-- Filter by ownership
SELECT * FROM Bookings
WHERE CreatedByUserId = @currentUserId
```

**Driver Users**:
```sql
-- Filter by assignment
SELECT * FROM Bookings
WHERE AssignedDriverUid = @currentUserUid
```

**Dispatcher Users** (Phase 2):
```sql
-- No filtering (see all bookings for operational needs)
-- But billing fields masked in response DTO
SELECT * FROM Bookings
```

---

## ?? Implementation Timeline

### Phase 1 ? COMPLETE (January 11, 2026)

| Task | Status | Completion Date |
|------|--------|-----------------|
| Add audit fields to DTOs | ? | Jan 11, 2026 |
| Implement 403 error handling | ? | Jan 11, 2026 |
| Create testing guide | ? | Jan 11, 2026 |
| Documentation | ? | Jan 11, 2026 |

**Deliverables**:
- ? All DTOs updated with audit fields
- ? User-friendly 403 error messages
- ? Comprehensive testing guide
- ? Build successful (0 errors)

---

### Phase 2 ?? PLANNED (Q1 2026)

| Task | Priority | Estimated Effort | Status |
|------|----------|------------------|--------|
| Add dispatcher role to AuthServer | ?? Critical | 2 days | Planned |
| Implement JWT decoding in portal | ?? Critical | 3 days | Planned |
| Create role-based UI components | ?? Important | 4 days | Planned |
| Implement field masking (backend) | ?? Critical | 5 days | Planned |
| Add username resolution API | ?? Important | 2 days | Planned |
| Display audit information in UI | ?? Normal | 3 days | Planned |
| Update all pages with role checks | ?? Important | 5 days | Planned |
| Phase 2 testing & documentation | ?? Important | 3 days | Planned |

**Total Estimated Effort**: ~27 days (4-5 weeks with testing)

---

## ?? Testing Strategy

### Phase 1 Testing (Complete)

**Test Accounts**:
- **alice** (admin) - Full access
- **bob** (admin) - Full access
- **testbooker** (booker) - Limited access (if created)

**Test Scenarios**:
1. ? Admin sees all bookings
2. ? Booker sees only own bookings
3. ? 403 error displays user-friendly message
4. ? Audit fields in API response
5. ? Network failure handling

**See**: Archived testing guide for detailed procedures

---

### Phase 2 Testing (Planned)

**Additional Test Accounts**:
- **dispatcher-test** (dispatcher) - Operational access, billing masked

**New Test Scenarios**:
1. Dispatcher cannot see billing fields
2. Dispatcher cannot access user management
3. Role-based UI elements hide correctly
4. Audit information displays with usernames
5. JWT decoding works correctly
6. Username resolution API functions

**Testing Checklist**:
- [ ] Dispatcher role created in AuthServer
- [ ] JWT includes dispatcher role claim
- [ ] Billing fields masked for dispatcher
- [ ] UI elements hidden based on role
- [ ] Audit trail displays correctly
- [ ] Username resolution works
- [ ] No access to restricted features

---

## ?? Security Considerations

### Risks Addressed (Phase 1)

**? Fixed: Passengers Viewing Others' Data**
- Bookings/quotes now filtered by `createdByUserId`
- 403 Forbidden returned for unauthorized access

**? Fixed: No Audit Trail**
- All records track creator and modifier
- Timestamps recorded for all changes

**? Fixed: Drivers Accessing Passenger Data**
- Driver endpoints filter by `assignedDriverUid`
- Cross-role access blocked

---

### Risks to Address (Phase 2)

**?? Pending: Dispatchers Seeing Billing**
- **Solution**: Field masking in Phase 2
- Backend omits billing fields for dispatcher role

**?? Pending: No Role-Based UI**
- **Solution**: Role-based components in Phase 2
- UI elements hidden based on JWT role claim

**?? Pending: Least Privilege Violations**
- **Solution**: Complete RBAC matrix enforcement
- Each role has minimum necessary permissions

---

## ?? Related Documentation

- [System Architecture](01-System-Architecture.md) - Overall system design
- [Security Model](23-Security-Model.md) - Authentication & authorization details
- [Testing Guide](02-Testing-Guide.md) - General testing procedures
- [API Reference](20-API-Reference.md) - AdminAPI endpoints used
- [Troubleshooting](32-Troubleshooting.md) - Common issues & solutions

### Archived Documentation

- `Archive/AdminPortal-Phase1_Implementation-Summary.md` - Phase 1 details
- `Archive/AdminPortal-Phase1_Testing-Guide.md` - Phase 1 testing procedures
- `Archive/AdminPortal-Phase1_Quick-Reference.md` - Quick reference
- `Archive/AdminPortal-Phase1_Implementation.md` - Backend reference
- `Archive/Planning-DataAccessEnforcement.md` - Original planning document

---

## ?? Support & Questions

**Phase 1 Implementation**: Complete - contact AdminPortal team for questions  
**Phase 2 Planning**: In progress - see roadmap above  
**Backend Integration**: Contact AdminAPI and AuthServer teams

---

**Last Updated**: January 17, 2026  
**Status**: ? Phase 1 Complete | ?? Phase 2 Planned  
**Version**: 2.0 (Post-reorganization)

---

*Phase 1 provides the foundation for secure, role-based data access. Phase 2 will complete the implementation with role-aware UI and dispatcher restrictions.* ???
