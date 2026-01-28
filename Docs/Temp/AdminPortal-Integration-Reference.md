# AdminPortal Integration Reference - Phase Alpha Quote Lifecycle

**Document Type**: Integration Reference  
**Target Audience**: AdminPortal Development Team  
**Last Updated**: January 27, 2026  
**AdminAPI Version**: Phase Alpha (1.0.0)  
**Status**: ? Production Ready

---

## ?? Overview

This document provides the AdminPortal team with all necessary API integration details for implementing the Phase Alpha quote lifecycle management dashboard.

**What You'll Build**:
- Quote list view with filtering
- Quote detail view with lifecycle actions
- Quote response form (price/ETA entry)
- Booking integration (quote acceptance)

**What We Provide**:
- 7 quote lifecycle endpoints
- Complete data models
- RBAC enforcement
- Field masking for dispatchers

---

## ?? Authentication

**All endpoints require JWT authentication** via Bearer token.

### Required Header

```
Authorization: Bearer {jwt_token}
```

### Obtaining Token

**Endpoint**: `POST https://auth.bellwood.com/api/auth/login`

```json
// Request
{
  "username": "diana",
  "password": "password"
}

// Response
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "username": "diana",
    "role": "dispatcher",
    "uid": "user-guid-123"
  }
}
```

### User Roles

| Role | Quote Access | Can Acknowledge | Can Respond | Can Accept | Notes |
|------|--------------|-----------------|-------------|------------|-------|
| **admin** | All quotes | ? Yes | ? Yes | ? No* | Full visibility, cannot accept on behalf |
| **dispatcher** | All quotes | ? Yes | ? Yes | ? No* | Billing fields masked |
| **booker** | Own quotes only | ? No | ? No | ? Yes | Can only accept own quotes |

***Critical**: Staff (admin/dispatcher) CANNOT accept quotes. Only the booker who created the quote can accept it (passenger consent required).

---

## ?? Quote List View (Section 2.1)

### GET /quotes/list - List All Quotes

**Purpose**: Retrieve paginated list of quotes for dashboard.

**Endpoint**:
```
GET https://api.bellwood.com/quotes/list?take=50
```

**Authorization**: `StaffOnly` (admin or dispatcher)

**Query Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `take` | integer | 50 | Number of quotes to return (max: 200) |

**Note**: Status filtering (`?status=Pending`) not implemented in Phase Alpha. Filter client-side for now.

**Response** (200 OK):

```json
[
  {
    "id": "quote-abc123",
    "createdUtc": "2026-01-27T15:30:00Z",
    "status": "Submitted",                    // or Acknowledged, Responded, Accepted, Cancelled
    "bookerName": "Chris Bailey",
    "passengerName": "Jordan Chen",
    "vehicleClass": "Sedan",
    "pickupLocation": "Langham Hotel, Chicago",
    "dropoffLocation": "O'Hare International Airport",
    "pickupDateTime": "2026-02-01T09:00:00Z"
  }
  // ... more quotes
]
```

**Field Mapping for Dashboard**:

| Dashboard Column | API Field | Notes |
|------------------|-----------|-------|
| Requestor Name | `bookerName` | Person who submitted quote |
| Pickup/Dropoff | `pickupLocation` / `dropoffLocation` | Combine for summary |
| Requested Date | `pickupDateTime` | Convert to local timezone |
| Status | `status` | Enum: Submitted, Acknowledged, Responded, Accepted, Cancelled |
| Created Time | `createdUtc` | Convert to local timezone |

**Status Enum Values**:

| Status | Display Label | Color Suggestion | Description |
|--------|---------------|------------------|-------------|
| `Submitted` | Pending | Yellow/Orange | Awaiting dispatcher acknowledgment |
| `Acknowledged` | Acknowledged | Blue | Dispatcher acknowledged, preparing response |
| `Responded` | Responded | Purple | Price/ETA sent to passenger |
| `Accepted` | Accepted | Green | Passenger accepted, booking created |
| `Cancelled` | Cancelled | Gray | Quote cancelled |

**Data Access**:
- ? Admin sees all quotes
- ? Dispatcher sees all quotes
- ? Booker sees only own quotes (not relevant for AdminPortal)

---

## ?? Quote Detail View (Section 2.1)

### GET /quotes/{id} - Get Quote Details

**Purpose**: Retrieve complete quote information for detail modal/page.

**Endpoint**:
```
GET https://api.bellwood.com/quotes/{id}
```

**Authorization**: `StaffOnly` (admin or dispatcher)

**Response** (200 OK):

```json
{
  "id": "quote-abc123",
  "status": "Responded",
  "createdUtc": "2026-01-27T15:30:00Z",
  
  // Basic info (always present)
  "bookerName": "Chris Bailey",
  "passengerName": "Jordan Chen",
  "vehicleClass": "Sedan",
  "pickupLocation": "Langham Hotel, Chicago",
  "dropoffLocation": "O'Hare International Airport",
  "pickupDateTime": "2026-02-01T09:00:00Z",
  
  // Full passenger/booker details
  "draft": {
    "booker": {
      "firstName": "Chris",
      "lastName": "Bailey",
      "phoneNumber": "312-555-5555",
      "emailAddress": "chris.bailey@example.com"
    },
    "passenger": {
      "firstName": "Jordan",
      "lastName": "Chen",
      "phoneNumber": "312-555-6666",
      "emailAddress": "jordan.chen@example.com"
    },
    "vehicleClass": "Sedan",
    "pickupDateTime": "2026-02-01T09:00:00Z",
    "pickupLocation": "Langham Hotel, Chicago",
    "pickupStyle": "Curbside",
    "dropoffLocation": "O'Hare International Airport",
    "roundTrip": false,
    "passengerCount": 2,
    "checkedBags": 2,
    "carryOnBags": 1
  },
  
  // Lifecycle fields (Phase Alpha - only populated after dispatcher actions)
  "createdByUserId": "user-guid-123",           // Owner ID
  "modifiedByUserId": "dispatcher-guid-456",    // Last modifier
  "modifiedOnUtc": "2026-01-27T16:00:00Z",      // Last modification time
  "acknowledgedAt": "2026-01-27T15:35:00Z",     // When acknowledged (null if not yet)
  "acknowledgedByUserId": "dispatcher-guid-456",// Who acknowledged
  "respondedAt": "2026-01-27T15:40:00Z",        // When responded (null if not yet)
  "respondedByUserId": "dispatcher-guid-456",   // Who responded
  "estimatedPrice": 125.50,                     // Dispatcher's price estimate (null if not yet)
  "estimatedPickupTime": "2026-02-01T08:45:00Z",// Dispatcher's ETA (null if not yet)
  "notes": "VIP service confirmed. Driver will meet you at arrivals."  // Dispatcher notes (null if not yet)
}
```

**Field Masking for Dispatchers**:
- ? Quotes do NOT have billing fields to mask
- ? All lifecycle fields visible to both admin and dispatcher
- ? No special handling needed for role-based display

**UI Action Mapping**:

Based on `status` field:

| Status | Show Buttons | Fields to Display |
|--------|--------------|-------------------|
| `Submitted` | **[Acknowledge]** | Basic info + draft |
| `Acknowledged` | **[Send Response]** (form) | All + price/ETA input fields |
| `Responded` | Read-only | All + `estimatedPrice`, `estimatedPickupTime`, `notes` |
| `Accepted` | Read-only + **[View Booking]** | All + link to booking (use booking search) |
| `Cancelled` | Read-only | All |

---

## ? Acknowledge Quote (Section 2.1)

### POST /quotes/{id}/acknowledge - Dispatcher Acknowledges Quote

**Purpose**: Mark quote as acknowledged (dispatcher has seen it).

**Endpoint**:
```
POST https://api.bellwood.com/quotes/{id}/acknowledge
```

**Authorization**: `StaffOnly` (admin or dispatcher)

**Request**: No body required

**FSM Validation**:
- ? Can only acknowledge quotes with status `Submitted`
- ? Returns 400 Bad Request for other statuses

**Response** (200 OK):

```json
{
  "message": "Quote acknowledged successfully",
  "id": "quote-abc123",
  "status": "Acknowledged",
  "acknowledgedAt": "2026-01-27T14:30:00Z",
  "acknowledgedBy": "diana-user-guid"
}
```

**Error Responses**:

```json
// 400 Bad Request - Invalid status
{
  "error": "Can only acknowledge quotes with status 'Submitted'. Current status: Acknowledged"
}

// 404 Not Found
{
  "error": "Quote not found"
}
```

**UI Flow**:
1. User clicks **[Acknowledge]** button on `Submitted` quote
2. Call `POST /quotes/{id}/acknowledge`
3. On success:
   - Update quote status to `Acknowledged` in UI
   - Show success toast: "Quote acknowledged"
   - Refresh quote detail to show response form

---

## ?? Send Quote Response (Section 2.1)

### POST /quotes/{id}/respond - Dispatcher Sends Price/ETA

**Purpose**: Dispatcher provides estimated price and pickup time to passenger.

**Endpoint**:
```
POST https://api.bellwood.com/quotes/{id}/respond
```

**Authorization**: `StaffOnly` (admin or dispatcher)

**Request Body**:

```json
{
  "estimatedPrice": 125.50,                       // decimal, required, must be > 0
  "estimatedPickupTime": "2026-02-01T14:00:00",   // ISO 8601 DateTime, required, must be in future
  "notes": "VIP service confirmed. Driver will meet you at arrivals."  // string, optional
}
```

**Validation Rules**:

| Field | Type | Required | Validation | Error Message |
|-------|------|----------|------------|---------------|
| `estimatedPrice` | decimal | Yes | > 0 | "EstimatedPrice must be greater than 0" |
| `estimatedPickupTime` | DateTime | Yes | Future (1-min grace period) | "EstimatedPickupTime must be in the future" |
| `notes` | string | No | Max 500 chars (not enforced, but recommended) | - |

**FSM Validation**:
- ? Can only respond to quotes with status `Acknowledged`
- ? Returns 400 Bad Request for other statuses

**Response** (200 OK):

```json
{
  "message": "Quote response sent successfully",
  "id": "quote-abc123",
  "status": "Responded",
  "respondedAt": "2026-01-27T14:35:00Z",
  "respondedBy": "diana-user-guid",
  "estimatedPrice": 150.00,
  "estimatedPickupTime": "2026-02-01T14:00:00",
  "notes": "VIP service confirmed"
}
```

**Side Effects**:
- ? Quote status ? `Responded`
- ? **Email sent to passenger** with price/ETA details
- ? Passenger can now accept quote via PassengerApp

**Error Responses**:

```json
// 400 Bad Request - Invalid status
{
  "error": "Can only respond to quotes with status 'Acknowledged'. Current status: Responded"
}

// 400 Bad Request - Invalid price
{
  "error": "EstimatedPrice must be greater than 0"
}

// 400 Bad Request - Invalid time
{
  "error": "EstimatedPickupTime must be in the future"
}
```

**UI Flow**:
1. User fills out form on `Acknowledged` quote:
   - Price input (numeric, required, > 0)
   - Pickup time input (datetime, required, future)
   - Notes textarea (optional)
2. Click **[Send Response]** button
3. Call `POST /quotes/{id}/respond` with form data
4. On success:
   - Update quote status to `Responded` in UI
   - Show success toast: "Quote response sent to passenger"
   - Refresh quote detail (now read-only with price/ETA displayed)

**Validation Notes**:
- Price: Use step="0.01" for 2 decimal places
- Time: Backend uses 1-minute grace period to handle clock skew
- DateTime format: ISO 8601 (`2026-02-01T14:00:00` or `2026-02-01T14:00:00Z`)

---

## ?? View Accepted Quotes & Booking Link (Section 2.2)

### Quote Acceptance (PassengerApp Only)

**Note**: Passengers accept quotes via PassengerApp (mobile), **NOT AdminPortal**.

AdminPortal should:
- ? Display `Accepted` quotes as read-only
- ? Show **[View Booking]** button/link
- ? DO NOT provide accept button (passengers only)

### Finding Associated Booking

**When quote status is `Accepted`**, a booking was automatically created with:
- `SourceQuoteId` = quote ID
- `Status` = `Requested` (ready for staff confirmation/driver assignment)

**To Find Booking**:

**Option 1: Search bookings by SourceQuoteId** (Recommended)

```
GET https://api.bellwood.com/bookings/list?take=200
```

Filter client-side:
```javascript
const quote = { id: "quote-abc123" };
const bookings = await fetchBookings();
const linkedBooking = bookings.find(b => b.sourceQuoteId === quote.id);

if (linkedBooking) {
  window.location.href = `/bookings/${linkedBooking.id}`;
}
```

**Option 2: Server-side search** (Future enhancement - not yet implemented)
```
GET /bookings/by-quote/{quoteId}  // TODO: Phase 3
```

**Booking Detail Includes SourceQuoteId**:

```
GET https://api.bellwood.com/bookings/{bookingId}
```
```json
{
  "id": "booking-xyz789",
  "status": "Requested",
  "sourceQuoteId": "quote-abc123",   // ? Link back to originating quote
  "passengerName": "Jordan Chen",
  "pickupLocation": "Langham Hotel",
  // ... rest of booking fields
}
```

**UI Implementation**:

```javascript
// Quote detail page - Accepted status
if (quote.status === "Accepted") {
  // Show "View Booking" button
  const viewBookingButton = document.createElement("button");
  viewBookingButton.textContent = "View Booking";
  viewBookingButton.onclick = async () => {
    // Find linked booking
    const bookings = await fetchBookings();
    const linkedBooking = bookings.find(b => b.sourceQuoteId === quote.id);
    
    if (linkedBooking) {
      navigateToBooking(linkedBooking.id);
    } else {
      showError("Associated booking not found");
    }
  };
}
```

**Booking Source Indicator**:

In booking list/detail, show "From Quote" badge if `sourceQuoteId` is present:

```html
<!-- Booking list item -->
<tr>
  <td>booking-xyz789</td>
  <td>Jordan Chen</td>
  <td>Requested</td>
  <td>
    <span class="badge badge-info" *ngIf="booking.sourceQuoteId">
      From Quote
    </span>
  </td>
</tr>
```

---

## ?? Real-Time Updates (Section 2.3)

### WebSocket Notifications (Optional Enhancement)

**Endpoint**: `wss://api.bellwood.com/hubs/location`

**Purpose**: Real-time notifications for new quotes and status changes.

**Authentication**:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl(`wss://api.bellwood.com/hubs/location?access_token=${token}`)
    .build();

await connection.start();
```

**Suggested Events** (Not yet implemented - use polling for Alpha):

```javascript
// TODO: Phase 3 - Real-time quote notifications
connection.on("NewQuoteSubmitted", (quoteId, bookerName) => {
  showToast(`New quote from ${bookerName}`);
  refreshQuoteList();
});

connection.on("QuoteStatusChanged", (quoteId, newStatus) => {
  updateQuoteInList(quoteId, newStatus);
});
```

**For Alpha**: Use **periodic polling** instead:

```javascript
// Poll every 30 seconds for new quotes
setInterval(async () => {
  const quotes = await fetchQuotes();
  checkForNewQuotes(quotes);
}, 30000);
```

---

## ??? RBAC & Field Masking (Section 2.3)

### Dispatcher Limitations

**What Dispatchers CAN Do**:
- ? View all quotes
- ? Acknowledge quotes
- ? Respond to quotes (send price/ETA)
- ? View all bookings
- ? Assign drivers to bookings

**What Dispatchers CANNOT Do**:
- ? Accept quotes on behalf of passengers (only passenger can accept)
- ? View payment/billing fields in bookings (masked as null)
- ? Seed test data
- ? Manage OAuth credentials

### Field Masking for Bookings

**When dispatcher views booking detail**, billing fields are masked:

```json
// Dispatcher sees:
{
  "id": "booking-xyz",
  "passengerName": "Jordan Chen",
  // ... operational fields ...
  
  // Billing fields (MASKED for dispatcher, visible to admin)
  "paymentMethodId": null,
  "paymentMethodLast4": null,
  "paymentAmount": null,
  "totalAmount": null,
  "totalFare": null
}
```

**Admin sees actual values** (when populated in Phase 3).

**UI Implementation**:

```javascript
// Check user role
const userRole = getUserRole(); // "admin" or "dispatcher"

// Show masked field with tooltip
if (booking.paymentMethodLast4 === null && userRole === "dispatcher") {
  // Show tooltip: "Payment details are only visible to administrators"
  showMaskedField("Payment Method", "Hidden (admin only)");
} else if (booking.paymentMethodLast4) {
  showField("Payment Method", `**** **** **** ${booking.paymentMethodLast4}`);
}
```

---

## ?? Summary of Endpoints

| Endpoint | Method | Purpose | Auth | Dashboard Use |
|----------|--------|---------|------|---------------|
| `/quotes/list` | GET | List quotes | StaffOnly | Quote list view |
| `/quotes/{id}` | GET | Quote details | StaffOnly | Quote detail modal/page |
| `/quotes/{id}/acknowledge` | POST | Acknowledge quote | StaffOnly | "Acknowledge" button |
| `/quotes/{id}/respond` | POST | Send price/ETA | StaffOnly | "Send Response" form |
| `/bookings/list` | GET | List bookings | StaffOnly | Find linked booking (filter by `sourceQuoteId`) |
| `/bookings/{id}` | GET | Booking details | StaffOnly | View booking from accepted quote |

---

## ?? Quick Start Checklist

- [ ] Implement authentication with AuthServer (obtain JWT token)
- [ ] Create quote list view with status badges
- [ ] Add status filtering (client-side for now)
- [ ] Implement quote detail modal/page
- [ ] Add "Acknowledge" button (status: `Submitted`)
- [ ] Create price/ETA response form (status: `Acknowledged`)
- [ ] Show read-only view for `Responded`, `Accepted`, `Cancelled`
- [ ] Add "View Booking" link for `Accepted` quotes
- [ ] Implement periodic polling for new quotes (30s interval)
- [ ] Add toast notifications for new quotes
- [ ] Handle RBAC (dispatcher vs admin)
- [ ] Implement field masking tooltips for bookings

---

## ?? Common Pitfalls

**1. Status Filtering**:
- ?? Server-side status filtering not implemented in Phase Alpha
- ? Use client-side filtering: `quotes.filter(q => q.status === "Submitted")`

**2. Finding Linked Booking**:
- ?? No direct `/bookings/by-quote/{id}` endpoint yet
- ? Fetch all bookings, filter by `sourceQuoteId`

**3. Accept Button**:
- ?? DO NOT add accept button in AdminPortal
- ? Only passengers can accept quotes (via PassengerApp)
- ? AdminPortal just shows `Accepted` status as read-only

**4. DateTime Formats**:
- ?? API expects ISO 8601 format
- ? Use `new Date().toISOString()` or `moment().format()`
- ? 1-minute grace period for clock skew

**5. Field Masking**:
- ?? Quotes do NOT have billing fields to mask
- ? Only bookings have masked billing fields (for dispatchers)

---

## ?? Related Documentation

**AdminAPI Docs**:
- `20-API-Reference.md` - Complete endpoint reference
- `15-Quote-Lifecycle.md` - Phase Alpha implementation details
- `23-Security-Model.md` - RBAC & authorization

**AuthServer Docs**:
- `AdminAPI-Phase2-Reference.md` - JWT authentication details

---

## ?? Support

**Questions?** Contact AdminAPI team:
- GitHub Issues: [AdminAPI Repository](https://github.com/BidumanADT/Bellwood.AdminApi/issues)
- Email: api-support@bellwood.com

---

**Document Version**: 1.0.0  
**AdminAPI Version**: Phase Alpha  
**Last Updated**: January 27, 2026  
**Status**: ? Production Ready
