namespace Bellwood.AdminPortal.Models;

/// <summary>
/// DTO for affiliate information with nested drivers
/// </summary>
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

/// <summary>
/// DTO for driver information
/// </summary>
public class DriverDto
{
    public string Id { get; set; } = string.Empty;
    public string AffiliateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// The driver's AuthServer User UID (e.g., "driver-001"). 
    /// This links the driver record to their authentication identity,
    /// enabling the driver to see assigned rides in the driver app.
    /// Optional but recommended for active drivers.
    /// </summary>
    public string? UserUid { get; set; }
}
