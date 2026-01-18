# Data Models & DTOs

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides a complete reference of all data models, DTOs (Data Transfer Objects), and entity schemas used in the Bellwood AdminPortal and AdminAPI.

**Purpose**: Catalog all data structures for developers integrating with AdminAPI or extending AdminPortal

**Conventions**:
- All DTOs use PascalCase property names
- Dates stored as `DateTime` (UTC)
- Optional fields use nullable types (`string?`, `int?`)
- Phase 1 audit fields added January 2026

**Target Audience**: Developers, API consumers, integration engineers  
**Prerequisites**: Understanding of C#, JSON serialization, REST APIs

---

## ?? Model Categories

### Booking Models
- [BookingListItem](#bookinglistitem) - Summary for list views
- [BookingDetailDto](#bookingdetaildto) - Complete booking details
- [AssignDriverRequest](#assigndriverrequest) - Driver assignment

### Quote Models
- [QuoteDetailDto](#quotedetaildto) - Quote information
- [UpdateQuoteDto](#updatequotedto) - Quote updates

### Affiliate & Driver Models
- [AffiliateDto](#affiliatedto) - Affiliate company info
- [DriverDto](#driverdto) - Driver information

### Location & Tracking Models
- [LocationUpdate](#locationupdate) - Real-time GPS update
- [LocationResponse](#locationresponse) - REST API location
- [ActiveRideLocationDto](#activeridelocationdto) - Active ride with location
- [RideStatusChangedEvent](#ridestatuschangedevent) - Status change event

### Authentication Models
- [LoginRequest](#loginrequest) - Login credentials
- [LoginResponse](#loginresponse) - JWT tokens

---

## ?? Booking Models

### BookingListItem

**Purpose**: Summary information for bookings list view

**File**: `Components/Pages/Bookings.razor` (internal DTO)

**Properties**:
```csharp
public class BookingListItem
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string? Status { get; set; }
    public string? CurrentRideStatus { get; set; }  // Real-time driver status
    public string BookerName { get; set; } = "";
    public string PassengerName { get; set; } = "";
    public string VehicleClass { get; set; } = "";
    public string PickupLocation { get; set; } = "";
    public string? DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTimeOffset? PickupDateTimeOffset { get; set; }  // Phase 1
    public string? AssignedDriverId { get; set; }
    public string? AssignedDriverName { get; set; }
    public string? AssignedDriverUid { get; set; }
    
    // Phase 1: Audit trail (January 2026)
    public string? CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
    
    // Computed property
    public string DisplayStatus => CurrentRideStatus ?? Status ?? "Unknown";
}
```

**JSON Example**:
```json
{
  "id": "bk-2025-001",
  "createdUtc": "2025-01-15T10:00:00Z",
  "status": "Scheduled",
  "currentRideStatus": "OnRoute",
  "bookerName": "Sarah Johnson",
  "passengerName": "Taylor Reed",
  "vehicleClass": "SUV",
  "pickupLocation": "O'Hare FBO Terminal",
  "dropoffLocation": "Langham Hotel Chicago",
  "pickupDateTime": "2025-01-20T14:00:00Z",
  "pickupDateTimeOffset": "2025-01-20T08:00:00-06:00",
  "assignedDriverId": "drv-001",
  "assignedDriverName": "Michael Johnson",
  "assignedDriverUid": "driver-001",
  "createdByUserId": "user-123",
  "modifiedByUserId": "admin-001",
  "modifiedOnUtc": "2025-01-18T09:30:00Z"
}
```

**Field Descriptions**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique booking identifier (e.g., "bk-2025-001") |
| `status` | string | Yes | Booking status: Requested, Confirmed, Scheduled, InProgress, Completed, Cancelled |
| `currentRideStatus` | string | No | Real-time driver status: OnRoute, Arrived, PassengerOnboard (null if not active) |
| `pickupDateTimeOffset` | DateTimeOffset | No | **Phase 1**: Timezone-aware pickup time |
| `createdByUserId` | string | No | **Phase 1**: User ID who created booking |
| `modifiedByUserId` | string | No | **Phase 1**: User ID who last modified |
| `modifiedOnUtc` | DateTime | No | **Phase 1**: When last modified |

**Usage**: GET `/bookings/list` response

---

### BookingDetailDto

**Purpose**: Complete booking information for detail view

**Extends**: BookingListItem with additional fields

**Additional Properties**:
```csharp
public class BookingDetailDto : BookingListItem
{
    public string? BookerEmail { get; set; }
    public string? BookerPhone { get; set; }
    public string? PassengerPhone { get; set; }
    public int PassengerCount { get; set; }
    public int LuggageCount { get; set; }
    public string? SpecialRequests { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}
```

**JSON Example**:
```json
{
  "id": "bk-2025-001",
  "status": "Scheduled",
  "currentRideStatus": "OnRoute",
  "passengerName": "Taylor Reed",
  "passengerPhone": "(312) 555-7890",
  "bookerName": "Sarah Johnson",
  "bookerEmail": "sarah@company.com",
  "bookerPhone": "(312) 555-1111",
  "pickupLocation": "O'Hare FBO Terminal",
  "dropoffLocation": "Langham Hotel Chicago",
  "pickupDateTime": "2025-01-20T14:00:00Z",
  "pickupDateTimeOffset": "2025-01-20T08:00:00-06:00",
  "vehicleClass": "SUV",
  "passengerCount": 3,
  "luggageCount": 5,
  "specialRequests": "Meet at FBO main entrance",
  "assignedDriverId": "drv-001",
  "assignedDriverName": "Michael Johnson",
  "assignedDriverUid": "driver-001",
  "createdUtc": "2025-01-15T10:00:00Z",
  "updatedUtc": "2025-01-18T09:30:00Z",
  "createdByUserId": "user-123",
  "modifiedByUserId": "admin-001",
  "modifiedOnUtc": "2025-01-18T09:30:00Z"
}
```

**Usage**: GET `/bookings/{id}` response

---

### AssignDriverRequest

**Purpose**: Assign a driver to a booking

**File**: Request DTO for driver assignment

**Properties**:
```csharp
public class AssignDriverRequest
{
    public string DriverId { get; set; } = string.Empty;
}
```

**JSON Example**:
```json
{
  "driverId": "drv-001"
}
```

**Usage**: POST `/bookings/{id}/assign-driver` request body

---

## ?? Quote Models

### QuoteDetailDto

**Purpose**: Complete quote information for viewing and editing

**File**: `Models/QuoteModels.cs`

**Properties**:
```csharp
public class QuoteDetailDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string? Status { get; set; }
    public string BookerName { get; set; } = string.Empty;
    public string BookerEmail { get; set; } = string.Empty;
    public string? BookerPhone { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string? PassengerPhone { get; set; }
    public string VehicleClass { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string? DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public int PassengerCount { get; set; }
    public int Luggage { get; set; }
    public string? SpecialRequests { get; set; }
    public decimal? QuotedPrice { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Phase 1: Audit trail
    public string? CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
}
```

**JSON Example**:
```json
{
  "id": "qt-2025-001",
  "createdUtc": "2025-01-18T08:00:00Z",
  "status": "Submitted",
  "bookerName": "Sarah Johnson",
  "bookerEmail": "sarah@company.com",
  "bookerPhone": "(312) 555-1111",
  "passengerName": "Robert Chen",
  "passengerPhone": "(312) 555-2222",
  "vehicleClass": "Sedan",
  "pickupLocation": "Langham Hotel Chicago",
  "dropoffLocation": "Midway Airport",
  "pickupDateTime": "2025-01-25T10:00:00Z",
  "passengerCount": 2,
  "luggage": 2,
  "specialRequests": null,
  "quotedPrice": null,
  "adminNotes": null,
  "updatedUtc": null,
  "updatedBy": null,
  "createdByUserId": "user-123",
  "modifiedByUserId": null,
  "modifiedOnUtc": null
}
```

**Status Values**:
- `Submitted` - New quote request
- `InReview` - Admin reviewing
- `Priced` - Quote price provided (triggers email)
- `Rejected` - Quote declined
- `Closed` - Quote expired or customer booked

**Usage**: 
- GET `/quotes/list` - Returns array of QuoteDetailDto
- GET `/quotes/{id}` - Returns single QuoteDetailDto

---

### UpdateQuoteDto

**Purpose**: Update quote pricing, status, or admin notes

**File**: `Models/QuoteModels.cs`

**Properties**:
```csharp
public class UpdateQuoteDto
{
    public decimal? QuotedPrice { get; set; }
    public string? Status { get; set; }
    public string? AdminNotes { get; set; }
}
```

**JSON Example**:
```json
{
  "quotedPrice": 150.00,
  "status": "Priced",
  "adminNotes": "Standard airport transfer rate"
}
```

**Validation**:
- `quotedPrice`: Must be positive if provided
- `status`: Must be valid status value
- All fields optional (partial updates supported)

**Usage**: PUT `/quotes/{id}` request body

---

## ?? Affiliate & Driver Models

### AffiliateDto

**Purpose**: Affiliate company information with nested drivers

**File**: `Models/AffiliateModels.cs`

**Properties**:
```csharp
public class AffiliateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PointOfContact { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public List<DriverDto> Drivers { get; set; } = new();
}
```

**JSON Example**:
```json
{
  "id": "aff-001",
  "name": "Chicago Limo Service",
  "pointOfContact": "John Smith",
  "phone": "(312) 555-1234",
  "email": "dispatch@chicagolimo.com",
  "streetAddress": "123 Main St",
  "city": "Chicago",
  "state": "IL",
  "drivers": [
    {
      "id": "drv-001",
      "affiliateId": "aff-001",
      "name": "Michael Johnson",
      "phone": "(312) 555-0001",
      "userUid": "driver-001"
    },
    {
      "id": "drv-002",
      "affiliateId": "aff-001",
      "name": "Sarah Lee",
      "phone": "(312) 555-0002",
      "userUid": "driver-002"
    }
  ]
}
```

**Validation**:
- `name`, `phone`, `email` are required
- `email` must be valid email format
- `drivers` automatically initialized to empty list

**Usage**:
- GET `/affiliates/list` - Returns array of AffiliateDto
- GET `/affiliates/{id}` - Returns single AffiliateDto
- POST `/affiliates` - Create new (exclude `drivers` array)
- PUT `/affiliates/{id}` - Update existing

---

### DriverDto

**Purpose**: Driver information within an affiliate

**File**: `Models/AffiliateModels.cs`

**Properties**:
```csharp
public class DriverDto
{
    public string Id { get; set; } = string.Empty;
    public string AffiliateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? UserUid { get; set; }  // AuthServer identity link
}
```

**JSON Example**:
```json
{
  "id": "drv-001",
  "affiliateId": "aff-001",
  "name": "Michael Johnson",
  "phone": "(312) 555-0001",
  "userUid": "driver-001"
}
```

**Field Descriptions**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Driver unique identifier |
| `affiliateId` | string | Yes | Parent affiliate ID |
| `name` | string | Yes | Driver full name |
| `phone` | string | Yes | Driver phone number |
| `userUid` | string | No | AuthServer user UID (for DriverApp login) |

**UserUid Purpose**: Links driver to AuthServer identity
- Enables driver login to DriverApp
- Driver can see assigned bookings via `assignedDriverUid` match
- Optional but recommended for active drivers

**Usage**: POST `/affiliates/{affiliateId}/drivers` to add driver

---

## ?? Location & Tracking Models

### LocationUpdate

**Purpose**: Real-time GPS location update from driver (SignalR event)

**File**: `Models/DriverTrackingModels.cs`

**Properties**:
```csharp
public class LocationUpdate
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Heading { get; set; }       // 0-360°, 0 = North
    public double? Speed { get; set; }         // meters/second
    public double? Accuracy { get; set; }      // meters
    public string? DriverName { get; set; }
    public string? DriverUid { get; set; }
}
```

**JSON Example**:
```json
{
  "rideId": "bk-2025-001",
  "latitude": 41.9742,
  "longitude": -87.9073,
  "timestamp": "2025-01-18T10:30:00Z",
  "heading": 45.5,
  "speed": 24.58,
  "accuracy": 8.5,
  "driverName": "Michael Johnson",
  "driverUid": "driver-001"
}
```

**Field Details**:
- **Heading**: Cardinal direction (0° = North, 90° = East, 180° = South, 270° = West)
- **Speed**: Convert to mph: `speed * 2.23694`, to km/h: `speed * 3.6`
- **Accuracy**: GPS precision (lower = better, typical: 5-20 meters)

**Usage**: SignalR event `LocationUpdate` from AdminAPI hub

**See**: [SignalR Reference](21-SignalR-Reference.md) for event details

---

### LocationResponse

**Purpose**: REST API location response (polling fallback)

**File**: `Models/DriverTrackingModels.cs`

**Properties**:
```csharp
public class LocationResponse
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double AgeSeconds { get; set; }     // How old is this data?
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
}
```

**JSON Example**:
```json
{
  "rideId": "bk-2025-001",
  "latitude": 41.9742,
  "longitude": -87.9073,
  "timestamp": "2025-01-18T10:29:45Z",
  "heading": 45.5,
  "speed": 24.58,
  "accuracy": 8.5,
  "ageSeconds": 15.3,
  "driverUid": "driver-001",
  "driverName": "Michael Johnson"
}
```

**AgeSeconds Calculation**:
```csharp
AgeSeconds = (DateTime.UtcNow - Timestamp).TotalSeconds
```

**Staleness Indicators**:
- `< 30s` - Fresh, real-time
- `30-60s` - Acceptable
- `> 60s` - Stale, driver may have stopped updating

**Usage**: GET `/driver/location/{rideId}` response

---

### ActiveRideLocationDto

**Purpose**: Active ride with location for admin dashboard

**File**: `Models/DriverTrackingModels.cs`

**Properties**:
```csharp
public class ActiveRideLocationDto
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
    public string? PassengerName { get; set; }
    public string? PickupLocation { get; set; }
    public string? DropoffLocation { get; set; }
    public string? Status { get; set; }           // Legacy booking status
    public string? CurrentStatus { get; set; }    // Real-time driver status
    public double AgeSeconds { get; set; }
}
```

**JSON Example**:
```json
{
  "rideId": "bk-2025-001",
  "latitude": 41.9742,
  "longitude": -87.9073,
  "timestamp": "2025-01-18T10:29:45Z",
  "heading": 45.5,
  "speed": 24.58,
  "accuracy": 8.5,
  "driverUid": "driver-001",
  "driverName": "Michael Johnson",
  "passengerName": "Taylor Reed",
  "pickupLocation": "O'Hare FBO",
  "dropoffLocation": "Langham Hotel",
  "status": "InProgress",
  "currentStatus": "OnRoute",
  "ageSeconds": 15.3
}
```

**Status vs CurrentStatus**:
- `Status`: Booking-level status (InProgress, Completed)
- `CurrentStatus`: Real-time driver status (OnRoute, Arrived, PassengerOnboard)
- **Prefer `CurrentStatus`** for displaying current ride state

**Usage**: GET `/admin/locations` response (wrapped in envelope)

---

### LocationsResponse

**Purpose**: Envelope for bulk location response

**File**: `Models/DriverTrackingModels.cs`

**Properties**:
```csharp
public class LocationsResponse
{
    public int Count { get; set; }
    public List<ActiveRideLocationDto> Locations { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
```

**JSON Example**:
```json
{
  "count": 3,
  "timestamp": "2025-01-18T10:30:00Z",
  "locations": [
    {
      "rideId": "bk-2025-001",
      "latitude": 41.9742,
      "longitude": -87.9073,
      "driverName": "Michael Johnson",
      "passengerName": "Taylor Reed",
      "currentStatus": "OnRoute",
      "ageSeconds": 15
    }
  ]
}
```

**Usage**: GET `/admin/locations` response

---

### RideStatusChangedEvent

**Purpose**: SignalR event when driver changes ride status

**File**: `Models/DriverTrackingModels.cs`

**Properties**:
```csharp
public class RideStatusChangedEvent
{
    public string RideId { get; set; } = string.Empty;
    public string DriverUid { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? PassengerName { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

**JSON Example**:
```json
{
  "rideId": "bk-2025-001",
  "driverUid": "driver-001",
  "driverName": "Michael Johnson",
  "passengerName": "Taylor Reed",
  "newStatus": "Arrived",
  "timestamp": "2025-01-18T10:30:00Z"
}
```

**Valid NewStatus Values**:
- `Accepted` - Driver accepted ride
- `OnRoute` - En route to pickup
- `Arrived` - Arrived at pickup
- `PassengerOnboard` - Passenger in vehicle
- `DropoffComplete` - Ride completed
- `Cancelled` - Ride cancelled

**Usage**: SignalR event `RideStatusChanged` from AdminAPI hub

**See**: [SignalR Reference](21-SignalR-Reference.md)

---

## ?? Authentication Models

### LoginRequest

**Purpose**: User login credentials

**Properties**:
```csharp
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**JSON Example**:
```json
{
  "username": "alice",
  "password": "password"
}
```

**Usage**: POST `/api/auth/login` request to AuthServer

---

### LoginResponse

**Purpose**: JWT authentication tokens

**Properties**:
```csharp
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }  // Seconds
}
```

**JSON Example**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "8f7d6e5c4b3a2...",
  "expiresIn": 1800
}
```

**Token Lifetime**:
- `accessToken`: 30 minutes (1800 seconds)
- `refreshToken`: 7 days (used to get new access token)

**Usage**: POST `/api/auth/login` response from AuthServer

---

## ?? Data Relationships

### Entity Relationship Diagram

```
?????????????????
?   Affiliate   ?
? (AffiliateDto)?
?????????????????
        ? 1
        ?
        ? Has Many
        ?
        ? N
?????????????????         ????????????????
?    Driver     ? Assigns ?   Booking    ?
?  (DriverDto)  ?????????>?(BookingDto)  ?
????????????????? 0..N    ????????????????
                          
      UserUid Link          CreatedByUserId
         ?                        ?
         ?                        ?
         ?                        ?
??????????????????          ????????????????
?   AuthServer   ?          ?  Staff User  ?
?   User (UID)   ?          ?   (Admin,    ?
?                ?          ?  Dispatcher) ?
??????????????????          ????????????????


????????????????
?    Quote     ?
? (QuoteDto)   ?
????????????????
       ? Can Convert To
       ?
       ?
????????????????         ????????????????
?   Booking    ? Tracked ?  Location    ?
?              ?????????>?   Update     ?
???????????????? 1..N    ????????????????
```

---

### Key Relationships

**Affiliate ? Driver** (1:N)
- One affiliate has many drivers
- Cascade delete: Deleting affiliate deletes drivers
- Driver assignment links booking to driver

**Driver ? Booking** (1:N Assignment)
- One driver can be assigned to many bookings
- `Booking.AssignedDriverId` ? `Driver.Id`
- `Booking.AssignedDriverUid` ? `Driver.UserUid` (for app filtering)

**Driver ? AuthServer User** (1:1 via UserUid)
- `Driver.UserUid` links to AuthServer user identity
- Enables driver login to DriverApp
- Optional but recommended for active drivers

**Booking ? Location Updates** (1:N)
- One booking (ride) has many location updates
- Updated every 5-10 seconds while ride is active
- Linked via `LocationUpdate.RideId` ? `Booking.Id`

**Quote ? Booking** (1:1 Conversion)
- Quote can be converted to booking (manual process)
- Not a database foreign key relationship
- Both contain similar trip information

---

## ?? Field Conventions

### Naming Conventions

| Convention | Example | Usage |
|------------|---------|-------|
| `Id` suffix | `BookingId`, `DriverId` | Unique identifiers |
| `Utc` suffix | `CreatedUtc`, `UpdatedUtc` | UTC timestamps |
| `?` (nullable) | `string?`, `int?` | Optional fields |
| `Dto` suffix | `BookingDto`, `QuoteDto` | Data Transfer Objects |
| `Event` suffix | `RideStatusChangedEvent` | SignalR events |

---

### Date/Time Handling

**UTC Storage**: All timestamps stored as UTC

```csharp
public DateTime CreatedUtc { get; set; }  // Always UTC
```

**Timezone-Aware Display** (Phase 1):
```csharp
public DateTimeOffset PickupDateTimeOffset { get; set; }  // With timezone offset
```

**Conversion**:
```csharp
// UTC to Local (for display)
var localTime = CreatedUtc.ToLocalTime();

// DateTimeOffset to UTC
var utc = PickupDateTimeOffset.UtcDateTime;
```

---

### Nullable Reference Types

**C# 8.0+ Enabled**: All projects use nullable reference types

**Convention**:
- `string Name` - Required, never null
- `string? Name` - Optional, may be null

**Example**:
```csharp
public class BookingDto
{
    public string Id { get; set; } = string.Empty;      // Required
    public string? SpecialRequests { get; set; }        // Optional
}
```

---

## ? Validation Rules

### Common Validations

**Email**:
```csharp
[EmailAddress]
public string Email { get; set; }
```

**Required Fields**:
```csharp
[Required]
public string Name { get; set; }
```

**String Length**:
```csharp
[StringLength(100, MinimumLength = 2)]
public string Name { get; set; }
```

**Range**:
```csharp
[Range(1, 10)]
public int PassengerCount { get; set; }
```

---

### Business Validations

**Booking**:
- `PassengerCount`: 1-15
- `LuggageCount`: 0-30
- `PickupDateTime`: Must be in future
- `AssignedDriverId`: Must exist in drivers table

**Quote**:
- `QuotedPrice`: Must be > 0 if set
- `Status`: Must be valid status value

**Affiliate**:
- `Email`: Must be valid email format
- `Name`: Must be unique (case-insensitive)

---

## ?? Related Documentation

- [API Reference](20-API-Reference.md) - Endpoint request/response examples
- [SignalR Reference](21-SignalR-Reference.md) - Real-time event payloads
- [System Architecture](01-System-Architecture.md) - How models flow through system
- [Security Model](23-Security-Model.md) - Authentication & audit fields

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This data models reference documents all DTOs used in AdminPortal and AdminAPI. Keep this updated when adding or modifying models.* ???
