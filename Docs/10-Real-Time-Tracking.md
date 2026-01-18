# Real-Time GPS Tracking

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 17, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Bellwood AdminPortal provides real-time GPS tracking of driver locations through SignalR WebSocket integration with an interactive Google Maps interface. Dispatchers can monitor active drivers, view live status updates, and track progress in real-time.

**Key Features**:
- ??? Interactive Google Maps with dark theme
- ? Real-time location updates via SignalR
- ?? Live driver markers with smooth animations
- ?? Automatic reconnection and polling fallback
- ?? Active rides dashboard with status tracking
- ?? Multi-page real-time synchronization

**Target Audience**: Developers, mobile app team  
**Prerequisites**: SignalR, JWT authentication, Google Maps API

---

## ?? Use Cases

### For Dispatchers

1. **Monitor Active Drivers**
   - View all drivers with active rides on a single map
   - See real-time location updates as drivers move
   - Track progress toward pickup/dropoff locations

2. **Respond to Customer Inquiries**
   - Answer "Where is my driver?" questions instantly
   - Provide accurate ETAs based on current location
   - Reassure customers with real-time updates

3. **Manage Fleet Operations**
   - Identify drivers nearby for new assignments
   - Monitor service coverage across the city
   - Detect delays or route deviations

4. **Support Driver Coordination**
   - Verify driver arrival at pickup locations
   - Confirm passenger pickup events
   - Track ride completion

---

## ??? Architecture

### Component Overview

```
???????????????????????????????????????????????????????????????
?                     AdminPortal                              ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? LiveTracking.razor                             ?        ?
?  ?  - Interactive map display                     ?        ?
?  ?  - Active rides sidebar                        ?        ?
?  ?  - Connection status indicator                 ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? DriverTrackingService                          ?        ?
?  ?  - SignalR client                              ?        ?
?  ?  - REST API fallback                           ?        ?
?  ?  - Event management                            ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
????????????????????????????????????????????????????????????????
                 ? SignalR WebSocket
                 ? (or HTTP polling fallback)
                 ?
???????????????????????????????????????????????????????????????
?                     AdminAPI                                 ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? LocationHub (SignalR)                          ?        ?
?  ?  - Broadcast LocationUpdate                    ?        ?
?  ?  - Broadcast RideStatusChanged                 ?        ?
?  ?  - Broadcast TrackingStopped                   ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? Location REST Endpoints                        ?        ?
?  ?  - GET /admin/locations                        ?        ?
?  ?  - GET /driver/location/{rideId}               ?        ?
?  ??????????????????????????????????????????????????        ?
?                                                              ?
????????????????????????????????????????????????????????????????
                 ? GPS Updates
???????????????????????????????????????????????????????????????
? DriverApp (MAUI)                                             ?
?  - POST /driver/location/update                              ?
?  - { rideId, lat, lng, speed, heading, accuracy }            ?
????????????????????????????????????????????????????????????????
```

---

## ?? Real-Time Update Flow

### Complete Event Sequence

```
1. Driver sends GPS update (DriverApp)
   POST /driver/location/update
   ?
2. AdminAPI persists location to storage
   ?
3. AdminAPI broadcasts SignalR event
   LocationUpdate ? "admin" group
   ?
4. AdminPortal receives event (All open pages)
   - Bookings.razor: Updates booking list
   - BookingDetail.razor: Updates location card
   - LiveTracking.razor: Updates map marker
   ?
5. UI updates via StateHasChanged()
   - Marker animates to new position
   - Sidebar shows updated speed/time
   - Status badges refresh
   ?
6. User sees update (< 1 second latency)
```

---

## ?? SignalR Integration

### DriverTrackingService

**File**: `Services/DriverTrackingService.cs`

**Key Features**:
- SignalR WebSocket connection with automatic reconnection
- REST API fallback for HTTP polling
- Event-driven architecture for UI updates
- Subscription management for rides and drivers

**Connection Management**:
```csharp
public async Task ConnectAsync()
{
    var token = await _tokenProvider.GetTokenAsync();
    
    _hubConnection = new HubConnectionBuilder()
        .WithUrl($"{_adminApiBaseUrl}/hubs/location?access_token={token}")
        .WithAutomaticReconnect(new[] { 
            TimeSpan.Zero,          // Immediate
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10) 
        })
        .Build();

    // Register event handlers
    _hubConnection.On<LocationUpdate>("LocationUpdate", HandleLocationUpdate);
    _hubConnection.On<RideStatusChangedEvent>("RideStatusChanged", HandleStatusChanged);
    _hubConnection.On<TrackingStoppedEvent>("TrackingStopped", HandleTrackingStopped);

    await _hubConnection.StartAsync();
    IsConnected = true;
}
```

---

### Events

**LocationUpdate**:
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

**RideStatusChanged**:
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

**TrackingStopped**:
```json
{
  "rideId": "abc123",
  "reason": "Ride completed",
  "timestamp": "2025-12-20T16:00:00Z"
}
```

**See**: [21-SignalR-Reference.md](21-SignalR-Reference.md) for complete event documentation

---

## ??? Live Tracking Page

### LiveTracking.razor

**File**: `Components/Pages/LiveTracking.razor`

**Features**:
- Interactive Google Maps with dark theme
- Real-time driver markers (car icons)
- Active rides sidebar
- Connection status indicator
- Ride selection and zoom
- Polling fallback (15-second intervals)

**UI Components**:

```
???????????????????????????????????????????????????????????
? Live Tracking Dashboard                                 ?
???????????????????????????????????????????????????????????
? [?] Connected  Active Rides: 5                          ?
???????????????????????????????????????????????????????????
? Active Rides Sidebar  ?    Interactive Map              ?
?                       ?                                 ?
? ???????????????????? ?  ???????????????????????????   ?
? ? Charlie Johnson  ? ?  ?         ???             ?   ?
? ? ? Maria Garcia   ? ?  ?    ?? ??              ?   ?
? ? OnRoute          ? ?  ?                         ?   ?
? ? 55 mph  2m ago   ? ?  ?      ??                ?   ?
? ???????????????????? ?  ?                         ?   ?
?                       ?  ?  ??        ??          ?   ?
? ???????????????????? ?  ???????????????????????????   ?
? ? Sarah Davis      ? ?                                 ?
? ? ? Robert Chen    ? ?  Selected: Charlie Johnson      ?
? ? Arrived          ? ?  Location: (41.87, -87.62)      ?
? ? 0 mph  1m ago    ? ?  Speed: 55 mph                  ?
? ???????????????????? ?  Last Update: 2 min ago         ?
?                       ?  [View Booking Details]         ?
???????????????????????????????????????????????????????????
```

---

### Google Maps Integration

**File**: `wwwroot/js/tracking-map.js`

**JavaScript Interop Functions**:

| Function | Description |
|----------|-------------|
| `initTrackingMap(elementId, apiKey)` | Initialize map with dark theme |
| `addDriverMarker(rideId, lat, lng, name, status)` | Add car icon marker |
| `updateDriverMarker(rideId, lat, lng)` | Animate marker to new position |
| `removeDriverMarker(rideId)` | Remove marker from map |
| `centerMapOnMarker(rideId, lat, lng)` | Pan and zoom to marker |
| `fitBoundsToMarkers()` | Auto-fit to show all markers |
| `disposeTrackingMap()` | Clean up resources |

**Custom Car Icon** (SVG):
```html
<svg width="32" height="32">
  <path d="M16 4 L8 16 L24 16 Z" fill="#FFD700" />
  <!-- Stylized car shape -->
</svg>
```

**Dark Theme Styling**:
```javascript
const styles = [
  { elementType: "geometry", stylers: [{ color: "#212121" }] },
  { elementType: "labels.text.fill", stylers: [{ color: "#757575" }] },
  { elementType: "labels.text.stroke", stylers: [{ color: "#212121" }] },
  // ... dark theme configuration
];
```

---

## ?? Dashboard Integration

### Bookings.razor (Real-Time Updates)

**SignalR Event Subscription**:
```csharp
protected override async Task OnInitializedAsync()
{
    // Subscribe to real-time events
    if (DriverTrackingService != null)
    {
        await DriverTrackingService.ConnectAsync();
        DriverTrackingService.StatusChanged += OnRideStatusChanged;
    }

    await LoadBookingsAsync();
}

private void OnRideStatusChanged(object? sender, RideStatusChangedEvent e)
{
    Console.WriteLine($"[Bookings] Ride {e.RideId} status updated to {e.NewStatus}");

    // Find and update booking in list
    var booking = allBookings.FirstOrDefault(b => b.Id == e.RideId);
    if (booking != null)
    {
        booking.CurrentRideStatus = e.NewStatus;
        FilterBookings(currentFilter); // Re-apply filter
        StateHasChanged(); // Trigger UI update
    }
}
```

**Active Filter**:
```csharp
private List<BookingListItem> FilterActiveBookings()
{
    // Show only rides with active GPS tracking
    return allBookings.Where(b => IsTrackableStatus(b.CurrentRideStatus)).ToList();
}

private bool IsTrackableStatus(string? status)
{
    return status switch
    {
        "OnRoute" => true,
        "Arrived" => true,
        "PassengerOnboard" => true,
        _ => false
    };
}
```

**UI Indicators**:
```razor
@foreach (var booking in filteredBookings)
{
    <div class="booking-card">
        @if (IsTrackableStatus(booking.CurrentRideStatus))
        {
            <span class="tracking-indicator" title="Live tracking active">??</span>
        }
        
        <div class="status-badge @GetStatusClass(booking.CurrentRideStatus ?? booking.Status)">
            @(booking.CurrentRideStatus ?? booking.Status)
        </div>
        
        <!-- Rest of card content -->
    </div>
}
```

---

### BookingDetail.razor (Live Tracking Card)

**Location Display**:
```razor
@if (IsTrackableStatus(booking.CurrentRideStatus))
{
    <div class="card mt-3">
        <div class="card-header">
            <h5>?? Live Tracking</h5>
        </div>
        <div class="card-body">
            @if (currentLocation != null)
            {
                <p><strong>Location:</strong> (@currentLocation.Latitude, @currentLocation.Longitude)</p>
                <p><strong>Speed:</strong> @currentLocation.Speed mph</p>
                <p><strong>Last Update:</strong> @currentLocation.TimeSince</p>
                
                <div class="btn-group">
                    <button class="btn btn-primary" @onclick="ViewOnMap">
                        View on Live Map
                    </button>
                    <button class="btn btn-secondary" @onclick="RefreshLocation">
                        Refresh
                    </button>
                </div>
            }
            else
            {
                <p>Loading location...</p>
            }
        </div>
    </div>
}
```

---

## ?? Dual Status Model

### Status vs CurrentRideStatus

The system uses two status fields for different purposes:

| Field | Purpose | Values | Audience |
|-------|---------|--------|----------|
| `Status` | Booking-level status | Requested, Confirmed, Scheduled, InProgress, Completed, Cancelled, NoShow | Reports, accounting, business analytics |
| `CurrentRideStatus` | Real-time driver state | Scheduled, OnRoute, Arrived, PassengerOnboard, Completed, Cancelled | Dispatchers, real-time operations |

**Display Priority**:
```csharp
// Always prefer CurrentRideStatus when available
string displayStatus = booking.CurrentRideStatus ?? booking.Status ?? "Unknown";
```

**Example Scenarios**:

| CurrentRideStatus | Status | UI Displays | Badge Color |
|-------------------|--------|-------------|-------------|
| `OnRoute` | `Scheduled` | **OnRoute** | Blue (bg-info) |
| `Arrived` | `Scheduled` | **Arrived** | Yellow (bg-warning) |
| `PassengerOnboard` | `InProgress` | **PassengerOnboard** | Green (bg-success) |
| `null` | `Scheduled` | **Scheduled** | Gray (bg-secondary) |
| `Completed` | `Completed` | **Completed** | Green (bg-success) |

**Why Two Fields?**:
- **Accounting needs**: "How many rides were completed this month?" ? Use `Status`
- **Dispatcher needs**: "Is the driver on the way?" ? Use `CurrentRideStatus`
- **No data loss**: Status transitions don't overwrite historical state

---

## ?? Timezone Support

### PickupDateTimeOffset

**Problem**: Bookings for different timezones displayed incorrectly

**Solution**: API returns `PickupDateTimeOffset` with explicit timezone

**API Response**:
```json
{
  "pickupDateTime": "2025-12-24T15:00:00Z",                  // Raw UTC (legacy)
  "pickupDateTimeOffset": "2025-12-24T09:00:00-06:00"        // Central Time with offset
}
```

**Display Logic**:
```csharp
// Prefer PickupDateTimeOffset when available
var displayTime = booking.PickupDateTimeOffset?.LocalDateTime 
    ?? booking.PickupDateTime.ToLocalTime();
```

**Result**: Times display correctly for worldwide operations

---

## ?? REST API Fallback

### Polling Mechanism

When SignalR is unavailable, the portal falls back to HTTP polling:

```csharp
private async Task StartPollingAsync()
{
    _pollingTimer = new Timer(async _ =>
    {
        try
        {
            var locations = await GetAllActiveLocationsAsync();
            foreach (var loc in locations)
            {
                LocationUpdated?.Invoke(this, loc);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DriverTrackingService] Polling error: {ex.Message}");
        }
    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
}
```

**Polling Interval**: 15 seconds

**Endpoints Used**:
- `GET /admin/locations` - All active driver locations
- `GET /driver/location/{rideId}` - Single ride location

---

## ?? Connection Management

### Automatic Reconnection

**Exponential Backoff Strategy**:
```csharp
.WithAutomaticReconnect(new[]
{
    TimeSpan.Zero,           // Immediate retry
    TimeSpan.FromSeconds(2), // 2 second delay
    TimeSpan.FromSeconds(5), // 5 second delay
    TimeSpan.FromSeconds(10) // 10 second delay
})
```

**Connection States**:
- **Connected** (Green badge): SignalR active, real-time updates
- **Disconnected** (Red badge): Polling mode, 15-second refresh

**State Change Events**:
```csharp
_hubConnection.Reconnecting += ex =>
{
    Console.WriteLine($"[DriverTrackingService] Reconnecting...");
    ConnectionStateChanged?.Invoke(this, false);
    return Task.CompletedTask;
};

_hubConnection.Reconnected += connectionId =>
{
    Console.WriteLine($"[DriverTrackingService] Reconnected!");
    ConnectionStateChanged?.Invoke(this, true);
    return Task.CompletedTask;
};
```

---

## ?? Resource Cleanup

### Proper Disposal

**IAsyncDisposable Implementation**:
```csharp
public async ValueTask DisposeAsync()
{
    if (_hubConnection != null)
    {
        await _hubConnection.DisposeAsync();
    }

    _pollingTimer?.Dispose();
}
```

**Component Disposal** (LiveTracking.razor):
```csharp
public async ValueTask DisposeAsync()
{
    // Unsubscribe from events
    if (DriverTrackingService != null)
    {
        DriverTrackingService.LocationUpdated -= OnLocationUpdated;
        DriverTrackingService.StatusChanged -= OnRideStatusChanged;
        DriverTrackingService.TrackingStopped -= OnTrackingStopped;
    }

    // Dispose map
    if (mapInitialized)
    {
        await JS.InvokeVoidAsync("disposeTrackingMap");
    }
}
```

**Importance**:
- ? Prevents memory leaks
- ? Closes WebSocket connections properly
- ? Cleans up JavaScript map resources
- ? Removes event handlers

---

## ?? Data Models

### LocationUpdate DTO

```csharp
public class LocationUpdate
{
    public string RideId { get; set; }
    public string DriverUid { get; set; }
    public string DriverName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Heading { get; set; }       // Degrees (0-360)
    public double? Speed { get; set; }         // mph
    public double? Accuracy { get; set; }      // meters
    public DateTime Timestamp { get; set; }
}
```

### ActiveRideLocationDto

```csharp
public class ActiveRideLocationDto
{
    public string RideId { get; set; }
    public string DriverName { get; set; }
    public string PassengerName { get; set; }
    public string PickupLocation { get; set; }
    public string DropoffLocation { get; set; }
    public string CurrentStatus { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public DateTime LastUpdate { get; set; }
    public string TimeSince { get; set; }     // "2 minutes ago"
}
```

**See**: [22-Data-Models.md](22-Data-Models.md) for complete model documentation

---

## ?? Configuration

### Required Settings

**appsettings.json**:
```json
{
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206"
  },
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  }
}
```

### Optional: Google Maps API Key

**Without API Key**:
- Map shows placeholder message
- Sidebar and real-time updates still functional
- Polling fallback works

**With API Key**:
- Full interactive map
- Driver markers with animations
- Dark theme styling
- Zoom and pan controls

**Get API Key**:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Enable "Maps JavaScript API"
3. Create credentials ? API key
4. Add to `appsettings.json`
5. Restart application

---

## ?? Testing

### Manual Testing Procedures

**Test 1: Real-Time Updates**:
1. Open AdminPortal LiveTracking page
2. Have driver send GPS update (DriverApp)
3. **Verify**: Marker appears/moves on map
4. **Verify**: Sidebar shows updated speed and time
5. **Verify**: Connection status shows "Connected"

**Test 2: Multi-Page Sync**:
1. Open 3 browser tabs:
   - Tab 1: Bookings dashboard
   - Tab 2: BookingDetail for ride ABC123
   - Tab 3: LiveTracking map
2. Driver (ABC123) changes status to OnRoute
3. **Verify**: All 3 tabs update simultaneously

**Test 3: SignalR Reconnection**:
1. Open LiveTracking page
2. Stop AdminAPI server
3. **Verify**: Status changes to "Disconnected" (red)
4. **Verify**: Polling mode activates
5. Restart AdminAPI server
6. **Verify**: Status changes to "Connected" (green) within 10 seconds

**Test 4: Polling Fallback**:
1. Disable WebSocket in firewall
2. Open LiveTracking page
3. **Verify**: Map still updates every 15 seconds
4. **Verify**: Connection status shows "Disconnected" (polling mode)

**See**: [02-Testing-Guide.md](02-Testing-Guide.md) for comprehensive testing procedures

---

## ?? Future Enhancements

### Planned Features

1. **Route History Playback**
   - Display driver's path as polyline on map
   - Scrub through historical GPS data
   - Analyze route efficiency

2. **ETA Calculations**
   - Use current speed and distance to estimate arrival
   - Factor in traffic data (Google Maps API)
   - Display countdown timer for passenger

3. **Geofencing Alerts**
   - Define zones (airports, downtown, etc.)
   - Notify when driver enters/exits zone
   - Automatic status updates based on location

4. **Heatmap View**
   - Visualize driver density across city
   - Identify high-demand areas
   - Optimize driver positioning

5. **Driver-Specific Tracking**
   - Admin can track individual drivers across multiple rides
   - Driver performance metrics
   - Historical location data

---

## ?? Related Documentation

- [SignalR Reference](21-SignalR-Reference.md) - Complete SignalR events documentation
- [API Reference](20-API-Reference.md) - Location endpoints
- [Data Models](22-Data-Models.md) - DTOs and schemas
- [System Architecture](01-System-Architecture.md) - Overall design
- [Security Model](23-Security-Model.md) - JWT authentication for SignalR

---

**Last Updated**: January 17, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*Real-time GPS tracking provides dispatchers with immediate visibility into driver locations and ride progress, enabling better customer service and operational efficiency.* ???
