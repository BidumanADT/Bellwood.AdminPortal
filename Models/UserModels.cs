namespace Bellwood.AdminPortal.Models;

/// <summary>
/// User information from AuthServer GET /api/admin/users
/// </summary>
public class UserDto
{
    public string Username { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

/// <summary>
/// Request to update user's role
/// </summary>
public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Response from role update
/// </summary>
public class UpdateUserRoleResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? NewRole { get; set; }
}
