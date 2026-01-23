using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Service for managing users via AuthServer
/// Phase 2: User management with role assignment
/// </summary>
public interface IUserManagementService
{
    Task<List<UserDto>> GetAllUsersAsync(string? roleFilter = null);
    Task<UpdateUserRoleResponse> UpdateUserRoleAsync(string username, string newRole);
}

public class UserManagementService : IUserManagementService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        ILogger<UserManagementService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var client = _httpFactory.CreateClient("AuthServer");

        // Attach JWT token
        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    /// <summary>
    /// Get all users from AuthServer
    /// </summary>
    public async Task<List<UserDto>> GetAllUsersAsync(string? roleFilter = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            
            var url = "/api/admin/users";
            if (!string.IsNullOrEmpty(roleFilter))
            {
                url += $"?role={Uri.EscapeDataString(roleFilter)}";
            }

            _logger.LogDebug($"[UserManagement] Fetching users from {url}");
            
            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to user list - requires admin role");
                throw new UnauthorizedAccessException("Access denied. You do not have permission to view users. Admin role required.");
            }

            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();
            
            _logger.LogInformation($"[UserManagement] Loaded {users.Count} users");
            
            return users;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to get users");
            throw;
        }
    }

    /// <summary>
    /// Update a user's role via AuthServer
    /// Phase 2: Direct AuthServer call (no audit logging)
    /// </summary>
    public async Task<UpdateUserRoleResponse> UpdateUserRoleAsync(string username, string newRole)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation($"[UserManagement] Updating role for {username} to {newRole}");

            var request = new UpdateUserRoleRequest { Role = newRole };
            
            var response = await client.PutAsJsonAsync($"/api/admin/users/{username}/role", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to role update - requires admin role");
                throw new UnauthorizedAccessException("Access denied. You do not have permission to update user roles. Admin role required.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[UserManagement] Role update failed: {response.StatusCode} - {errorContent}");
                return new UpdateUserRoleResponse
                {
                    Success = false,
                    Message = $"Failed to update role: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<UpdateUserRoleResponse>();
            
            if (result == null)
            {
                return new UpdateUserRoleResponse
                {
                    Success = true,
                    NewRole = newRole,
                    Message = "Role updated successfully"
                };
            }

            _logger.LogInformation($"[UserManagement] Role updated successfully for {username}");
            
            return result;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[UserManagement] Failed to update role for {username}");
            throw;
        }
    }
}
