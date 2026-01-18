# SignalR Events Reference

**Document Type**: Living Document - Technical Reference  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides a complete reference for all SignalR events, hub methods, and real-time communication patterns used by the Bellwood AdminPortal for live driver tracking and status updates.

**Hub URL**: `{AdminAPI BaseUrl}/hubs/location`  
**Example**: `wss://localhost:5206/hubs/location`

**Protocol**: WebSocket (with automatic fallback to Long Polling)

**Authentication**: JWT token via query string (`?access_token={jwt}`)

**Target Audience**: Developers, integration engineers  
**Prerequisites**: Understanding of SignalR, WebSocket communication, real-time patterns

---

## ?? Connection Setup

### Client-Side Connection (AdminPortal)

**File**: `Services/DriverTrackingService.cs`

**Connection Code**:
```csharp
// Get JWT token for authentication
var token = await _tokenProvider.GetTokenAsync();

// Build hub URL with token in query string
var adminApiBaseUrl = _configuration["AdminAPI:BaseUrl"] ?? "https://localhost:5206";
var hubUrl = $"{adminApiBaseUrl}/hubs/location?access_token={Uri.EscapeDataString(token)}";

// Create hub connection with automatic reconnection
_hubConnection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options =>
    {
        #if DEBUG
        // Accept any SSL certificate in development
        options.HttpMessageHandlerFactory = _ => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        #endif
    })
    .WithAutomaticReconnect(new[] { 
        TimeSpan.Zero,          // Immediate retry
        TimeSpan.FromSeconds(2), // Then 2 seconds
        TimeSpan.FromSeconds(5), // Then 5 seconds
        TimeSpan.FromSeconds(10) // Then 10 seconds
    })
    .Build();

// Register event handlers (before connecting)
_hubConnection.On<LocationUpdate>("LocationUpdate", OnLocationUpdate);
_hubConnection.On<string, string>("TrackingStopped", OnTrackingStopped);
_hubConnection.On<RideStatusChangedEvent>("RideStatusChanged", OnRideStatusChanged);
_hubConnection.On<string>("SubscriptionConfirmed", OnSubscriptionConfirmed);

// Register lifecycle handlers
_hubConnection.Closed += OnHubClosed;
_hubConnection.Reconnected += OnHubReconnected;
_hubConnection.Reconnecting += OnHubReconnecting;

// Connect
await _hubConnection.StartAsync();
```

---

### Connection Lifecycle Events

#### Event: Closed

**Trigger**: Connection terminates (network failure, server shutdown)

**Handler**:
```csharp
private Task OnHubClosed(Exception? exception)
{
    if (exception != null)
    {
        _logger.LogWarning(exception, "SignalR connection closed with error");
    }
    else
    {
        _logger.LogInformation("SignalR connection closed");
    }
    ConnectionStateChanged?.Invoke(this, false);
    return Task.CompletedTask;
}
```

**Action**: Fire `ConnectionStateChanged` event with `false` to update UI

---

#### Event: Reconnected

**Trigger**: Connection successfully re-established after disconnect

**Handler**:
```csharp
private Task OnHubReconnected(string? connectionId)
{
    _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
    ConnectionStateChanged?.Invoke(this, true);
    
    // Resubscribe to previous subscriptions
    foreach (var rideId in _subscribedRides.ToList())
    {
        await _hubConnection.InvokeAsync("SubscribeToRide", rideId);
    }
    
    return Task.CompletedTask;
}
```

**Action**: Resubscribe to all previously subscribed rides/drivers

---

#### Event: Reconnecting

**Trigger**: Connection lost, attempting to reconnect

**Handler**:
```csharp
private Task OnHubReconnecting(Exception? exception)
{
    _logger.LogWarning(exception, "SignalR reconnecting...");
    return Task.CompletedTask;
}
```

**Action**: Log reconnection attempt, optionally show UI indicator

---

## ?? Server-to-Client Events

### Event: LocationUpdate

**Purpose**: Real-time GPS location update from a driver

**Triggered By**: Driver sends location update via DriverApp (POST `/driver/location/update`)

**Frequency**: Every 5-10 seconds while driver is on a ride

**Event Signature**:
```csharp
_hubConnection.On<LocationUpdate>("LocationUpdate", OnLocationUpdate);
```

**Payload**:
```csharp
public class LocationUpdate
{
    public string RideId { get; set; }           // Booking ID
    public double Latitude { get; set; }         // GPS latitude
    public double Longitude { get; set; }        // GPS longitude
    public DateTime Timestamp { get; set; }       // When location was captured
    public double? Heading { get; set; }          // Direction (0-360°, 0 = North)
    public double? Speed { get; set; }            // Speed in meters/second
    public double? Accuracy { get; set; }         // GPS accuracy in meters
    public string? DriverName { get; set; }       // Driver's name
    public string? DriverUid { get; set; }        // Driver's AuthServer UID
}
```

**Example Payload**:
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

**Speed Conversion**:
```csharp
// Convert m/s to mph
double speedMph = speedMetersPerSecond * 2.23694;

// Convert m/s to km/h
double speedKmh = speedMetersPerSecond * 3.6;
```

**Handler Example**:
```csharp
private void OnLocationUpdate(LocationUpdate update)
{
    _logger.LogDebug("Received location update for ride {RideId}: {Lat}, {Lng}", 
        update.RideId, update.Latitude, update.Longitude);
    
    // Update map marker
    LocationUpdated?.Invoke(this, update);
    
    // Calculate age
    var age = DateTime.UtcNow - update.Timestamp;
    if (age.TotalSeconds > 30)
    {
        _logger.LogWarning("Location update is {Seconds}s old", age.TotalSeconds);
    }
}
```

**Usage in LiveTracking Page**:
```csharp
protected override async Task OnInitializedAsync()
{
    TrackingService.LocationUpdated += OnLocationUpdated;
    await TrackingService.ConnectAsync();
}

private async void OnLocationUpdated(object? sender, LocationUpdate update)
{
    await InvokeAsync(async () =>
    {
        // Update JavaScript map via interop
        await JSRuntime.InvokeVoidAsync("updateMarker", 
            update.RideId, 
            update.Latitude, 
            update.Longitude, 
            update.Heading ?? 0);
        
        StateHasChanged();
    });
}
```

**See**: [Real-Time Tracking](10-Real-Time-Tracking.md) for complete implementation

---

### Event: RideStatusChanged

**Purpose**: Notify when driver changes ride status (OnRoute, Arrived, etc.)

**Triggered By**: Driver changes status in DriverApp

**Event Signature**:
```csharp
_hubConnection.On<RideStatusChangedEvent>("RideStatusChanged", OnRideStatusChanged);
```

**Payload**:
```csharp
public class RideStatusChangedEvent
{
    public string RideId { get; set; }          // Booking ID
    public string DriverUid { get; set; }       // Driver's AuthServer UID
    public string? DriverName { get; set; }     // Driver's name
    public string? PassengerName { get; set; }  // Passenger name
    public string NewStatus { get; set; }       // New status value
    public DateTime Timestamp { get; set; }     // When status changed
}
```

**Valid Status Values**:
- `Accepted` - Driver accepted the ride
- `OnRoute` - Driver is driving to pickup location
- `Arrived` - Driver has arrived at pickup
- `PassengerOnboard` - Passenger is in the vehicle
- `DropoffComplete` - Ride completed successfully
- `Cancelled` - Ride cancelled by driver

**Example Payload**:
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

**Handler Example**:
```csharp
private void OnRideStatusChanged(RideStatusChangedEvent evt)
{
    _logger.LogInformation("Ride {RideId} status changed to {NewStatus} by {DriverName}", 
        evt.RideId, evt.NewStatus, evt.DriverName);
    
    RideStatusChanged?.Invoke(this, evt);
}
```

**Usage in Bookings Page**:
```csharp
// Subscribe to status updates
TrackingService.RideStatusChanged += OnRideStatusChanged;

// Handle status change
private async void OnRideStatusChanged(object? sender, RideStatusChangedEvent evt)
{
    await InvokeAsync(() =>
    {
        // Find booking in list
        var booking = allBookings.FirstOrDefault(b => b.Id == evt.RideId);
        if (booking != null)
        {
            // Update status
            booking.CurrentRideStatus = evt.NewStatus;
            
            // Re-filter list
            FilterBookings(selectedFilter);
            StateHasChanged();
        }
    });
}
```

**Multi-Page Sync**: This event is received by **all connected admin users**, ensuring status updates appear on:
- Bookings dashboard
- Booking detail page
- Live Tracking map

---

### Event: TrackingStopped

**Purpose**: Notify when tracking stops for a ride (completed or cancelled)

**Triggered By**: Ride reaches terminal status (DropoffComplete, Cancelled)

**Event Signature**:
```csharp
_hubConnection.On<string, string>("TrackingStopped", OnTrackingStopped);
```

**Parameters**:
1. `rideId` (string): Booking ID
2. `reason` (string): Why tracking stopped

**Reason Values**:
- `"RideCompleted"` - Ride finished successfully
- `"RideCancelled"` - Ride was cancelled
- `"DriverDisconnected"` - Driver lost connection for >5 minutes

**Handler Example**:
```csharp
private void OnTrackingStopped(string rideId, string reason)
{
    _logger.LogInformation("Tracking stopped for ride {RideId}: {Reason}", rideId, reason);
    
    // Remove from subscriptions
    _subscribedRides.Remove(rideId);
    
    // Fire event for UI
    TrackingStopped?.Invoke(this, new TrackingStoppedEventArgs 
    { 
        RideId = rideId, 
        Reason = reason 
    });
}
```

**Usage in LiveTracking**:
```csharp
TrackingService.TrackingStopped += OnTrackingStopped;

private async void OnTrackingStopped(object? sender, TrackingStoppedEventArgs args)
{
    await InvokeAsync(async () =>
    {
        // Remove marker from map
        await JSRuntime.InvokeVoidAsync("removeMarker", args.RideId);
        
        // Remove from active rides list
        activeRides.RemoveAll(r => r.RideId == args.RideId);
        
        // Show notification
        await JSRuntime.InvokeVoidAsync("showNotification", 
            $"Ride {args.RideId} tracking stopped: {args.Reason}");
        
        StateHasChanged();
    });
}
```

---

### Event: SubscriptionConfirmed

**Purpose**: Server confirms successful subscription

**Triggered By**: Client calls `SubscribeToRide` or `SubscribeToDriver`

**Event Signature**:
```csharp
_hubConnection.On<string>("SubscriptionConfirmed", OnSubscriptionConfirmed);
```

**Parameter**: Message (string) with confirmation details

**Example Messages**:
- `"Subscribed to ride bk-2025-001"`
- `"Subscribed to driver driver-001"`

**Handler Example**:
```csharp
private void OnSubscriptionConfirmed(string message)
{
    _logger.LogDebug("Subscription confirmed: {Message}", message);
}
```

---

## ?? Client-to-Server Methods

### Method: SubscribeToRide

**Purpose**: Subscribe to location updates for a specific ride

**Signature**:
```csharp
await _hubConnection.InvokeAsync("SubscribeToRide", rideId);
```

**Parameters**:
- `rideId` (string): Booking ID to subscribe to

**Server Action**:
1. Adds connection to ride's subscriber group
2. Sends `SubscriptionConfirmed` event
3. Future `LocationUpdate` events for this ride sent to this connection

**Usage**:
```csharp
public async Task SubscribeToRideAsync(string rideId)
{
    _subscribedRides.Add(rideId);
    
    if (_hubConnection?.State == HubConnectionState.Connected)
    {
        await _hubConnection.InvokeAsync("SubscribeToRide", rideId);
        _logger.LogDebug("Subscribed to ride {RideId}", rideId);
    }
}
```

**When to Use**:
- User opens booking detail page
- User clicks on booking in Live Tracking
- Admin wants to monitor specific ride

---

### Method: UnsubscribeFromRide

**Purpose**: Stop receiving updates for a ride

**Signature**:
```csharp
await _hubConnection.InvokeAsync("UnsubscribeFromRide", rideId);
```

**Parameters**:
- `rideId` (string): Booking ID to unsubscribe from

**Server Action**:
1. Removes connection from ride's subscriber group
2. No more `LocationUpdate` events sent for this ride

**Usage**:
```csharp
public async Task UnsubscribeFromRideAsync(string rideId)
{
    _subscribedRides.Remove(rideId);
    
    if (_hubConnection?.State == HubConnectionState.Connected)
    {
        await _hubConnection.InvokeAsync("UnsubscribeFromRide", rideId);
        _logger.LogDebug("Unsubscribed from ride {RideId}", rideId);
    }
}
```

**When to Use**:
- User navigates away from booking detail
- Ride completes and no longer needs tracking
- Component disposal (cleanup)

---

### Method: SubscribeToDriver

**Purpose**: Subscribe to all rides for a specific driver (admin only)

**Signature**:
```csharp
await _hubConnection.InvokeAsync("SubscribeToDriver", driverUid);
```

**Parameters**:
- `driverUid` (string): Driver's AuthServer UID

**Server Action**:
1. Validates admin role (403 if not admin)
2. Subscribes to all current and future rides for this driver
3. Sends confirmation

**Usage**:
```csharp
public async Task SubscribeToDriverAsync(string driverUid)
{
    _subscribedDrivers.Add(driverUid);
    
    if (_hubConnection?.State == HubConnectionState.Connected)
    {
        await _hubConnection.InvokeAsync("SubscribeToDriver", driverUid);
        _logger.LogDebug("Subscribed to driver {DriverUid}", driverUid);
    }
}
```

**When to Use**:
- Monitoring specific driver's performance
- Debugging driver issues
- Fleet management view

**Authorization**: Requires **admin** role

---

### Method: UnsubscribeFromDriver

**Purpose**: Stop receiving updates for a driver

**Signature**:
```csharp
await _hubConnection.InvokeAsync("UnsubscribeFromDriver", driverUid);
```

**Parameters**:
- `driverUid` (string): Driver's AuthServer UID

**Usage**:
```csharp
public async Task UnsubscribeFromDriverAsync(string driverUid)
{
    _subscribedDrivers.Remove(driverUid);
    
    if (_hubConnection?.State == HubConnectionState.Connected)
    {
        await _hubConnection.InvokeAsync("UnsubscribeFromDriver", driverUid);
    }
}
```

---

## ?? Event Flow Diagrams

### Location Update Flow

```
DriverApp                   AdminAPI                   AdminPortal
   |                           |                           |
   |  POST /driver/location    |                           |
   |-------------------------->|                           |
   |  { lat, lng, speed }      |                           |
   |                           |                           |
   |       200 OK              |                           |
   |<--------------------------|                           |
   |                           |                           |
   |                           |  LocationUpdate Event     |
   |                           |-------------------------->|
   |                           |  { rideId, lat, lng }     |
   |                           |                           |
   |                           |                           | Update Map
   |                           |                           |---.
   |                           |                           |   |
   |                           |                           |<--'
```

---

### Subscription Flow

```
AdminPortal                 AdminAPI SignalR Hub
   |                              |
   |  SubscribeToRide(bk-001)     |
   |----------------------------->|
   |                              | Add to group
   |                              |---.
   |                              |   |
   |  SubscriptionConfirmed       |<--'
   |<-----------------------------|
   |                              |
   |         (Later)              |
   |                              |
   |  LocationUpdate (bk-001)     |
   |<-----------------------------|
   |                              |
```

---

### Status Change Flow

```
DriverApp           AdminAPI            SignalR Hub         AdminPortal (Bookings)    AdminPortal (Tracking)
   |                   |                     |                       |                        |
   | Status: Arrived   |                     |                       |                        |
   |------------------>|                     |                       |                        |
   |                   | Update DB           |                       |                        |
   |                   |---.                 |                       |                        |
   |                   |   |                 |                       |                        |
   |    200 OK         |<--'                 |                       |                        |
   |<------------------|                     |                       |                        |
   |                   |                     |                       |                        |
   |                   | RideStatusChanged   |                       |                        |
   |                   |-------------------->|                       |                        |
   |                   |                     | Broadcast to all      |                        |
   |                   |                     |---------------------->|                        |
   |                   |                     |                       | Update booking card    |
   |                   |                     |                       |                        |
   |                   |                     |---------------------------------------------->|
   |                   |                     |                       |             Update map marker
```

---

## ? Performance Characteristics

### Message Frequency

| Event | Typical Frequency | Max Rate |
|-------|-------------------|----------|
| LocationUpdate | Every 5-10 seconds | 6/minute per ride |
| RideStatusChanged | On user action | Variable |
| TrackingStopped | On ride completion | Once per ride |

### Bandwidth Estimates

**Per Active Ride**:
- LocationUpdate: ~200 bytes × 6/min = **1.2 KB/min**
- RideStatusChanged: ~150 bytes × occasional = **< 0.1 KB/min**

**Total for 50 Active Rides**: ~60 KB/min (~1 KB/second)

**Efficient**: Minimal bandwidth usage, WebSocket overhead low

---

### Connection Limits

**Server Side** (AdminAPI):
- Concurrent connections: Limited by server resources
- Recommended max: 1000 concurrent admin users
- Each admin can subscribe to unlimited rides

**Client Side** (AdminPortal):
- One connection per browser tab
- Connection shared across components via singleton service
- Automatic reconnection prevents connection exhaustion

---

## ?? Security Considerations

### Authentication

**JWT in Query String**:
```
wss://localhost:5206/hubs/location?access_token={jwt}
```

**?? Security Note**: Query string visible in logs

**Alternative** (more secure):
```csharp
options.AccessTokenProvider = async () => await GetTokenAsync();
```

**Recommendation**: Use header-based auth in production

---

### Authorization

**Role-Based Access**:
- `SubscribeToRide`: Requires authenticated user
- `SubscribeToDriver`: Requires **admin** role
- `LocationUpdate`: Server validates driver owns the ride

**Phase 2 Enhancement**: Filter subscriptions by user role
- **booker**: Can only subscribe to their own bookings
- **admin/dispatcher**: Can subscribe to any ride

---

## ?? Error Handling

### Connection Failures

**Automatic Retry**: Built-in with `WithAutomaticReconnect()`

**Retry Strategy**:
1. Immediate retry (0s delay)
2. 2 seconds delay
3. 5 seconds delay
4. 10 seconds delay
5. Give up (requires manual reconnect)

**Handling**:
```csharp
_hubConnection.Closed += async (exception) =>
{
    // After all retries exhausted
    _logger.LogError("SignalR connection closed permanently");
    
    // Optionally show UI message
    await ShowReconnectButtonAsync();
};
```

---

### Event Delivery Guarantees

**At-Most-Once Delivery**: SignalR does not guarantee message delivery

**Mitigation Strategies**:
1. **Polling Fallback**: If no `LocationUpdate` for 30+ seconds, poll REST API
2. **Sequence Numbers**: Include sequence in events to detect gaps
3. **State Reconciliation**: Periodically refresh from REST API

**Example Fallback**:
```csharp
private Timer _pollingTimer;

// If SignalR fails, start polling
if (!TrackingService.IsConnected)
{
    _pollingTimer = new Timer(async _ =>
    {
        var location = await GetRideLocationAsync(rideId);
        if (location != null)
        {
            UpdateMap(location);
        }
    }, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
}
```

---

## ?? Testing SignalR Events

### Unit Testing Event Handlers

**Mock Hub Connection**:
```csharp
var mockHub = new Mock<HubConnection>();
mockHub.Setup(h => h.On<LocationUpdate>("LocationUpdate", It.IsAny<Action<LocationUpdate>>()))
       .Callback<string, Action<LocationUpdate>>((name, handler) =>
       {
           // Store handler to invoke later
           _locationUpdateHandler = handler;
       });

var service = new DriverTrackingService(mockHub.Object);

// Simulate event
var update = new LocationUpdate { RideId = "test", Latitude = 41.0, Longitude = -87.0 };
_locationUpdateHandler(update);

// Assert handler was called
Assert.Equal("test", capturedRideId);
```

---

### Integration Testing

**Test SignalR Connection**:
```powershell
# Install SignalR test client
dotnet tool install -g Microsoft.AspNetCore.SignalR.Client.Testing

# Connect to hub
dotnet signalr test https://localhost:5206/hubs/location

# Subscribe to ride
invoke SubscribeToRide bk-001

# Wait for events
listen LocationUpdate
```

**See**: [Testing Guide](02-Testing-Guide.md) for comprehensive testing procedures

---

## ?? Related Documentation

- [Real-Time Tracking](10-Real-Time-Tracking.md) - GPS tracking implementation
- [API Reference](20-API-Reference.md) - REST API endpoints
- [System Architecture](01-System-Architecture.md) - Overall design
- [Data Models](22-Data-Models.md) - Event payload schemas
- [Driver App Integration](Archive/DRIVER_APP_COMPLETE_INTEGRATION_GUIDE.md) - Mobile app setup

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This SignalR reference documents all real-time events and hub methods. Keep this updated when adding new events or modifying payloads.* ??
