# Bellwood AdminPortal

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=.net)
![Framework](https://img.shields.io/badge/framework-Blazor%20Server-blue?style=flat-square)
![Status](https://img.shields.io/badge/status-Production%20Ready-success?style=flat-square)
![License](https://img.shields.io/badge/license-Proprietary-red?style=flat-square)

A production-ready dispatch and operations portal for the Bellwood Global chauffeur and limousine management system, providing real-time driver tracking, booking management, and fleet coordination for worldwide operations.

## Overview

Bellwood AdminPortal is the **command center** for dispatchers and administrators, enabling:

- ?? **Complete Booking Operations** – View, filter, assign drivers, and manage entire booking lifecycle
- ??? **Real-Time GPS Tracking** – Live driver location via SignalR WebSockets with Google Maps integration
- ?? **Affiliate & Driver Management** – Multi-company fleet coordination with driver assignment
- ?? **Status Monitoring** – Real-time ride status updates (OnRoute, Arrived, PassengerOnboard)
- ??? **Live Map Dashboard** – Interactive tracking map with driver markers and route visualization
- ?? **Timezone Support** – Automatic timezone-aware pickup times for worldwide operations
- ?? **Quote Management** – Complete quote pricing workflow with status updates and customer notifications
- ?? **Secure Authentication** – JWT-based login with role-based authorization
- ?? **Responsive Design** – Premium Bellwood Elite branding with dark theme support

## Architecture

The Bellwood ecosystem consists of five interconnected components:

```
???????????????????    JWT Auth      ????????????????
?   AuthServer    ? ???????????????? ?  AdminAPI    ?
?  (Identity)     ?                  ?  (Backend)   ?
???????????????????                  ????????????????
                                             ?
                     ?????????????????????????????????????????????????
                     ?                       ?                       ?
              ???????????????        ???????????????        ???????????????
              ? AdminPortal ?        ? PassengerApp?        ?  DriverApp  ?
              ? (This Repo) ?        ?   (MAUI)    ?        ?   (MAUI)    ?
              ???????????????        ???????????????        ???????????????
```

### Integration Points

| Component | Technology | Purpose | Authentication |
|-----------|-----------|---------|----------------|
| **AuthServer** | .NET Identity | Issues JWT tokens with role/uid claims | N/A |
| **AdminAPI** | Minimal APIs + SignalR | Backend services, GPS tracking, data storage | JWT (admin/dispatcher role) |
| **AdminPortal** | Blazor Server | **This App** - Dispatch operations and fleet management | JWT (admin role) |
| **PassengerApp** | .NET MAUI | Customer booking and ride tracking | JWT (email claim) |
| **DriverApp** | .NET MAUI | Driver assignments and GPS updates | JWT (driver role + uid) |

## Current Capabilities

### Core Features

- **Authentication & Authorization:** JWT Bearer tokens with `admin` and `dispatcher` roles; secure authentication state provider with automatic token refresh; login/logout flow with protected routes.
- **Booking Management:** Full booking list with filtering (All, Requested, Confirmed, Active, Completed, Cancelled); search by passenger name, location, booker; detailed booking view with passenger info, pickup/dropoff, assigned driver; driver assignment workflow with affiliate selection; booking cancellation support.
- **Real-Time Status Updates:** SignalR subscription on all pages (Bookings, BookingDetail, LiveTracking); instant status changes without manual refresh (OnRoute, Arrived, PassengerOnboard); dual status display (`Status` for reports, `CurrentRideStatus` for real-time driver state); automatic filter updates when status changes.
- **Quote Management:** Complete quote workflow with detail view; interactive pricing and status management; filter by status (Submitted, InReview, Priced, Rejected, Closed); admin notes for internal tracking; customer notifications when quotes are priced; quick action buttons for common tasks; quote-to-booking conversion support.
- **Affiliate & Driver Management:** View affiliate companies with driver lists; add new affiliates and drivers; assign drivers to bookings; driver UserUid linking to AuthServer for app access.

### Real-Time Tracking Features

- **Live Tracking Dashboard:** Interactive Google Maps with dark theme matching Bellwood branding; real-time driver location markers with car icons; driver list sidebar with ride details (passenger, pickup/dropoff, status, speed); connection status indicator (SignalR vs polling); auto-fit map bounds to show all active drivers; click-to-zoom on selected ride.
- **SignalR Real-Time Updates:** `LocationUpdate` events (GPS coordinates from drivers); `RideStatusChanged` events (driver state changes); `TrackingStopped` events (ride completion); automatic reconnection on network interruption; polling fallback (15-second intervals) if SignalR unavailable.
- **Booking Integration:** Live tracking card on BookingDetail page; "View on Live Map" quick navigation; tracking indicators (??) on trackable rides; "Active" filter shows rides with active GPS tracking; automatic location loading when ride becomes trackable.
- **Admin Location Access:** View all active driver locations simultaneously; batch query specific rides; see location age (staleness detection); driver name, passenger name, pickup/dropoff context; current speed and heading display.

### Dashboard Features

- **Main Dashboard:** Recent bookings overview with status summary; quick stats (Total Bookings, Active Rides, Pending Quotes, Drivers Available); quick actions (New Booking, View Live Map, Manage Affiliates); upcoming rides preview.
- **Bookings Dashboard:** Filterable list with status badges; search across passenger names and locations; "Active" filter for tracking rides; live status updates via SignalR; tracking indicators for active GPS; one-click navigation to booking details or live map.
- **Booking Detail Page:** Complete passenger and booker information; pickup/dropoff addresses; flight details (if airport pickup); assigned driver info with UserUid; real-time status badge (prefers `CurrentRideStatus`); live tracking card with coordinates; refresh button for latest location.

## Project Structure

```
Bellwood.AdminPortal/
?? Components/                        # Blazor Components
?   ?? Layout/
?   ?   ?? MainLayout.razor          # Main layout with navigation
?   ?   ?? NavMenu.razor             # Sidebar navigation
?   ?   ?? EmptyLayout.razor         # Login page layout
?   ?? Pages/
?   ?   ?? Home.razor                # Dashboard home page
?   ?   ?? Main.razor                # Main dashboard with stats
?   ?   ?? Login.razor               # Login page
?   ?   ?? Logout.razor              # Logout handler
?   ?   ?? Bookings.razor            # Bookings list with filters + real-time updates
?   ?   ?? BookingDetail.razor       # Booking details with live tracking
?   ?   ?? LiveTracking.razor        # Interactive tracking map + SignalR
?   ?   ?? Quotes.razor              # Quote requests list
?   ?   ?? QuoteDetail.razor         # Quote detail with pricing and status management
?   ?   ?? Affiliates.razor          # Affiliate management
?   ?   ?? AffiliateDetail.razor     # Affiliate details + driver list
?   ?? App.razor                     # Root app component
?   ?? Routes.razor                  # Route definitions
?? Services/                          # Application Services
?   ?? IAuthTokenProvider.cs         # JWT token provider interface
?   ?? AuthTokenProvider.cs          # Stores JWT in browser localStorage
?   ?? JwtAuthenticationStateProvider.cs # Authentication state management
?   ?? IAdminApiKeyProvider.cs       # Admin API key provider
?   ?? IDriverTrackingService.cs     # Driver tracking service interface
?   ?? DriverTrackingService.cs      # SignalR client + location service
?   ?? IAffiliateService.cs          # Affiliate service interface
?   ?? AffiliateService.cs           # Affiliate/driver management service
?   ?? IQuoteService.cs              # Quote service interface
?   ?? QuoteService.cs               # Quote management service
?? Models/                            # Data Models
?   ?? DriverTrackingModels.cs       # LocationUpdate, ActiveRideLocationDto, etc.
?   ?? AffiliateModels.cs            # Affiliate and Driver DTOs
?   ?? QuoteModels.cs                # Quote and UpdateQuote DTOs
?? Auth/                              # Authorization
?   ?? StaffAuthorizeAttribute.cs    # Custom authorize attribute
?? wwwroot/                           # Static Assets
?   ?? css/
?   ?   ?? bellwood.css              # Bellwood branding (gold theme)
?   ?   ?? app.css                   # Base styles
?   ?? js/
?   ?   ?? tracking-map.js           # Google Maps JavaScript interop
?   ?? favicon.ico
?? Docs/                              # Comprehensive Documentation
?   ?? PRODUCTION_DEPLOYMENT_READINESS.md # Deployment checklist
?   ?? ADMINPORTAL_DASHBOARD_REALTIME_UPDATES.md # Dashboard SignalR integration
?   ?? ADMINPORTAL_REALTIME_STATUS_UPDATES.md # LiveTracking implementation
?   ?? DRIVER_TRACKING_ADMINPORTAL_IMPLEMENTATION.md # Complete tracking guide
?   ?? ADMINPORTAL_STATUS_TIMEZONE_INTEGRATION.md # Status + timezone fixes
?   ?? DRIVER_ASSIGNMENT_IMPLEMENTATION.md # Driver assignment workflow
?   ?? QUOTE_MANAGEMENT_IMPLEMENTATION.md # Quote feature implementation guide
?   ?? QUICK_START.md                # Quick start guide
?   ?? [...].md                      # 20+ detailed docs
?? Scripts/                           # PowerShell Test Scripts
?   ?? seed-admin-api.ps1            # Seed AdminAPI with test data
?   ?? test-api-connection.ps1       # Test AdminAPI connectivity
?? appsettings.json                  # Configuration
?? Program.cs                        # Application startup
?? Bellwood.AdminPortal.csproj       # .NET 8 Blazor Server project
```

## Documentation

> **Documentation has been reorganized!** (January 17, 2026)
> 
> All documentation now follows the [Bellwood Documentation Standard](Docs/BELLWOOD-DOCUMENTATION-STANDARD.md).
> 
> **Start here**: [Docs/00-README.md](Docs/00-README.md) - Complete documentation index

### Quick Links

| Document | Description |
|----------|-------------|
| [00-README](Docs/00-README.md) | Complete documentation index & quick start |
| [01-System-Architecture](Docs/01-System-Architecture.md) | Technical design & components |
| [10-Real-Time-Tracking](Docs/10-Real-Time-Tracking.md) | GPS tracking & SignalR |
| [13-User-Access-Control](Docs/13-User-Access-Control.md) | RBAC Phase 1 & 2 |
| [20-API-Reference](Docs/20-API-Reference.md) | AdminAPI endpoints |
| [30-Deployment-Guide](Docs/30-Deployment-Guide.md) | Build & deploy instructions |
| [32-Troubleshooting](Docs/32-Troubleshooting.md) | Common issues & solutions |

### All Documentation

See [Docs/00-README.md](Docs/00-README.md) for the complete documentation library.

**Historical Documentation**: Pre-reorganization docs are archived in [Docs/Archive/](Docs/Archive/) for reference.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **AuthServer** running at `https://localhost:5001` (for JWT tokens)
- **AdminAPI** running at `https://localhost:5206` (for backend services)
- [Google Maps API Key](https://developers.google.com/maps/documentation/javascript/get-api-key) (optional, for full map functionality)

## Getting Started

### 1. Clone & Restore

```sh
git clone https://github.com/BidumanADT/Bellwood.AdminPortal.git
cd Bellwood.AdminPortal
dotnet restore
```

### 2. Configure

Update `appsettings.json` with your settings:

```json
{
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": ""  // Optional admin API key
  },
  "AuthServer": {
    "LoginUrl": "https://localhost:5001/login"
  },
  "GoogleMaps": {
    "ApiKey": ""  // Get from Google Cloud Console
  }
}
```

### 3. Run

```sh
dotnet run
```

The portal will start at:
- **HTTPS**: `https://localhost:7257`
- **HTTP**: `http://localhost:5072`

### 4. Login

Navigate to `https://localhost:7257` and login with test credentials:

| Username | Password | Role | Purpose |
|----------|----------|------|---------|
| `alice` | `password` | Admin | Full access to all features |
| `bob` | `password` | Dispatcher | Booking management and tracking |

### 5. Seed Test Data

```powershell
# Seed AdminAPI with test data
.\Scripts\seed-admin-api.ps1

# Or manually via AdminAPI
curl -X POST https://localhost:5206/bookings/seed -k
curl -X POST https://localhost:5206/affiliates/seed -k
```

### 6. Verify Connection

```powershell
# Test AdminAPI connectivity
.\Scripts\test-api-connection.ps1
```

## Key Pages

### Main Dashboard (`/`)

**Features:**
- Recent bookings overview
- Quick stats (Total Bookings, Active Rides, Pending Quotes)
- Quick actions (New Booking, View Live Map, Manage Affiliates)
- Upcoming rides preview

**Access**: Any authenticated user

---

### Bookings List (`/bookings`)

**Features:**
- Filterable booking list (All, Requested, Confirmed, Active, Completed, Cancelled)
- Search by passenger name, location, booker
- Real-time status updates via SignalR
- Tracking indicators (??) for active GPS
- One-click navigation to booking details or live map

**Access**: Any authenticated user

**Filters:**
- **All** – Show all bookings
- **Requested** – Pending admin approval
- **Confirmed** – Approved, awaiting driver assignment
- **Active** – Rides with active GPS tracking (OnRoute, Arrived, PassengerOnboard)
- **Completed** – Finished rides
- **Cancelled** – Cancelled bookings

**Search**: Passenger name, booker name, pickup location, dropoff location

---

### Booking Detail (`/bookings/{id}`)

**Features:**
- Complete passenger and booker information
- Pickup/dropoff addresses with flight details (if airport)
- Assigned driver info with UserUid
- Real-time status badge (prefers `CurrentRideStatus` over `Status`)
- Live tracking card with coordinates, speed, last update
- Driver assignment workflow (select affiliate ? select driver)
- "View on Live Map" quick navigation
- Refresh button for latest location

**Access**: Any authenticated user

**Real-Time Updates:**
- SignalR subscription for ride status changes
- Automatic location refresh when ride becomes trackable
- Status badge updates instantly (no manual refresh)

---

### Live Tracking Map (`/tracking`)

**Features:**
- Interactive Google Maps with dark theme
- Real-time driver location markers (car icons)
- Driver list sidebar with ride details
- Connection status indicator (SignalR vs polling)
- Auto-fit map bounds to show all active drivers
- Click-to-zoom on selected ride
- Selected ride detail panel with coordinates, speed, heading
- Direct link to booking details

**Access**: Any authenticated user (admin/dispatcher recommended)

**Real-Time Updates:**
- SignalR `LocationUpdate` events (GPS coordinates)
- SignalR `RideStatusChanged` events (driver state changes)
- SignalR `TrackingStopped` events (ride completion)
- Polling fallback (15-second intervals) if SignalR unavailable

**Map Features:**
- Custom car icon markers for drivers
- Smooth marker animation between GPS updates
- Info windows with driver/passenger names
- Status badges (OnRoute, Arrived, PassengerOnboard)
- Last update time and age
- Current speed (mph)
- Dark theme styling matching Bellwood branding

---

### Quotes List (`/quotes`)

**Features:**
- View incoming quote requests
- Filter by status (All, Submitted, InReview, Priced, Rejected, Closed)
- Search by passenger name, booker name, pickup/dropoff location
- One-click navigation to quote details
- Status badge indicators
- Real-time quote count by status

**Access**: Any authenticated user

**Filters:**
- **All** – Show all quote requests
- **Submitted** – New quotes awaiting review
- **InReview** – Quotes being processed by admin
- **Priced** – Quotes with pricing ready for customer
- **Rejected** – Declined quote requests
- **Closed** – Completed/archived quotes

**Search**: Passenger name, booker name, pickup location, dropoff location

---

### Quote Detail (`/quotes/{id}`)

**Features:**
- Complete quote request information
- Booker and passenger contact details
- Trip details (pickup, dropoff, vehicle class, passengers, luggage)
- Special requests display
- Interactive pricing form with currency formatting
- Status management dropdown
- Admin notes (internal only, not visible to customers)
- Quick action buttons (Mark as Priced, Mark In Review, Reject)
- Save/Reset functionality with validation
- Success/error feedback messages
- Automatic customer notification when status changes to "Priced"

**Access**: Any authenticated user

**Status Workflow:**
1. **Submitted** – Initial state when quote request arrives
2. **InReview** – Admin is processing the quote
3. **Priced** – Price set and customer can view (triggers notification)
4. **Rejected** – Quote declined by admin
5. **Closed** – Quote completed/archived

**Admin Actions:**
- Set quoted price (decimal with $0.01 precision)
- Update status to track quote progress
- Add internal notes for team reference
- View previous notes and update history
- Use quick actions for common workflows

---

### Affiliates List (`/affiliates`)

**Features:**
- View affiliate companies
- Driver count per affiliate
- "View Drivers" quick navigation

**Access**: Any authenticated user

---

### Affiliate Detail (`/affiliates/{id}`)

**Features:**
- Affiliate company details
- Driver list for affiliate
- Add new driver form with UserUid linking
- Edit affiliate information
- Delete affiliate (if no drivers assigned)

**Access**: Any authenticated user

**Add Driver:**
- Driver name, phone
- UserUid (links to AuthServer account for DriverApp access)
- Automatic email to affiliate upon driver addition

---

## Real-Time Tracking (SignalR)

### How It Works

```
1. AdminPortal connects to LocationHub via SignalR WebSocket
   ?
2. Subscribes to "admin" group (automatic for admin/dispatcher role)
   ?
3. Driver sends GPS update (POST /driver/location/update)
   ?
4. AdminAPI broadcasts LocationUpdate to "admin" group
   ?
5. AdminPortal receives event and updates:
   - Bookings list (if booking is in view)
   - BookingDetail page (if viewing that ride)
   - LiveTracking map (updates marker position)
   ?
6. UI refreshes via StateHasChanged() – no manual refresh needed
```

### SignalR Events Received

**LocationUpdate** - Driver GPS update:

```json
{
  "rideId": "abc123",
  "driverUid": "driver-001",
  "driverName": "Charlie Johnson",
  "latitude": 41.8781,
  "longitude": -87.6298,
  "heading": 45.5,
  "speed": 12.3,
  "accuracy": 8.5,
  "timestamp": "2025-12-20T15:30:00Z"
}
```

**RideStatusChanged** - Driver state change:

```json
{
  "rideId": "abc123",
  "driverUid": "driver-001",
  "driverName": "Charlie Johnson",
  "passengerName": "Jane Doe",
  "newStatus": "OnRoute",
  "timestamp": "2025-12-20T15:30:00Z"
}
```

**TrackingStopped** - Ride completion:

```json
{
  "rideId": "abc123",
  "reason": "Ride completed",
  "timestamp": "2025-12-20T16:00:00Z"
}
```

### Connection Management

**DriverTrackingService** handles all SignalR operations:

- **Automatic Connection**: Connects on first use, stores JWT token in query parameter
- **Automatic Reconnection**: Exponential backoff (0s, 2s, 5s, 10s) on disconnect
- **Polling Fallback**: 15-second REST polling if SignalR unavailable
- **Event Cleanup**: Proper `IAsyncDisposable` implementation for component lifecycle

**Connection States:**
- **Connected** (green badge) – SignalR active, real-time updates
- **Disconnected** (red badge) – Polling mode, 15-second refresh

## AdminAPI Integration

The AdminPortal consumes the following AdminAPI endpoints:

### Booking Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/bookings/list?take=100` | GET | List recent bookings (includes `CurrentRideStatus` + `PickupDateTimeOffset`) |
| `/bookings/{id}` | GET | Get booking details |
| `/bookings/{id}/assign-driver` | POST | Assign driver to booking |
| `/bookings/{id}/cancel` | POST | Cancel booking |

### Quote Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/quotes/list?take=100` | GET | List recent quotes |
| `/quotes/{id}` | GET | Get quote details |

### Affiliate & Driver Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/affiliates/list` | GET | List all affiliates with drivers |
| `/affiliates/{id}` | GET | Get affiliate details |
| `/affiliates` | POST | Create affiliate |
| `/affiliates/{id}/drivers` | POST | Add driver to affiliate |
| `/drivers/list` | GET | List all drivers |

### Location Tracking Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/admin/locations` | GET | Get all active driver locations (envelope format) |
| `/driver/location/{rideId}` | GET | Get latest location for specific ride |
| `/hubs/location` | WebSocket | SignalR hub for real-time updates |

### SignalR Hub Methods

**Auto-Join Groups:**
- `admin` group (for all users with admin/dispatcher role)

**Manual Subscriptions:**
- `SubscribeToRide(rideId)` – Get updates for specific ride
- `SubscribeToDriver(driverUid)` – Track specific driver
- `UnsubscribeFromRide(rideId)` – Stop ride updates
- `UnsubscribeFromDriver(driverUid)` – Stop driver updates

## Configuration

### Required Settings

| Setting | Location | Purpose |
|---------|----------|---------|
| `AdminAPI:BaseUrl` | appsettings.json | AdminAPI URL for REST and SignalR |
| `AuthServer:LoginUrl` | appsettings.json | AuthServer login endpoint |
| `GoogleMaps:ApiKey` | appsettings.json | Google Maps JavaScript API key (optional) |

### Optional Settings

| Setting | Location | Purpose |
|---------|----------|---------|
| `AdminAPI:ApiKey` | appsettings.json | Admin API key for additional security |

### Google Maps Setup

**Without API Key**: Map shows placeholder, sidebar still functional

**With API Key**:
1. Get API key from [Google Cloud Console](https://console.cloud.google.com/)
2. Enable "Maps JavaScript API"
3. Add to `appsettings.json`:

```json
{
  "GoogleMaps": {
    "ApiKey": "AIzaSy..."
  }
}
```

4. Restart portal

## Authentication & Authorization

### JWT Token Structure

```json
{
  "sub": "alice",            // Username
  "uid": "admin-001",        // Admin UserUid (optional)
  "email": "alice@example.com", // Email
  "role": "admin",           // Role (admin, dispatcher)
  "exp": 1234567890          // Expiration timestamp
}
```

### Authentication Flow

```
1. User navigates to AdminPortal
   ?
2. If not authenticated ? Redirect to /login
   ?
3. User enters username/password
   ?
4. POST to AuthServer /login
   ?
5. AuthServer returns JWT token
   ?
6. Portal stores token in localStorage (AuthTokenProvider)
   ?
7. JwtAuthenticationStateProvider updates auth state
   ?
8. User redirected to dashboard
   ?
9. All API calls include: Authorization: Bearer {token}
```

### Authorization Roles

| Role | Access | Features |
|------|--------|----------|
| **admin** | Full access | All features + affiliate management |
| **dispatcher** | Booking management | Bookings, quotes, tracking (limited affiliate access) |
| **Unauthenticated** | None | Redirected to login |

## Status Display Logic

### Dual Status Model

The AdminPortal displays ride status using **two fields**:

| Field | Purpose | Values | Audience |
|-------|---------|--------|----------|
| `Status` | Booking-level status | Requested, Confirmed, Scheduled, InProgress, Completed, Cancelled, NoShow | Reports, accounting |
| `CurrentRideStatus` | Real-time driver state | Scheduled, OnRoute, Arrived, PassengerOnboard, Completed, Cancelled | Dispatchers, real-time operations |

### Display Priority

```csharp
// Always prefer CurrentRideStatus when available
string displayStatus = booking.CurrentRideStatus ?? booking.Status ?? "Unknown";
```

**Example**:

| CurrentRideStatus | Status | Displayed | Badge Color |
|-------------------|--------|-----------|-------------|
| `OnRoute` | `Scheduled` | **OnRoute** | Blue (`bg-info`) |
| `Arrived` | `Scheduled` | **Arrived** | Yellow (`bg-warning`) |
| `PassengerOnboard` | `InProgress` | **PassengerOnboard** | Green (`bg-success`) |
| `null` | `Scheduled` | **Scheduled** | Gray (`bg-secondary`) |
| `Completed` | `Completed` | **Completed** | Green (`bg-success`) |

### Status Badge Styling

**Driver Statuses** (`CurrentRideStatus`):
- **OnRoute** – Blue (`bg-info`)
- **Arrived** – Yellow (`bg-warning text-dark`)
- **PassengerOnboard** – Green (`bg-success`)

**Booking Statuses** (`Status`):
- **Requested** – Warning (`bg-warning`)
- **Confirmed** – Success (`bg-success`)
- **Scheduled** – Info (`bg-info`)
- **InProgress** – Primary (`bg-primary`)
- **Completed** – Success (`bg-success`)
- **Cancelled** – Danger (`bg-danger`)
- **NoShow** – Secondary (`bg-secondary`)

## Timezone Support

### How It Works

The AdminPortal displays **timezone-aware pickup times** via the `PickupDateTimeOffset` field from AdminAPI.

**API Response Example**:

```json
{
  "pickupDateTime": "2025-12-24T15:00:00Z",  // Raw UTC (backward compatibility)
  "pickupDateTimeOffset": "2025-12-24T09:00:00-06:00"  // Central Time with offset
}
```

**Display Logic**:

```csharp
// Prefer PickupDateTimeOffset when available
var displayTime = booking.PickupDateTimeOffset?.LocalDateTime 
    ?? booking.PickupDateTime.ToLocalTime();
```

**Result**: Times display correctly for users in any timezone worldwide.

## Troubleshooting

### Common Issues

**1. SignalR connection failures**

**Symptoms**: Red connection badge, polling mode active

**Checklist**:
- [ ] AdminAPI running at configured `AdminAPI:BaseUrl`?
- [ ] JWT token valid (not expired)?
- [ ] HTTPS certificate trusted (dev environment)?

**Fix**: Check browser console for error details. Ensure AdminAPI `/hubs/location` endpoint accessible.

---

**2. Status not updating in real-time**

**Symptoms**: Status badge doesn't change when driver updates status

**Checklist**:
- [ ] SignalR connection "Connected"?
- [ ] `RideStatusChanged` event handler registered?
- [ ] AdminAPI returning `CurrentRideStatus` in `/bookings/list`?

**Fix**: Check browser console for `[Bookings] Ride abc123 status updated to OnRoute` log messages.

---

**3. Map not loading**

**Symptoms**: Map shows placeholder "Map unavailable"

**Checklist**:
- [ ] `GoogleMaps:ApiKey` configured in `appsettings.json`?
- [ ] Maps JavaScript API enabled in Google Cloud Console?
- [ ] API key restrictions allow localhost?

**Fix**: Add API key to `appsettings.json`, verify API enabled, check browser console for Google Maps errors.

---

**4. 401 Unauthorized on API calls**

**Symptoms**: Bookings list empty, "Failed to load" errors

**Checklist**:
- [ ] Logged in successfully?
- [ ] JWT token stored in localStorage?
- [ ] Token not expired?
- [ ] AdminAPI configured to accept JWT from AuthServer?

**Fix**: Logout and login again. Check `localStorage` for `authToken` key in browser DevTools.

---

**5. "Active" filter shows no rides**

**Symptoms**: Active filter empty even with tracking rides

**Checklist**:
- [ ] AdminAPI returning `CurrentRideStatus` in `/bookings/list`?
- [ ] Driver has updated status to OnRoute/Arrived/PassengerOnboard?

**Fix**: Ensure AdminAPI deployed with latest `CurrentRideStatus` support (see AdminAPI Booking List API Enhancement doc).

## Testing

### Manual Testing Flow

**1. Login Test**:
```sh
# Navigate to portal
https://localhost:7257

# Login as alice/password
# Verify redirect to dashboard
```

**2. Booking List Test**:
```sh
# Click "Bookings" in sidebar
# Verify bookings load
# Click "Active" filter
# Verify only tracking rides shown
```

**3. Real-Time Update Test**:
```sh
# Open Bookings page in browser
# Use DriverApp to change ride status to "OnRoute"
# Verify badge updates instantly (no refresh)
# Verify "Active" filter includes the ride
```

**4. Live Map Test**:
```sh
# Click "Live Tracking" in sidebar
# Verify map loads with driver markers
# Verify connection status "Connected" (green)
# Click driver in sidebar
# Verify map zooms to driver
# Use DriverApp to send GPS update
# Verify marker moves on map
```

**5. Driver Assignment Test**:
```sh
# Open booking detail for unassigned booking
# Click "Assign Driver"
# Select affiliate
# Select driver
# Click "Confirm Assignment"
# Verify driver assigned successfully
# Verify email sent to affiliate
```

### Integration Testing

**Prerequisites**:
- AuthServer running with test users (alice, bob, charlie)
- AdminAPI running with seeded test data
- DriverApp running with test driver logged in

**Test Scenario**:
1. **Dispatcher** (Alice) opens AdminPortal
2. **Driver** (Charlie) starts ride in DriverApp (status ? OnRoute)
3. **Dispatcher** sees status change instantly in Bookings list
4. **Dispatcher** opens Live Tracking map
5. **Driver** sends GPS updates
6. **Dispatcher** sees marker move on map in real-time
7. **Driver** arrives at pickup (status ? Arrived)
8. **Dispatcher** sees status badge change to "Arrived"
9. **Driver** picks up passenger (status ? PassengerOnboard)
10. **Dispatcher** sees status badge change to "PassengerOnboard"
11. **Driver** completes ride (status ? Completed)
12. **Dispatcher** sees ride removed from "Active" filter
13. **Dispatcher** verifies marker removed from map

**Expected Result**: All updates happen instantly without manual refresh ?

## Deployment

### Build

```sh
# Development
dotnet build

# Production
dotnet build -c Release
```

### Publish

```sh
# Self-contained (recommended for IIS)
dotnet publish -c Release -r win-x64 --self-contained

# Framework-dependent
dotnet publish -c Release
```

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | `Development` |
| `ASPNETCORE_URLS` | Listening URLs | `https://localhost:7257;http://localhost:5072` |

### Production Checklist

- [ ] Update `AdminAPI:BaseUrl` to production AdminAPI URL
- [ ] Update `AuthServer:LoginUrl` to production AuthServer URL
- [ ] Configure Google Maps API key
- [ ] Set up HTTPS certificates
- [ ] Enable detailed logging for debugging
- [ ] Configure reverse proxy (IIS, Nginx, etc.)
- [ ] Test SignalR WebSocket connectivity through firewall
- [ ] Verify JWT token validation with production AuthServer

### IIS Deployment

**Prerequisites**:
- Windows Server with IIS 10+
- [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer)

**Steps**:
1. Publish portal: `dotnet publish -c Release -r win-x64 --self-contained`
2. Copy `bin/Release/net8.0/win-x64/publish/` to IIS server
3. Create IIS site pointing to publish folder
4. Create application pool (.NET CLR Version = No Managed Code)
5. Enable WebSocket Protocol in IIS features
6. Configure HTTPS binding with certificate
7. Update `appsettings.json` with production URLs
8. Restart IIS site

**WebSocket Configuration**:
```xml
<!-- web.config (auto-generated by publish) -->
<configuration>
  <system.webServer>
    <webSocket enabled="true" />
  </system.webServer>
</configuration>
```

## Monitoring & Logging

### Console Logging

Key events logged to console:

```
? [Bookings] Ride abc123 status updated to OnRoute by Charlie Johnson
?? [LiveTracking] Ride abc123 status updated to Arrived by Charlie Johnson
?? [DriverTrackingService] Connected to SignalR location hub
?? [DriverTrackingService] SignalR connection closed: Connection lost
?? [DriverTrackingService] SignalR reconnected
```

### SignalR Connection Monitoring

**DriverTrackingService** exposes connection state:

```csharp
public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
```

**UI Indicators**:
- **Green badge** – SignalR connected
- **Red badge** – Polling fallback active

### Performance Metrics

| Metric | Target | Typical |
|--------|--------|---------|
| Page load time | < 2s | ~500ms-1s |
| SignalR event latency | < 1s | ~100-500ms |
| Map marker update | < 500ms | ~100-300ms |
| Booking list refresh | < 1s | ~200-500ms |

## Roadmap

### Short-Term (Q1 2025)

- [ ] Add toast notifications for status changes
- [ ] Implement driver availability status
- [ ] Add booking creation from quotes
- [ ] Support bulk driver assignment
- [ ] Add export bookings to CSV/Excel

### Long-Term (2025+)

- [ ] Historical route playback on map
- [ ] ETA calculations based on GPS speed
- [ ] Geofencing alerts (driver entered/exited zone)
- [ ] Heatmap view of driver locations
- [ ] Multi-language support
- [ ] Mobile-responsive layout improvements
- [ ] Dark mode toggle

## Branches

- **main** – Stable production code
- **feature/driver-tracking-prep** – Driver tracking development (merged)
- **develop** – Integration branch for features

## Security & Standards

- **JWT Authentication** with role-based authorization
- **HTTPS** for all connections; dev builds allow local certificates
- **SignalR WebSockets** with automatic reconnection and polling fallback
- **Secure Token Storage** in browser localStorage with proper cleanup
- **Input Validation** on all forms
- Follow **Blazor Server best practices**, **async/await** for I/O, **DI-first architecture**, **nullable reference types** enabled

## Support

For issues or questions:

- **GitHub Issues**: [https://github.com/BidumanADT/Bellwood.AdminPortal/issues](https://github.com/BidumanADT/Bellwood.AdminPortal/issues)
- **Documentation**: See `Docs/` directory (20+ comprehensive guides)
- **Email**: support@bellwood.com

---

## Key Features Summary

? **Real-Time GPS Tracking** via SignalR WebSockets with interactive Google Maps  
? **Instant Status Updates** on all pages without manual refresh  
? **Dual Status Model** (public + driver-facing) with automatic priority logic  
? **Live Tracking Dashboard** with driver markers, sidebar, and auto-zoom  
? **Booking Management** with search, filters, and driver assignment  
? **Affiliate & Driver Management** with UserUid linking to AuthServer  
? **Timezone Support** with automatic timezone-aware pickup times  
? **Premium Bellwood Branding** with dark theme and gold accents  
? **Automatic Reconnection** with polling fallback for SignalR failures  
? **Complete Documentation** with deployment guides and troubleshooting  
? **Production-Ready** with proper error handling, logging, and security  

---

**Built with care using Blazor Server + SignalR on .NET 8**

*© 2025 Biduman ADT / Bellwood Global. All rights reserved.*
