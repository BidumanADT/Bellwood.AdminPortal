namespace Bellwood.AdminPortal.Models;

/// <summary>
/// Represents a real-time location update from a driver
/// </summary>
public class LocationUpdate
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    
    /// <summary>Direction of travel in degrees (0-360, 0 = North)</summary>
    public double? Heading { get; set; }
    
    /// <summary>Current speed in meters/second</summary>
    public double? Speed { get; set; }
    
    /// <summary>Location accuracy in meters</summary>
    public double? Accuracy { get; set; }
    
    /// <summary>Driver's name for display</summary>
    public string? DriverName { get; set; }
    
    /// <summary>Driver's unique identifier</summary>
    public string? DriverUid { get; set; }
}

/// <summary>
/// Extended location response from the admin API
/// </summary>
public class LocationResponse
{
    public string RideId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double AgeSeconds { get; set; }
    public string? DriverUid { get; set; }
    public string? DriverName { get; set; }
}

/// <summary>
/// Active ride with location data for the admin dashboard
/// </summary>
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
    
    /// <summary>
    /// Legacy booking status field (Scheduled, InProgress, Completed)
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Real-time driver status (OnRoute, Arrived, PassengerOnboard, etc.)
    /// Prefer this over Status for displaying current ride state.
    /// </summary>
    public string? CurrentStatus { get; set; }
    
    /// <summary>
    /// Age of location data in seconds
    /// </summary>
    public double AgeSeconds { get; set; }
}

/// <summary>
/// Wrapper for GET /admin/locations endpoint response
/// </summary>
public class LocationsResponse
{
    public int Count { get; set; }
    public List<ActiveRideLocationDto> Locations { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// SignalR event when a driver updates ride status
/// </summary>
public class RideStatusChangedEvent
{
    public string RideId { get; set; } = string.Empty;
    public string DriverUid { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? PassengerName { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
