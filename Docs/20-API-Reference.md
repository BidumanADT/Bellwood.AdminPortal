# AdminAPI Reference

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document catalogs all AdminAPI endpoints consumed by the Bellwood AdminPortal, including request/response formats, authentication requirements, and usage examples.

**AdminAPI Base URL**:
- Development: `https://localhost:5206`
- Production: `https://api.bellwood.com` (configure in appsettings)

**Authentication**: All endpoints require:
- `X-Admin-ApiKey` header (API key authentication)
- `Authorization: Bearer {token}` header (JWT authentication)

**Target Audience**: Developers, API team, integration engineers  
**Prerequisites**: Understanding of REST APIs, HTTP methods, JSON

---

## ?? Authentication & Headers

### Required Headers

**Every Request Must Include**:

```http
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

### Header Configuration

**AdminPortal** (`appsettings.Development.json`):
```json
{
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": "dev-secret-123"
  }
}
```

**AdminAPI** (validates these headers):
- API Key validation in middleware
- JWT Bearer token validation
- Role-based authorization checks

---

## ?? Bookings Endpoints

### GET /bookings/list

**Purpose**: Retrieve list of bookings with optional filtering

**Method**: `GET`

**URL**: `/bookings/list?take={count}&skip={offset}&status={status}`

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `take` | int | No | 100 | Number of bookings to return |
| `skip` | int | No | 0 | Number of bookings to skip (pagination) |
| `status` | string | No | null | Filter by status (Requested, Confirmed, Scheduled, etc.) |

**Request Example**:
```http
GET /bookings/list?take=50&skip=0&status=Requested HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
```json
[
  {
    "id": "bk-2025-001",
    "status": "Requested",
    "currentRideStatus": null,
    "passengerName": "Taylor Reed",
    "passengerPhone": "(312) 555-7890",
    "bookerName": "Sarah Johnson",
    "bookerEmail": "sarah@company.com",
    "pickupLocation": "O'Hare FBO Terminal",
    "dropoffLocation": "Langham Hotel Chicago",
    "pickupDateTime": "2025-01-20T14:00:00Z",
    "pickupDateTimeOffset": "2025-01-20T08:00:00-06:00",
    "vehicleClass": "SUV",
    "passengerCount": 3,
    "luggageCount": 5,
    "specialRequests": "Meet at FBO main entrance",
    "assignedDriverId": null,
    "assignedDriverName": null,
    "createdUtc": "2025-01-15T10:00:00Z"
  }
]
```

**Response Fields**:
- `currentRideStatus`: Real-time driver status (OnRoute, Arrived, PassengerOnboard) - **null if not tracking**
- `status`: Booking-level status (Requested, Confirmed, Scheduled, InProgress, Completed, Cancelled)
- `pickupDateTimeOffset`: Timezone-aware pickup time (**Phase 1**)
- `assignedDriverId`, `assignedDriverName`: Driver assignment details

**Authorization**: Requires authenticated user (admin, dispatcher, or booker)

**Role-Based Filtering** (Phase 2):
- **booker** role: Returns only bookings created by that user (`createdByUserId` match)
- **admin/dispatcher** role: Returns all bookings

---

### GET /bookings/{id}

**Purpose**: Retrieve detailed information for a specific booking

**Method**: `GET`

**URL**: `/bookings/{id}`

**Path Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | string | Yes | Booking unique identifier |

**Request Example**:
```http
GET /bookings/bk-2025-001 HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
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

**Error Responses**:
- `404 Not Found`: Booking ID doesn't exist
- `403 Forbidden`: User doesn't have permission to view this booking (**Phase 2**)

**Authorization**: Requires authenticated user

**Access Control** (Phase 2):
- **booker**: Can only view bookings they created
- **admin/dispatcher**: Can view all bookings

---

### POST /bookings/{id}/assign-driver

**Purpose**: Assign a driver to a booking

**Method**: `POST`

**URL**: `/bookings/{id}/assign-driver`

**Request Body**:
```json
{
  "driverId": "drv-001"
}
```

**Request Example**:
```http
POST /bookings/bk-2025-001/assign-driver HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "driverId": "drv-001"
}
```

**Response** (`200 OK`):
```json
{
  "success": true,
  "booking": {
    "id": "bk-2025-001",
    "assignedDriverId": "drv-001",
    "assignedDriverName": "Michael Johnson",
    "assignedDriverUid": "driver-001",
    "status": "Scheduled"
  }
}
```

**Side Effects**:
1. Updates booking: `assignedDriverId`, `assignedDriverName`, `assignedDriverUid`
2. Changes status to `Scheduled` (if currently `Requested` or `Confirmed`)
3. Sends email notification to affiliate with driver assignment details
4. Updates `modifiedByUserId` and `modifiedOnUtc` (**Phase 1**)

**Email Sent To**: Affiliate email (e.g., `dispatch@chicagolimo.com`)

**Email Content**:
```
Subject: Bellwood Elite - Driver Assignment

Hello Chicago Limo Service Team,

A driver from your affiliate has been assigned to a new booking.

Driver Information:
  Name: Michael Johnson
  Phone: (312) 555-0001

Booking Details:
  Passenger: Taylor Reed
  Pickup: O'Hare FBO Terminal
  Dropoff: Langham Hotel Chicago
  Date/Time: January 20, 2025 at 8:00 AM CST
  Vehicle: SUV
  Passengers: 3
  Luggage: 5

Special Instructions:
Meet at FBO main entrance

Booking ID: bk-2025-001

Thank you,
Bellwood Global Team
```

**Authorization**: Requires admin or dispatcher role

---

### POST /bookings/seed

**Purpose**: Seed test booking data (development only)

**Method**: `POST`

**URL**: `/bookings/seed`

**Response** (`200 OK`):
```json
{
  "added": 3
}
```

**Authorization**: No authentication required (development endpoint only)

**?? Remove in production**

---

## ?? Quotes Endpoints

### GET /quotes/list

**Purpose**: Retrieve list of quote requests

**Method**: `GET`

**URL**: `/quotes/list?take={count}&status={status}`

**Query Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `take` | int | No | 100 | Number of quotes to return |
| `status` | string | No | null | Filter by status (Submitted, InReview, Priced, Rejected, Closed) |

**Request Example**:
```http
GET /quotes/list?take=50&status=Submitted HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
```json
[
  {
    "id": "qt-2025-001",
    "status": "Submitted",
    "quotedPrice": null,
    "bookerName": "Sarah Johnson",
    "bookerEmail": "sarah@company.com",
    "bookerPhone": "(312) 555-1111",
    "passengerName": "Robert Chen",
    "passengerPhone": "(312) 555-2222",
    "pickupLocation": "Langham Hotel Chicago",
    "dropoffLocation": "Midway Airport",
    "pickupDateTime": "2025-01-25T10:00:00Z",
    "pickupDateTimeOffset": "2025-01-25T04:00:00-06:00",
    "vehicleClass": "Sedan",
    "passengerCount": 2,
    "luggageCount": 2,
    "specialRequests": null,
    "adminNotes": null,
    "createdUtc": "2025-01-18T08:00:00Z",
    "updatedUtc": null
  }
]
```

**Authorization**: Requires authenticated user (admin or dispatcher)

**Role-Based Filtering** (Phase 2):
- **booker**: Returns only quotes created by that user
- **admin/dispatcher**: Returns all quotes

---

### GET /quotes/{id}

**Purpose**: Retrieve detailed information for a specific quote

**Method**: `GET`

**URL**: `/quotes/{id}`

**Response** (`200 OK`):
```json
{
  "id": "qt-2025-001",
  "status": "Submitted",
  "quotedPrice": null,
  "bookerName": "Sarah Johnson",
  "bookerEmail": "sarah@company.com",
  "bookerPhone": "(312) 555-1111",
  "passengerName": "Robert Chen",
  "passengerPhone": "(312) 555-2222",
  "pickupLocation": "Langham Hotel Chicago",
  "dropoffLocation": "Midway Airport",
  "pickupDateTime": "2025-01-25T10:00:00Z",
  "pickupDateTimeOffset": "2025-01-25T04:00:00-06:00",
  "vehicleClass": "Sedan",
  "passengerCount": 2,
  "luggageCount": 2,
  "specialRequests": null,
  "adminNotes": null,
  "createdUtc": "2025-01-18T08:00:00Z",
  "updatedUtc": null,
  "createdByUserId": "user-123",
  "modifiedByUserId": null,
  "modifiedOnUtc": null
}
```

**Error Responses**:
- `404 Not Found`: Quote ID doesn't exist
- `403 Forbidden`: User doesn't have permission to view this quote (**Phase 2**)

**Authorization**: Requires authenticated user

---

### PUT /quotes/{id}

**Purpose**: Update quote pricing, status, or admin notes

**Method**: `PUT`

**URL**: `/quotes/{id}`

**Request Body**:
```json
{
  "quotedPrice": 150.00,
  "status": "Priced",
  "adminNotes": "Standard airport transfer rate"
}
```

**Request Example**:
```http
PUT /quotes/qt-2025-001 HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "quotedPrice": 150.00,
  "status": "Priced",
  "adminNotes": "Standard airport transfer rate"
}
```

**Response** (`200 OK`):
```json
{
  "success": true,
  "quote": {
    "id": "qt-2025-001",
    "quotedPrice": 150.00,
    "status": "Priced",
    "updatedUtc": "2025-01-18T10:00:00Z"
  }
}
```

**Side Effects**:
1. Updates quote: `quotedPrice`, `status`, `adminNotes`
2. If status changed to `"Priced"`, sends email notification to customer
3. Updates `modifiedByUserId` and `modifiedOnUtc` (**Phase 1**)

**Email Trigger**: Status change to `"Priced"`

**Email Content** (sent to `bookerEmail`):
```
Subject: Your Quote is Ready - Bellwood Global

Dear Sarah,

Your quote request for:
- Pickup: Langham Hotel Chicago
- Dropoff: Midway Airport
- Date: January 25, 2025 4:00 AM

Quoted Price: $150.00

[Accept Quote] [View Details] [Decline]

This quote is valid for 48 hours.

Thank you,
Bellwood Global Team
```

**Authorization**: Requires admin or dispatcher role

**Error Responses**:
- `403 Forbidden`: User doesn't have permission to update quotes (**Phase 2**)

---

## ?? Affiliates Endpoints

### GET /affiliates/list

**Purpose**: Retrieve list of all affiliate companies

**Method**: `GET`

**URL**: `/affiliates/list`

**Request Example**:
```http
GET /affiliates/list HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
```json
[
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
]
```

**Response Fields**:
- `drivers`: Array of drivers associated with this affiliate
- `userUid`: Links driver to AuthServer identity (for DriverApp login)

**Authorization**: Requires admin or dispatcher role

---

### GET /affiliates/{id}

**Purpose**: Retrieve detailed information for a specific affiliate

**Method**: `GET`

**URL**: `/affiliates/{id}`

**Response** (`200 OK`):
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
  "drivers": [...]
}
```

**Error Responses**:
- `404 Not Found`: Affiliate ID doesn't exist

---

### POST /affiliates

**Purpose**: Create a new affiliate company

**Method**: `POST`

**URL**: `/affiliates`

**Request Body**:
```json
{
  "name": "Elite Transportation",
  "pointOfContact": "Tom Wilson",
  "phone": "(630) 555-4567",
  "email": "dispatch@elite.com",
  "streetAddress": "456 Oak Ave",
  "city": "Oakbrook",
  "state": "IL"
}
```

**Response** (`201 Created`):
```json
{
  "id": "aff-003"
}
```

**Validation**:
- `name` (required)
- `phone` (required)
- `email` (required, must be valid email format)

**Authorization**: Requires admin role

---

### PUT /affiliates/{id}

**Purpose**: Update affiliate information

**Method**: `PUT`

**URL**: `/affiliates/{id}`

**Request Body**:
```json
{
  "name": "Elite Transportation Services",
  "pointOfContact": "Tom Wilson",
  "phone": "(630) 555-4567",
  "email": "dispatch@elite.com",
  "streetAddress": "789 New Address",
  "city": "Oakbrook",
  "state": "IL"
}
```

**Response** (`200 OK`):
```json
{
  "success": true
}
```

**Authorization**: Requires admin role

---

### DELETE /affiliates/{id}

**Purpose**: Delete an affiliate and all associated drivers (cascade)

**Method**: `DELETE`

**URL**: `/affiliates/{id}`

**Response** (`200 OK`):
```json
{
  "success": true,
  "deletedDrivers": 2
}
```

**Side Effects**:
- Deletes affiliate record
- **Cascade deletes** all drivers associated with this affiliate
- Does **not** delete booking assignments (historical data preserved)

**?? Warning**: This action cannot be undone

**Authorization**: Requires admin role

---

## ?? Drivers Endpoints

### POST /affiliates/{affiliateId}/drivers

**Purpose**: Add a new driver to an affiliate

**Method**: `POST`

**URL**: `/affiliates/{affiliateId}/drivers`

**Request Body**:
```json
{
  "name": "Charlie Johnson",
  "phone": "(312) 555-CHAS",
  "userUid": "charlie-uid-001"
}
```

**Request Fields**:
- `name` (required): Driver full name
- `phone` (required): Driver phone number
- `userUid` (optional): AuthServer identity UID (for DriverApp login)

**Response** (`201 Created`):
```json
{
  "id": "drv-004"
}
```

**Authorization**: Requires admin or dispatcher role

---

### GET /drivers/{id}

**Purpose**: Retrieve driver information

**Method**: `GET`

**URL**: `/drivers/{id}`

**Response** (`200 OK`):
```json
{
  "id": "drv-001",
  "affiliateId": "aff-001",
  "name": "Michael Johnson",
  "phone": "(312) 555-0001",
  "userUid": "driver-001"
}
```

---

### PUT /drivers/{id}

**Purpose**: Update driver information

**Method**: `PUT`

**URL**: `/drivers/{id}`

**Request Body**:
```json
{
  "name": "Michael J. Johnson",
  "phone": "(312) 555-0001",
  "userUid": "driver-001"
}
```

**Authorization**: Requires admin or dispatcher role

---

### DELETE /drivers/{id}

**Purpose**: Delete a driver

**Method**: `DELETE`

**URL**: `/drivers/{id}`

**Response** (`200 OK`):
```json
{
  "success": true
}
```

**?? Note**: Does not cascade delete booking assignments (historical data preserved)

**Authorization**: Requires admin role

---

## ?? Location & Tracking Endpoints

### GET /admin/locations

**Purpose**: Get all active driver locations (admin dashboard)

**Method**: `GET`

**URL**: `/admin/locations`

**Request Example**:
```http
GET /admin/locations HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
```json
{
  "count": 3,
  "timestamp": "2025-01-18T10:30:00Z",
  "locations": [
    {
      "rideId": "bk-2025-001",
      "driverName": "Michael Johnson",
      "passengerName": "Taylor Reed",
      "pickupLocation": "O'Hare FBO",
      "dropoffLocation": "Langham Hotel",
      "currentStatus": "OnRoute",
      "latitude": 41.9742,
      "longitude": -87.9073,
      "speed": 55.0,
      "lastUpdate": "2025-01-18T10:29:45Z",
      "timeSince": "15 seconds ago"
    }
  ]
}
```

**Authorization**: Requires admin role

**Purpose**: Used by Live Tracking page for initial map load

---

### GET /driver/location/{rideId}

**Purpose**: Get current location for a specific ride

**Method**: `GET`

**URL**: `/driver/location/{rideId}`

**Response** (`200 OK`):
```json
{
  "rideId": "bk-2025-001",
  "latitude": 41.9742,
  "longitude": -87.9073,
  "heading": 45.5,
  "speed": 55.0,
  "accuracy": 8.5,
  "timestamp": "2025-01-18T10:29:45Z",
  "timeSince": "15 seconds ago"
}
```

**Error Responses**:
- `404 Not Found`: No location data for this ride
- `403 Forbidden`: User doesn't have permission to view this location (**Phase 2**)

**Authorization**: Requires authenticated user

**Access Control** (Phase 2):
- **booker**: Can view location for their own bookings
- **admin/dispatcher**: Can view all locations

---

### GET /admin/locations/rides

**Purpose**: Get locations for multiple rides (batch query)

**Method**: `GET`

**URL**: `/admin/locations/rides?rideIds={id1},{id2},{id3}`

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `rideIds` | string | Yes | Comma-separated list of ride IDs |

**Request Example**:
```http
GET /admin/locations/rides?rideIds=bk-2025-001,bk-2025-002 HTTP/1.1
Host: localhost:5206
X-Admin-ApiKey: dev-secret-123
Authorization: Bearer eyJ...
```

**Response** (`200 OK`):
```json
[
  {
    "rideId": "bk-2025-001",
    "latitude": 41.9742,
    "longitude": -87.9073,
    "speed": 55.0,
    "timestamp": "2025-01-18T10:29:45Z"
  },
  {
    "rideId": "bk-2025-002",
    "latitude": 41.8781,
    "longitude": -87.6298,
    "speed": 35.0,
    "timestamp": "2025-01-18T10:30:00Z"
  }
]
```

**Authorization**: Requires admin or dispatcher role

---

### POST /driver/location/update

**Purpose**: Driver sends GPS location update (called by DriverApp)

**Method**: `POST`

**URL**: `/driver/location/update`

**Request Body**:
```json
{
  "rideId": "bk-2025-001",
  "latitude": 41.9742,
  "longitude": -87.9073,
  "heading": 45.5,
  "speed": 55.0,
  "accuracy": 8.5
}
```

**Response** (`200 OK`):
```json
{
  "success": true,
  "timestamp": "2025-01-18T10:30:00Z"
}
```

**Side Effects**:
1. Persists location to storage
2. Broadcasts `LocationUpdate` event via SignalR to subscribed admin users
3. Updates `lastUpdate` timestamp

**Authorization**: Requires driver role (driver can only update their assigned rides)

**See**: [DriverApp Integration Guide](Archive/DRIVER_APP_COMPLETE_INTEGRATION_GUIDE.md) for complete implementation

---

## ?? Health & Diagnostics

### GET /health

**Purpose**: Health check endpoint

**Method**: `GET`

**URL**: `/health`

**Response** (`200 OK`):
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-18T10:30:00Z"
}
```

**Authorization**: None (public endpoint)

**Use Cases**:
- Monitoring systems (Pingdom, StatusCake)
- Load balancer health checks
- Deployment verification
- Development testing

---

## ?? Error Responses

### Standard HTTP Status Codes

| Code | Name | Meaning |
|------|------|---------|
| `200` | OK | Request successful |
| `201` | Created | Resource created successfully |
| `400` | Bad Request | Invalid request data (validation failed) |
| `401` | Unauthorized | Missing or invalid authentication |
| `403` | Forbidden | User lacks permission for this resource |
| `404` | Not Found | Resource doesn't exist |
| `500` | Internal Server Error | Server-side error |

---

### Error Response Format

**401 Unauthorized**:
```json
{
  "error": "Unauthorized",
  "message": "Missing or invalid authentication token"
}
```

**403 Forbidden** (Phase 2):
```json
{
  "error": "Forbidden",
  "message": "Access denied. You do not have permission to view this resource.",
  "details": {
    "userId": "user-123",
    "resourceId": "bk-2025-001",
    "requiredRole": "admin"
  }
}
```

**404 Not Found**:
```json
{
  "error": "NotFound",
  "message": "Booking with ID 'bk-invalid' not found"
}
```

**400 Bad Request** (Validation):
```json
{
  "error": "ValidationFailed",
  "message": "One or more validation errors occurred",
  "errors": {
    "name": ["The Name field is required."],
    "email": ["Invalid email format."]
  }
}
```

---

## ?? Rate Limiting

**Current Implementation**: None

**Planned** (Future Enhancement):
- 100 requests/minute per user
- 1000 requests/minute per API key
- 429 Too Many Requests response when exceeded

---

## ?? Development Utilities

### Seed Endpoints

**?? Development Only - Remove in Production**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/bookings/seed` | POST | Seed 3 test bookings |
| `/quotes/seed` | POST | Seed test quotes |
| `/affiliates/seed` | POST | Seed default affiliates & drivers |

**Usage**:
```powershell
# Seed via PowerShell script
.\Scripts\seed-admin-api.ps1
```

---

## ?? Related Documentation

- [SignalR Reference](21-SignalR-Reference.md) - Real-time events and hub methods
- [Data Models](22-Data-Models.md) - Request/response DTOs
- [Security Model](23-Security-Model.md) - Authentication & authorization
- [Testing Guide](02-Testing-Guide.md) - API testing procedures
- [Driver App Integration](Archive/DRIVER_APP_COMPLETE_INTEGRATION_GUIDE.md) - Mobile app endpoints

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This API reference documents all endpoints consumed by the AdminPortal. When adding new endpoints, update this document to maintain accuracy.* ???
