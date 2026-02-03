namespace Bellwood.AdminPortal.Models;

/// <summary>
/// User information from AdminApi GET /users/list
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
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
