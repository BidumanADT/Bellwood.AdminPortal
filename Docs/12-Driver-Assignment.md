# Driver Assignment & Affiliate Management

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 17, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Driver Assignment feature enables administrative staff to manage affiliate companies (partner chauffeur services), their drivers, and assign drivers to customer bookings with automated email notifications.

**Key Capabilities**:
- ?? Manage affiliate companies with contact information
- ?? Add and manage drivers under each affiliate
- ?? Assign drivers to bookings
- ?? Automated email notifications to affiliates
- ?? Track driver assignments and availability

**Target Audience**: Developers, administrative staff, affiliate managers  
**Prerequisites**: Understanding of booking workflow, email configuration

---

## ?? Business Use Case

### For Bellwood Staff

**Problem**: Bellwood doesn't own a fleet - they partner with affiliate companies who provide drivers and vehicles.

**Solution**: The portal enables staff to:
1. Maintain a database of trusted affiliate partners
2. Track which drivers work for which affiliates
3. Assign appropriate drivers to customer bookings
4. Notify affiliates of assignments via automated emails
5. Monitor assignment completion and driver performance

---

### Typical Workflow

```
1. Customer books ride
   ?
2. Booking appears in AdminPortal (Status: Requested)
   ?
3. Staff reviews booking requirements:
   - Vehicle type needed
   - Pickup time and location
   - Special requests
   ?
4. Staff selects appropriate affiliate
   ?
5. Staff assigns driver from that affiliate
   ?
6. Status changes to "Scheduled"
   ?
7. Affiliate receives email with:
   - Driver assignment details
   - Customer information
   - Pickup/dropoff locations
   - Vehicle requirements
   ?
8. Driver completes the ride
   ?
9. Status changes to "Completed"
```

---

## ??? Architecture

### Component Overview

```
???????????????????????????????????????????????????????????????
?                     AdminPortal                              ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? Bookings.razor                                 ?        ?
?  ?  - Shows driver assignment status              ?        ?
?  ?  - Click to view detail                        ?        ?
?  ??????????????????????????????????????????????????        ?
?                ? Click booking ? Navigate                   ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? BookingDetail.razor                            ?        ?
?  ?  - Booking information display                 ?        ?
?  ?  - Driver assignment panel                     ?        ?
?  ?  - Hierarchical affiliate/driver selector      ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? Affiliates.razor                               ?        ?
?  ?  - Grid view of all affiliates                 ?        ?
?  ?  - Create/Edit/Delete operations               ?        ?
?  ??????????????????????????????????????????????????        ?
?                ? View Details ? Navigate                    ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? AffiliateDetail.razor                          ?        ?
?  ?  - Affiliate information                       ?        ?
?  ?  - Drivers table                               ?        ?
?  ?  - Add driver functionality                    ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? AffiliateService                               ?        ?
?  ?  - GetAffiliatesAsync()                        ?        ?
?  ?  - CreateAffiliateAsync()                      ?        ?
?  ?  - AddDriverToAffiliateAsync()                 ?        ?
?  ?  - AssignDriverToBookingAsync()                ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
????????????????????????????????????????????????????????????????
                 ? HTTP + JWT + API Key
                 ?
???????????????????????????????????????????????????????????????
?                     AdminAPI                                 ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? Affiliate Endpoints                            ?        ?
?  ?  - GET /affiliates/list                        ?        ?
?  ?  - POST /affiliates                            ?        ?
?  ?  - GET /affiliates/{id}                        ?        ?
?  ?  - PUT /affiliates/{id}                        ?        ?
?  ?  - DELETE /affiliates/{id}                     ?        ?
?  ?                                                ?        ?
?  ? Driver Endpoints                               ?        ?
?  ?  - POST /affiliates/{id}/drivers               ?        ?
?  ?  - GET /drivers/{id}                           ?        ?
?  ?  - PUT /drivers/{id}                           ?        ?
?  ?  - DELETE /drivers/{id}                        ?        ?
?  ?                                                ?        ?
?  ? Assignment Endpoint                            ?        ?
?  ?  - POST /bookings/{id}/assign-driver           ?        ?
?  ??????????????????????????????????????????????????        ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? Email Service                                  ?        ?
?  ?  - Send assignment notification to affiliate   ?        ?
?  ??????????????????????????????????????????????????        ?
????????????????????????????????????????????????????????????????
```

---

## ??? Data Models

### AffiliateDto

**File**: `Models/AffiliateModels.cs`

```csharp
public class AffiliateDto
{
    /// <summary>
    /// Unique identifier for the affiliate
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Affiliate company name (e.g., "Chicago Limo Service")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Primary contact person at the affiliate
    /// </summary>
    public string? PointOfContact { get; set; }

    /// <summary>
    /// Affiliate phone number
    /// </summary>
    public string Phone { get; set; }

    /// <summary>
    /// Affiliate email address (for assignment notifications)
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Street address (optional)
    /// </summary>
    public string? StreetAddress { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State (e.g., "IL", "CA")
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// List of drivers associated with this affiliate
    /// </summary>
    public List<DriverDto> Drivers { get; set; } = new();
}
```

---

### DriverDto

```csharp
public class DriverDto
{
    /// <summary>
    /// Unique identifier for the driver
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// ID of the affiliate this driver works for
    /// </summary>
    public string AffiliateId { get; set; }

    /// <summary>
    /// Driver full name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Driver phone number
    /// </summary>
    public string Phone { get; set; }
}
```

---

### Booking Assignment Fields

Added to `BookingListItem` and `BookingInfo`:

```csharp
/// <summary>
/// ID of the assigned driver (null if unassigned)
/// </summary>
public string? AssignedDriverId { get; set; }

/// <summary>
/// Name of the assigned driver (null if unassigned)
/// </summary>
public string? AssignedDriverName { get; set; }
```

**See**: [22-Data-Models.md](22-Data-Models.md) for complete model documentation

---

## ??? Affiliate Service

### IAffiliateService Interface

**File**: `Services/AffiliateService.cs`

```csharp
public interface IAffiliateService
{
    // Affiliate Management
    Task<List<AffiliateDto>> GetAffiliatesAsync();
    Task<AffiliateDto?> GetAffiliateAsync(string id);
    Task<string> CreateAffiliateAsync(AffiliateDto affiliate);
    Task UpdateAffiliateAsync(string id, AffiliateDto affiliate);
    Task DeleteAffiliateAsync(string id);

    // Driver Management
    Task<string> AddDriverToAffiliateAsync(string affiliateId, DriverDto driver);

    // Driver Assignment
    Task AssignDriverToBookingAsync(string bookingId, string driverId);
}
```

---

### Key Methods

**GetAffiliatesAsync()**:
```csharp
public async Task<List<AffiliateDto>> GetAffiliatesAsync()
{
    var client = await GetAuthorizedClientAsync();
    var response = await client.GetAsync("/affiliates/list");
    response.EnsureSuccessStatusCode();
    
    return await response.Content.ReadFromJsonAsync<List<AffiliateDto>>() 
        ?? new List<AffiliateDto>();
}
```

**AssignDriverToBookingAsync()**:
```csharp
public async Task AssignDriverToBookingAsync(string bookingId, string driverId)
{
    var client = await GetAuthorizedClientAsync();
    var body = new { driverId };
    
    var response = await client.PostAsJsonAsync(
        $"/bookings/{bookingId}/assign-driver", body);
    
    response.EnsureSuccessStatusCode();
}
```

**Authorization** (same pattern as QuoteService):
- X-Admin-ApiKey header
- Authorization: Bearer {JWT}

**Error Handling**:
- HttpRequestException for network errors
- Specific exceptions for 403 Forbidden
- User-friendly error messages

---

## ?? Bookings Page (Updated)

### Bookings.razor

**Driver Status Display**:

```razor
<div class="driver-status mt-2">
    @if (string.IsNullOrEmpty(booking.AssignedDriverName))
    {
        <span class="text-warning">
            <i class="bi bi-exclamation-triangle"></i> Unassigned
        </span>
    }
    else
    {
        <span class="text-success">
            <i class="bi bi-person-check-fill"></i> @booking.AssignedDriverName
        </span>
    }
</div>
```

**Visual**:
- ?? **Unassigned** (yellow warning text with icon)
- ? **?? Michael Johnson** (green success text with icon)

**Click Handler**:
```csharp
private void ViewBookingDetail(string bookingId)
{
    Navigation.NavigateTo($"/bookings/{bookingId}");
}
```

---

## ?? Booking Detail Page

### BookingDetail.razor

**Route**: `/bookings/{BookingId}`

**Layout**: Two-column design

---

### Left Column - Booking Information

**Display**:
- Booking ID
- Status badge (color-coded)
- Passenger name and phone
- Booker information
- Pickup location
- Dropoff location
- Pickup date and time
- Vehicle class
- Passenger count
- Luggage count
- Special requests

**Example UI**:
```
???????????????????????????????????????
? Booking Details                     ?
???????????????????????????????????????
? Booking ID: BK-2025-001234          ?
? Status: [Requested]                 ?
?                                     ?
? Passenger: Alice Morgan             ?
? Phone: (312) 555-1234               ?
?                                     ?
? Booker: John Doe                    ?
? Email: john@company.com             ?
?                                     ?
? Pickup: O'Hare International        ?
? Dropoff: Langham Hotel Chicago      ?
? When: Dec 28, 2025 3:00 PM         ?
?                                     ?
? Vehicle: SUV                        ?
? Passengers: 4                       ?
? Luggage: 6                          ?
?                                     ?
? Special Requests:                   ?
? Please meet at arrivals terminal    ?
???????????????????????????????????????
```

---

### Right Column - Driver Assignment

**Current Assignment Status**:
```razor
<div class="card">
    <div class="card-header">
        <h5>?? Driver Assignment</h5>
    </div>
    <div class="card-body">
        <p><strong>Current Driver:</strong></p>
        @if (string.IsNullOrEmpty(booking.AssignedDriverName))
        {
            <div class="alert alert-warning">
                <i class="bi bi-exclamation-triangle"></i> Unassigned
            </div>
        }
        else
        {
            <div class="alert alert-success">
                <i class="bi bi-person-check-fill"></i> @booking.AssignedDriverName
            </div>
        }
    </div>
</div>
```

---

**Hierarchical Driver Selector**:

```
????????????????????????????????????????
? Select Driver:                       ?
????????????????????????????????????????
? ? ?? Chicago Limo Service (3)       ?
?   ?? ?? Michael Johnson  [Assign]   ?
?   ?? ?? Sarah Lee        [Assign]   ?
?   ?? ?? Robert Brown     [Assign]   ?
?   ?? [+ Add Driver]                  ?
?                                      ?
? ? ?? Suburban Chauffeurs (1)        ?
?                                      ?
? ? ?? Elite Transportation (2)       ?
????????????????????????????????????????
```

**Implementation**:
```razor
@foreach (var affiliate in affiliates)
{
    <div class="affiliate-node">
        <div class="affiliate-header" @onclick="() => ToggleAffiliate(affiliate.Id)">
            <i class="bi @(IsExpanded(affiliate.Id) ? "bi-caret-down-fill" : "bi-caret-right-fill")"></i>
            <span class="affiliate-name">@affiliate.Name</span>
            <span class="driver-count">(@affiliate.Drivers.Count)</span>
        </div>

        @if (IsExpanded(affiliate.Id))
        {
            <div class="driver-list">
                @foreach (var driver in affiliate.Drivers)
                {
                    <div class="driver-item">
                        <span>?? @driver.Name</span>
                        <button class="btn btn-sm btn-primary" 
                                @onclick="() => AssignDriver(driver.Id, driver.Name)">
                            Assign
                        </button>
                    </div>
                }

                <button class="btn btn-sm btn-outline-secondary mt-2" 
                        @onclick="() => ShowAddDriverForm(affiliate.Id)">
                    + Add Driver
                </button>
            </div>
        }
    </div>
}
```

---

**Inline Add Driver Form**:

When user clicks "+ Add Driver", show:

```razor
@if (showAddDriverForm && selectedAffiliateId == affiliate.Id)
{
    <div class="add-driver-form mt-2 p-2 border rounded">
        <div class="mb-2">
            <input type="text" 
                   class="form-control form-control-sm" 
                   @bind="newDriverName"
                   placeholder="Driver Name *" />
        </div>
        <div class="mb-2">
            <input type="text" 
                   class="form-control form-control-sm" 
                   @bind="newDriverPhone"
                   placeholder="Phone *" />
        </div>
        <div class="btn-group btn-group-sm">
            <button class="btn btn-success" @onclick="SaveNewDriver">
                Save
            </button>
            <button class="btn btn-secondary" @onclick="CancelAddDriver">
                Cancel
            </button>
        </div>
    </div>
}
```

---

**Assignment Logic**:

```csharp
private async Task AssignDriver(string driverId, string driverName)
{
    isAssigning = true;
    errorMessage = null;
    successMessage = null;

    try
    {
        await AffiliateService.AssignDriverToBookingAsync(BookingId, driverId);

        // Update local state
        booking.AssignedDriverId = driverId;
        booking.AssignedDriverName = driverName;
        booking.Status = "Scheduled";  // Status auto-updated by API

        successMessage = $"? Driver assigned successfully! {driverName} will handle this booking. Affiliate has been notified via email.";
        
        StateHasChanged();
    }
    catch (Exception ex)
    {
        errorMessage = $"Failed to assign driver: {ex.Message}";
    }
    finally
    {
        isAssigning = false;
    }
}
```

---

## ?? Affiliates Page

### Affiliates.razor

**Route**: `/affiliates`

**Features**:
- Grid view of all affiliates (responsive 3-column layout)
- Create new affiliate
- Edit existing affiliate
- Delete affiliate (with confirmation)
- View affiliate details

**UI Layout**:

```
????????????????????????????????????????????????????????????
? Affiliate Management           [+ Create Affiliate]      ?
????????????????????????????????????????????????????????????
? ????????????????  ????????????????  ????????????????   ?
? ? ?? Chicago   ?  ? ?? Suburban  ?  ? ?? Elite     ?   ?
? ? Limo Service ?  ? Chauffeurs   ?  ? Transport    ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? Contact:     ?  ? Contact:     ?  ? Contact:     ?   ?
? ? John Smith   ?  ? Emily Davis  ?  ? Tom Wilson   ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? ?? 312-555-  ?  ? ?? 847-555-  ?  ? ?? 630-555-  ?   ?
? ?    1234      ?  ?    9876      ?  ?    4567      ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? ?? dispatch@ ?  ? ?? emily@    ?  ? ?? dispatch@ ?   ?
? ?    chicago.. ?  ?    suburban. ?  ?    elite...  ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? ?? Chicago,  ?  ? ?? Napervil  ?  ? ?? Oakbrook  ?   ?
? ?    IL        ?  ?    le, IL    ?  ?    , IL      ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? ?? 3 drivers ?  ? ?? 1 driver  ?  ? ?? 2 drivers ?   ?
? ?              ?  ?              ?  ?              ?   ?
? ? [View Detail]?  ? [View Detail]?  ? [View Detail]?   ?
? ? [Edit]       ?  ? [Edit]       ?  ? [Edit]       ?   ?
? ? [Delete]     ?  ? [Delete]     ?  ? [Delete]     ?   ?
? ????????????????  ????????????????  ????????????????   ?
????????????????????????????????????????????????????????????
```

---

### Create Affiliate Form

**Trigger**: Click "+ Create Affiliate" button

**Fields**:
```razor
<div class="row">
    <div class="col-md-6 mb-3">
        <label>Name *</label>
        <input type="text" class="form-control" @bind="newAffiliate.Name" required />
    </div>
    <div class="col-md-6 mb-3">
        <label>Point of Contact</label>
        <input type="text" class="form-control" @bind="newAffiliate.PointOfContact" />
    </div>
</div>

<div class="row">
    <div class="col-md-6 mb-3">
        <label>Phone *</label>
        <input type="tel" class="form-control" @bind="newAffiliate.Phone" required />
    </div>
    <div class="col-md-6 mb-3">
        <label>Email *</label>
        <input type="email" class="form-control" @bind="newAffiliate.Email" required />
    </div>
</div>

<div class="row">
    <div class="col-12 mb-3">
        <label>Street Address</label>
        <input type="text" class="form-control" @bind="newAffiliate.StreetAddress" />
    </div>
</div>

<div class="row">
    <div class="col-md-6 mb-3">
        <label>City</label>
        <input type="text" class="form-control" @bind="newAffiliate.City" />
    </div>
    <div class="col-md-6 mb-3">
        <label>State</label>
        <input type="text" class="form-control" @bind="newAffiliate.State" maxlength="2" />
    </div>
</div>

<div class="d-grid gap-2">
    <button class="btn btn-primary" @onclick="SaveAffiliate">
        ?? Save Affiliate
    </button>
    <button class="btn btn-outline-secondary" @onclick="CancelCreate">
        Cancel
    </button>
</div>
```

**Validation**:
- Name, Phone, Email are required
- Email format validation
- State limited to 2 characters

---

### Delete Confirmation Modal

**Warning**: Shows cascade effect

```razor
<div class="modal" style="display:@(showDeleteModal ? "block" : "none")">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Confirm Delete</h5>
            </div>
            <div class="modal-body">
                <p class="text-danger">
                    <strong>Warning:</strong> Deleting this affiliate will also delete 
                    <strong>@affiliateToDelete?.Drivers.Count driver(s)</strong> associated with it.
                </p>
                <p>Are you sure you want to delete <strong>@affiliateToDelete?.Name</strong>?</p>
            </div>
            <div class="modal-footer">
                <button class="btn btn-danger" @onclick="ConfirmDelete">
                    Delete
                </button>
                <button class="btn btn-secondary" @onclick="CancelDelete">
                    Cancel
                </button>
            </div>
        </div>
    </div>
</div>
```

---

## ?? Affiliate Detail Page

### AffiliateDetail.razor

**Route**: `/affiliates/{AffiliateId}`

**Layout**: Single page with affiliate info and drivers table

---

### Affiliate Information Section

```
???????????????????????????????????????????
? Affiliate Details                       ?
???????????????????????????????????????????
? Name: Chicago Limo Service              ?
? Contact: John Smith                     ?
? Phone: (312) 555-1234                   ?
? Email: dispatch@chicagolimo.com         ?
? Address: 123 Main St, Chicago, IL       ?
?                                         ?
? [Edit Affiliate] [Back to List]        ?
???????????????????????????????????????????
```

---

### Drivers Table

```
???????????????????????????????????????????
? Drivers                  [+ Add Driver] ?
???????????????????????????????????????????
? Name             ? Phone      ? Actions ?
???????????????????????????????????????????
? Michael Johnson  ? 312-555-01 ? [Edit]  ?
? Sarah Lee        ? 312-555-02 ? [Edit]  ?
? Robert Brown     ? 312-555-03 ? [Edit]  ?
???????????????????????????????????????????
```

**Add Driver Form** (inline):

```razor
@if (showAddDriverForm)
{
    <div class="card mt-3">
        <div class="card-body">
            <h6>Add New Driver</h6>
            <div class="mb-2">
                <label>Driver Name *</label>
                <input type="text" class="form-control" @bind="newDriver.Name" />
            </div>
            <div class="mb-2">
                <label>Phone *</label>
                <input type="tel" class="form-control" @bind="newDriver.Phone" />
            </div>
            <div class="btn-group">
                <button class="btn btn-success" @onclick="SaveDriver">
                    Save
                </button>
                <button class="btn btn-secondary" @onclick="CancelAddDriver">
                    Cancel
                </button>
            </div>
        </div>
    </div>
}
```

---

## ?? Email Notifications

### Assignment Email

When driver is assigned, AdminAPI sends email to affiliate:

**Subject**: New Booking Assignment - [Date]

**Body**:
```
Dear [Affiliate Name],

A new booking has been assigned to your driver.

Driver Assigned: Michael Johnson
Phone: (312) 555-0001

Booking Details:
- Passenger: Alice Morgan
- Pickup: O'Hare International Airport, Terminal 5
- Dropoff: Langham Hotel, 330 N Wabash Ave
- Date/Time: December 28, 2025 at 3:00 PM
- Vehicle: SUV
- Passengers: 4
- Luggage: 6

Special Instructions:
Please meet passenger at arrivals terminal.

Booking ID: BK-2025-001234

Please confirm receipt and driver availability.

Thank you,
Bellwood Global Team
```

**See**: Archived `EMAIL_FIX_FOR_ADMINAPI.md` for email template implementation

---

## ?? Testing

### Test Scenarios

**Test 1: Create Affiliate & Add Driver**:
1. Navigate to `/affiliates`
2. Click "+ Create Affiliate"
3. Fill required fields:
   - Name: "Test Limo Service"
   - Phone: "555-1234"
   - Email: "test@limo.com"
4. Click "Save"
5. **Verify**: Affiliate appears in grid
6. Click "View Details"
7. Click "+ Add Driver"
8. Fill driver info:
   - Name: "Test Driver"
   - Phone: "555-5678"
9. Click "Save"
10. **Verify**: Driver appears in table

**Test 2: Assign Driver to Booking**:
1. Navigate to `/bookings`
2. Click unassigned booking
3. **Verify**: Shows "Unassigned" warning
4. Expand affiliate node
5. Click "Assign" next to driver
6. **Verify**: Success message displays
7. **Verify**: Driver name shows in green
8. **Verify**: Email sent to affiliate

**Test 3: Quick Add Driver During Assignment**:
1. On booking detail page
2. Expand affiliate
3. Click "+ Add Driver"
4. Enter driver details
5. Click "Save"
6. **Verify**: Driver appears in list
7. Click "Assign" for new driver
8. **Verify**: Assignment successful

**Test 4: Delete Affiliate Cascade**:
1. Navigate to `/affiliates`
2. Click "Delete" on affiliate with drivers
3. **Verify**: Modal shows driver count warning
4. Click "Delete"
5. **Verify**: Affiliate and drivers removed

**See**: [02-Testing-Guide.md](02-Testing-Guide.md) for comprehensive testing procedures

---

## ?? Related Documentation

- [System Architecture](01-System-Architecture.md) - Overall design
- [Data Models](22-Data-Models.md) - Affiliate and driver DTOs
- [API Reference](20-API-Reference.md) - Affiliate endpoints
- [Real-Time Tracking](10-Real-Time-Tracking.md) - Driver GPS tracking
- [Security Model](23-Security-Model.md) - Authentication requirements

---

**Last Updated**: January 17, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*Driver Assignment and Affiliate Management enable efficient coordination with partner chauffeur services, ensuring smooth booking fulfillment.* ???
