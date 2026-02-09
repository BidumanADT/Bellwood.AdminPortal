using System.Text.Json.Serialization;

namespace Bellwood.AdminPortal.Models;

/// <summary>
/// User information DTO matching AdminAPI format (synchronized with AuthServer)
/// All 9 fields must be present in response
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID (GUID) - unique identifier
    /// </summary>
    [JsonPropertyName("userId")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Username - login identifier (unique)
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name (null for Phase Alpha)
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    /// <summary>
    /// Last name (null for Phase Alpha)
    /// </summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    /// <summary>
    /// User roles (lowercase: "admin", "dispatcher", "driver", "booker")
    /// </summary>
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Computed property - gets first role for display
    /// Falls back to "None" if roles array is empty
    /// </summary>
    [JsonIgnore]
    public string Role => Roles.FirstOrDefault() ?? "None";
    
    /// <summary>
    /// Account disabled status (true = locked out)
    /// NOT nullable - always present
    /// </summary>
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
    
    /// <summary>
    /// Account creation timestamp (UTC)
    /// Null for Phase Alpha, will be populated later
    /// </summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTime? CreatedAtUtc { get; set; }
    
    /// <summary>
    /// Last modification timestamp (UTC)
    /// Null until first modification
    /// </summary>
    [JsonPropertyName("modifiedAtUtc")]
    public DateTime? ModifiedAtUtc { get; set; }
}

public class CreateUserRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Temporary password - must match AdminAPI field name "tempPassword"
    /// </summary>
    [JsonPropertyName("tempPassword")]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserDisabledRequest
{
    public bool IsDisabled { get; set; }
}

public class UserActionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool EndpointNotFound { get; set; }
}
