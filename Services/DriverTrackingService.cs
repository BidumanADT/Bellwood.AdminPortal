using Bellwood.AdminPortal.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Interface for accessing real-time driver location data
/// </summary>
public interface IDriverTrackingService : IAsyncDisposable
{
    /// <summary>
    /// Event fired when a location update is received via SignalR
    /// </summary>
    event EventHandler<LocationUpdate>? LocationUpdated;
    
    /// <summary>
    /// Event fired when tracking stops for a ride
    /// </summary>
    event EventHandler<TrackingStoppedEventArgs>? TrackingStopped;
    
    /// <summary>
    /// Event fired when a ride's status changes (OnRoute, Arrived, etc.)
    /// </summary>
    event EventHandler<RideStatusChangedEvent>? RideStatusChanged;
    
    /// <summary>
    /// Event fired when connection state changes
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;
    
    /// <summary>
    /// Indicates if the SignalR connection is active
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Connect to the SignalR hub for real-time updates
    /// </summary>
    Task ConnectAsync();
    
    /// <summary>
    /// Disconnect from the SignalR hub
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Subscribe to location updates for a specific ride
    /// </summary>
    Task SubscribeToRideAsync(string rideId);
    
    /// <summary>
    /// Unsubscribe from location updates for a specific ride
    /// </summary>
    Task UnsubscribeFromRideAsync(string rideId);
    
    /// <summary>
    /// Subscribe to location updates for a specific driver (admin only)
    /// </summary>
    Task SubscribeToDriverAsync(string driverUid);
    
    /// <summary>
    /// Unsubscribe from location updates for a specific driver
    /// </summary>
    Task UnsubscribeFromDriverAsync(string driverUid);
    
    /// <summary>
    /// Get the current location for a specific ride (polling fallback)
    /// </summary>
    Task<LocationResponse?> GetRideLocationAsync(string rideId);
    
    /// <summary>
    /// Get all active driver locations (admin dashboard)
    /// </summary>
    Task<List<ActiveRideLocationDto>> GetAllActiveLocationsAsync();
    
    /// <summary>
    /// Get locations for multiple rides in a batch
    /// </summary>
    Task<List<LocationResponse>> GetRideLocationsAsync(IEnumerable<string> rideIds);
}

public class TrackingStoppedEventArgs : EventArgs
{
    public string RideId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing real-time driver tracking via SignalR and REST fallback
/// </summary>
public class DriverTrackingService : IDriverTrackingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DriverTrackingService> _logger;
    
    private HubConnection? _hubConnection;
    private readonly HashSet<string> _subscribedRides = new();
    private readonly HashSet<string> _subscribedDrivers = new();
    
    public event EventHandler<LocationUpdate>? LocationUpdated;
    public event EventHandler<TrackingStoppedEventArgs>? TrackingStopped;
    public event EventHandler<RideStatusChangedEvent>? RideStatusChanged;
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    
    public DriverTrackingService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        IConfiguration configuration,
        ILogger<DriverTrackingService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
        {
            _logger.LogDebug("SignalR connection already active or connecting");
            return;
        }
        
        try
        {
            var token = await _tokenProvider.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Cannot connect to SignalR hub without authentication token");
                return;
            }
            
            // Get the AdminAPI base URL from configuration or use default
            var adminApiBaseUrl = _configuration["AdminAPI:BaseUrl"] ?? "https://localhost:5206";
            var hubUrl = $"{adminApiBaseUrl}/hubs/location?access_token={Uri.EscapeDataString(token)}";
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // For development, accept any certificate
                    #if DEBUG
                    options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    #endif
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();
            
            // Register event handlers
            _hubConnection.On<LocationUpdate>("LocationUpdate", OnLocationUpdate);
            _hubConnection.On<string, string>("TrackingStopped", OnTrackingStopped);
            _hubConnection.On<RideStatusChangedEvent>("RideStatusChanged", OnRideStatusChanged);
            _hubConnection.On<string>("SubscriptionConfirmed", OnSubscriptionConfirmed);
            
            _hubConnection.Closed += OnHubClosed;
            _hubConnection.Reconnected += OnHubReconnected;
            _hubConnection.Reconnecting += OnHubReconnecting;
            
            await _hubConnection.StartAsync();
            
            _logger.LogInformation("Connected to SignalR location hub");
            ConnectionStateChanged?.Invoke(this, true);
            
            // Resubscribe to any previous subscriptions
            foreach (var rideId in _subscribedRides.ToList())
            {
                await _hubConnection.InvokeAsync("SubscribeToRide", rideId);
            }
            foreach (var driverUid in _subscribedDrivers.ToList())
            {
                await _hubConnection.InvokeAsync("SubscribeToDriver", driverUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR location hub");
            ConnectionStateChanged?.Invoke(this, false);
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disconnecting from SignalR hub");
            }
            finally
            {
                _hubConnection = null;
                _subscribedRides.Clear();
                _subscribedDrivers.Clear();
                ConnectionStateChanged?.Invoke(this, false);
            }
        }
    }
    
    public async Task SubscribeToRideAsync(string rideId)
    {
        _subscribedRides.Add(rideId);
        
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("SubscribeToRide", rideId);
                _logger.LogDebug("Subscribed to ride {RideId}", rideId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to ride {RideId}", rideId);
            }
        }
    }
    
    public async Task UnsubscribeFromRideAsync(string rideId)
    {
        _subscribedRides.Remove(rideId);
        
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromRide", rideId);
                _logger.LogDebug("Unsubscribed from ride {RideId}", rideId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unsubscribe from ride {RideId}", rideId);
            }
        }
    }
    
    public async Task SubscribeToDriverAsync(string driverUid)
    {
        _subscribedDrivers.Add(driverUid);
        
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("SubscribeToDriver", driverUid);
                _logger.LogDebug("Subscribed to driver {DriverUid}", driverUid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to driver {DriverUid}", driverUid);
            }
        }
    }
    
    public async Task UnsubscribeFromDriverAsync(string driverUid)
    {
        _subscribedDrivers.Remove(driverUid);
        
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromDriver", driverUid);
                _logger.LogDebug("Unsubscribed from driver {DriverUid}", driverUid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unsubscribe from driver {DriverUid}", driverUid);
            }
        }
    }
    
    public async Task<LocationResponse?> GetRideLocationAsync(string rideId)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            return await client.GetFromJsonAsync<LocationResponse>($"/driver/location/{rideId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Access denied to location for ride {RideId}. User may not have permission.", rideId);
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("No location data found for ride {RideId}", rideId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location for ride {RideId}", rideId);
            return null;
        }
    }
    
    public async Task<List<ActiveRideLocationDto>> GetAllActiveLocationsAsync()
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            
            // API returns envelope format: { count, locations[], timestamp }
            var envelope = await client.GetFromJsonAsync<LocationsResponse>("/admin/locations");
            
            if (envelope == null)
            {
                _logger.LogWarning("Received null response from /admin/locations endpoint");
                return new();
            }
            
            _logger.LogDebug("Loaded {Count} active locations from API", envelope.Count);
            return envelope.Locations;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Access denied to admin locations endpoint. User may not have admin role.");
            throw new UnauthorizedAccessException("You do not have permission to view location data. Admin role required.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all active locations");
            return new();
        }
    }
    
    public async Task<List<LocationResponse>> GetRideLocationsAsync(IEnumerable<string> rideIds)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var rideIdsParam = string.Join(",", rideIds);
            return await client.GetFromJsonAsync<List<LocationResponse>>($"/admin/locations/rides?rideIds={Uri.EscapeDataString(rideIdsParam)}") ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get locations for rides");
            return new();
        }
    }
    
    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var client = _httpFactory.CreateClient("AdminAPI");
        
        var apiKey = _apiKeyProvider.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-ApiKey", apiKey);
        }
        
        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        return client;
    }
    
    private void OnLocationUpdate(LocationUpdate update)
    {
        _logger.LogDebug("Received location update for ride {RideId}: {Lat}, {Lng}", 
            update.RideId, update.Latitude, update.Longitude);
        LocationUpdated?.Invoke(this, update);
    }
    
    private void OnRideStatusChanged(RideStatusChangedEvent evt)
    {
        _logger.LogInformation("Ride {RideId} status changed to {NewStatus} by {DriverName}", 
            evt.RideId, evt.NewStatus, evt.DriverName);
        RideStatusChanged?.Invoke(this, evt);
    }
    
    private void OnTrackingStopped(string rideId, string reason)
    {
        _logger.LogInformation("Tracking stopped for ride {RideId}: {Reason}", rideId, reason);
        _subscribedRides.Remove(rideId);
        TrackingStopped?.Invoke(this, new TrackingStoppedEventArgs { RideId = rideId, Reason = reason });
    }
    
    private void OnSubscriptionConfirmed(string message)
    {
        _logger.LogDebug("Subscription confirmed: {Message}", message);
    }
    
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
    
    private Task OnHubReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
        ConnectionStateChanged?.Invoke(this, true);
        return Task.CompletedTask;
    }
    
    private Task OnHubReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR reconnecting...");
        return Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
