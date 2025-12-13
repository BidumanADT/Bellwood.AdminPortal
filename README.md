# Bellwood Admin Portal

> Modern admin dashboard for managing bookings, quotes, affiliates, and real-time driver tracking for Bellwood Elite Transportation Services.

---

## ?? Quick Start

### Prerequisites
- .NET 8 SDK
- Running **AuthServer** on `https://localhost:5001`
- Running **AdminAPI** on `https://localhost:5206`

### 1. Run the Portal

```bash
dotnet run
```

### 2. Login

Navigate to `https://localhost:7257` and login with:
- Username: `alice` / Password: `password`
- Username: `bob` / Password: `password`

### 3. Seed Test Data (Optional)

```powershell
# Seed bookings
.\seed-admin-api.ps1

# Or manually
curl -X POST https://localhost:5206/bookings/seed -k
```

---

## ? Features

### ?? Bookings Management
- View all customer bookings in a sortable, filterable table
- Search by passenger name, phone, or booking ID
- Filter by status (Pending, Confirmed, Completed, Cancelled)
- View detailed booking information including:
  - Passenger details
  - Pickup and dropoff locations
  - Ride dates and times
  - Assigned driver and affiliate
  - Service type and vehicle requirements
- Update booking statuses
- Driver assignment workflow

### ?? Quotes Management
- Review incoming quote requests
- Provide pricing estimates
- Convert quotes to confirmed bookings
- Track quote status (Pending, Sent, Accepted, Declined)

### ?? Affiliates Management
- Manage transportation affiliates and their driver fleets
- Create, edit, and delete affiliate organizations
- View affiliate contact information and addresses
- Manage drivers associated with each affiliate
- Assign drivers to bookings

### ??? Live Driver Tracking
- **Real-time map view** of all active drivers
- **SignalR WebSocket** integration for instant location updates
- **HTTP polling fallback** when WebSocket unavailable
- Interactive Google Maps with custom car icons
- Driver information cards showing:
  - Current speed and heading
  - Passenger name
  - Pickup/dropoff locations
  - Ride status (On Route, Arrived, Passenger On Board)
  - Last update timestamp
- Click-to-zoom on specific rides
- Connection status indicator
- Seamless integration with booking details

### ?? Premium Design
- Modern, responsive Bootstrap 5 UI
- Bellwood Elite gold and black branding
- Dark theme map styling
- Mobile-friendly layouts
- Intuitive navigation with sidebar menu

---

## ?? Project Structure

```
Bellwood.AdminPortal/
??? Components/
?   ??? App.razor                 # Root component with Router
?   ??? Layout/
?   ?   ??? MainLayout.razor      # Authenticated pages layout with sidebar
?   ?   ??? EmptyLayout.razor     # Login/public pages layout
?   ?   ??? NavMenu.razor         # Navigation sidebar
?   ??? Pages/
?       ??? Home.razor            # Root route (redirects based on auth)
?       ??? Login.razor           # Login page with JWT authentication
?       ??? Logout.razor          # Logout handler
?       ??? Main.razor            # Dashboard landing page
?       ??? Bookings.razor        # Bookings list with filters
?       ??? BookingDetail.razor   # Individual booking details
?       ??? Quotes.razor          # Quotes management
?       ??? Affiliates.razor      # Affiliates list and management
?       ??? AffiliateDetail.razor # Affiliate details with drivers
?       ??? LiveTracking.razor    # Real-time driver tracking map
??? Services/
?   ??? IAuthTokenProvider.cs           # JWT token storage interface
?   ??? AuthTokenProvider.cs            # In-memory token storage
?   ??? IAdminApiKeyProvider.cs         # API key access interface
?   ??? AdminApiKeyProvider.cs          # Reads API key from config
?   ??? JwtAuthenticationStateProvider.cs # Blazor auth state management
?   ??? IAffiliateService.cs            # Affiliate CRUD interface
?   ??? AffiliateService.cs             # Affiliate HTTP client service
?   ??? IDriverTrackingService.cs       # Driver tracking interface
?   ??? DriverTrackingService.cs        # SignalR + REST tracking service
??? Models/
?   ??? BookingModels.cs          # Booking DTOs
?   ??? AffiliateModels.cs        # Affiliate and Driver DTOs
?   ??? DriverTrackingModels.cs   # Location and tracking DTOs
??? wwwroot/
?   ??? js/
?   ?   ??? tracking-map.js       # Google Maps JavaScript interop
?   ??? css/
?       ??? bellwood.css          # Custom styling
??? Program.cs                     # DI configuration & middleware
??? appsettings.json              # Configuration
??? Bellwood.AdminPortal.csproj   # Project file

```

---

## ?? Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": "dev-secret-123"
  },
  "GoogleMaps": {
    "ApiKey": ""
  }
}
```

**Important Configuration Notes:**

1. **AdminAPI.ApiKey**: Must match `Email:ApiKey` in AdminAPI's `appsettings.Development.json`
2. **AdminAPI.BaseUrl**: Should point to your running AdminAPI instance
3. **GoogleMaps.ApiKey**: Optional - required for live driver tracking map functionality
   - Without API key: Map shows placeholder, sidebar still functions
   - Get your API key from: https://console.cloud.google.com/

---

## ??? Architecture

### Authentication Flow

```
User ? Login.razor ? AuthServer (/api/auth/login)
       ?
   JWT Token Received
       ?
   JwtAuthenticationStateProvider.MarkUserAsAuthenticatedAsync()
       ?
   Token stored in AuthTokenProvider (in-memory)
       ?
   Blazor AuthenticationState updated
       ?
   Navigate to /main
```

### API Communication

All API calls to AdminAPI include:
- **X-Admin-ApiKey** header (from configuration)
- **Authorization: Bearer {token}** header (from AuthTokenProvider)

```
Component ? HttpClient "AdminAPI" ? AdminAPI Endpoint
                ?
         Base URL: https://localhost:5206
         Headers: X-Admin-ApiKey, Authorization
                ?
         AdminAPI validates both
                ?
         Returns JSON response
```

### Real-Time Driver Tracking

```
AdminAPI LocationHub (SignalR WebSocket)
       ?
DriverTrackingService (Blazor Server)
  - Maintains WebSocket connection
  - Handles automatic reconnection
  - Fires events on location updates
       ?
LiveTracking.razor (UI Component)
  - Subscribes to service events
  - Updates map markers via JS Interop
  - Refreshes ride list
       ?
tracking-map.js (JavaScript)
  - Manages Google Maps instance
  - Animates marker positions
  - Handles user interactions
```

**Fallback Mode:** When SignalR disconnected, automatic HTTP polling every 15 seconds.

---

## ?? Key Components

### 1. JwtAuthenticationStateProvider

Bridge between login flow and Blazor's authorization system.

**Responsibilities:**
- Stores current user's authentication state
- Notifies Blazor when auth state changes (login/logout)
- Used by `<AuthorizeView>` to determine authentication status

### 2. DriverTrackingService

Manages real-time driver location tracking.

**Key Features:**
- SignalR WebSocket connection with auto-reconnect
- HTTP polling fallback
- Event-driven architecture for UI updates
- Subscription management for specific rides or drivers

**Events:**
- `LocationUpdated` - New location data received
- `TrackingStopped` - Ride completed or cancelled
- `ConnectionStateChanged` - SignalR connection state changes

### 3. AffiliateService

Handles affiliate and driver management.

**Operations:**
- CRUD operations for affiliates
- Driver fleet management
- Affiliate-driver associations

---

## ?? NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.11" />
<PackageReference Include="System.Net.Http.Json" Version="8.0.0" />
```

**?? Important:** SignalR.Client must be version 8.x (not 10.x) to avoid form submission conflicts with Blazor's antiforgery system.

---

## ?? Routes

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home.razor | Redirects to /main if authenticated, else /login |
| `/login` | Login.razor | JWT authentication page |
| `/logout` | Logout.razor | Clears auth state and redirects to login |
| `/main` | Main.razor | Dashboard landing page |
| `/bookings` | Bookings.razor | Bookings list with filters |
| `/bookings/{id}` | BookingDetail.razor | Detailed booking view |
| `/quotes` | Quotes.razor | Quote requests management |
| `/affiliates` | Affiliates.razor | Affiliates management |
| `/affiliates/{id}` | AffiliateDetail.razor | Affiliate details with drivers |
| `/tracking` | LiveTracking.razor | Real-time driver tracking map |

---

## ?? Usage Guide

### For Dispatchers

**Managing Bookings:**
1. Navigate to **Bookings** from sidebar
2. Use filters to find specific bookings
3. Click a booking to view full details
4. Assign drivers using the "Assign Driver" button
5. Update booking status as needed

**Live Driver Tracking:**
1. Navigate to **Live Tracking** from sidebar
2. View all active drivers on the map
3. Click a ride card to zoom to that driver
4. Monitor driver speed, location, and status
5. Click "View Booking Details" for full ride information

**Managing Affiliates:**
1. Navigate to **Affiliates** from sidebar
2. Create new affiliates with "Create Affiliate" button
3. Edit affiliate details by clicking the edit icon
4. Manage drivers within each affiliate
5. Delete affiliates (only if no active assignments)

### For Administrators

**System Monitoring:**
- Green badge on Live Tracking = Real-time SignalR connected
- Red badge = HTTP polling mode (check network/SignalR)
- Configure Google Maps API key in appsettings.json for full map functionality

---

## ?? Future Enhancements

### Planned Features
- Route history playback with polylines
- ETA calculations using speed data
- Geofencing alerts for zone-based notifications
- Driver-specific tracking across multiple rides
- Heatmap view for location density
- Export/reporting for BI tools
- OAuth 2.0 integration with LimoAnywhere

### OAuth 2.0 Migration Path

When integrating with LimoAnywhere's OAuth 2.0:

1. **Replace AuthServer calls** in Login.razor with OAuth authorization flow
2. **Create OAuth callback handler** to exchange authorization code for access token
3. **Update token storage** to handle refresh tokens
4. **Implement token refresh logic** in AuthTokenProvider

The current architecture is designed to support this migration with minimal changes.

---

## ?? Additional Documentation

Detailed documentation available in the `Docs/` folder:

- **[QUICK_START.md](Docs/QUICK_START.md)** - Getting started guide
- **[ARCHITECTURE.md](Docs/ARCHITECTURE.md)** - System architecture details
- **[DRIVER_TRACKING_ADMINPORTAL_IMPLEMENTATION.md](Docs/DRIVER_TRACKING_ADMINPORTAL_IMPLEMENTATION.md)** - Driver tracking implementation
- **[DRIVER_ASSIGNMENT_IMPLEMENTATION.md](Docs/DRIVER_ASSIGNMENT_IMPLEMENTATION.md)** - Driver assignment workflow
- **[PREMIUM_DESIGN_IMPLEMENTATION.md](Docs/PREMIUM_DESIGN_IMPLEMENTATION.md)** - UI/UX design guide
- **[STAKEHOLDER_DEMO_GUIDE.md](Docs/STAKEHOLDER_DEMO_GUIDE.md)** - Demo preparation guide

---

## ?? Contributing

### Development Workflow

1. Create feature branch from `main`
2. Implement features with tests
3. Update documentation
4. Submit pull request

### Code Style

- Follow C# naming conventions
- Use meaningful variable names
- Document public APIs with XML comments
- Keep components focused and single-purpose

---

## ?? License

© 2024 Bellwood Global, Inc. All rights reserved.

---

## ?? Support

For issues or questions:
- Check documentation in `Docs/` folder
- Review console logs for detailed error messages
- Verify AuthServer and AdminAPI are running
- Ensure API keys and configuration are correct

---

**Built with ?? using .NET 8 and Blazor Server**
