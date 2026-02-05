using System.Text.Json.Serialization;

namespace Bellwood.AdminPortal.Models;

/// <summary>
/// User information from AuthServer GET /api/admin/users
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID (maps to 'userId' from AuthServer response)
    /// </summary>
    [JsonPropertyName("userId")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty; // Primary identifier
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// IMPORTANT: AuthServer returns a single 'role' (string), NOT 'roles' (array)
    /// We map it to a List for consistency with the UI
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Computed property - converts single role to list for UI compatibility
    /// </summary>
    [JsonIgnore]
    public List<string> Roles => string.IsNullOrEmpty(Role) ? new List<string>() : new List<string> { Role };
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("modifiedAt")]
    public DateTime? ModifiedAt { get; set; }
    
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
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
