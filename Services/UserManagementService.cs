using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IUserManagementService
{
    Task<List<UserDto>> GetUsersAsync(int take = 50, int skip = 0);
    Task<UserActionResult> CreateUserAsync(CreateUserRequest request);
    Task<UserActionResult> UpdateUserRoleAsync(string username, string role); // Changed: single role, username parameter
    Task<UserActionResult> SetUserDisabledAsync(string id, bool isDisabled);
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
        // FIXED: User management endpoints are on AuthServer, not AdminAPI
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

    public async Task<List<UserDto>> GetUsersAsync(int take = 50, int skip = 0)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            // AuthServer endpoint: /api/admin/users
            var url = $"/api/admin/users?take={take}&skip={skip}";

            _logger.LogDebug("[UserManagement] Fetching users from {Url}", url);

            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to user list - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to view users.");
            }

            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();

            _logger.LogInformation("[UserManagement] Loaded {Count} users", users.Count);

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

    public async Task<UserActionResult> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Creating user {Email}", request.Email);

            var response = await client.PostAsJsonAsync("/api/admin/users", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to create user - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to create users.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Create user failed: {Status} - {Message}", response.StatusCode, message);
                return new UserActionResult { Success = false, Message = message };
            }

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to create user");
            throw;
        }
    }

    public async Task<UserActionResult> UpdateUserRoleAsync(string username, string role)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Updating role for user {Username} to {Role}", username, role);

            // AuthServer expects: PUT /api/admin/users/{username}/role
            // Request body: { "role": "admin" }
            var request = new { role = role };

            var response = await client.PutAsJsonAsync($"/api/admin/users/{username}/role", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to update role - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to update roles.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Role update failed for {Username}: {Status} - {Message}", 
                    username, response.StatusCode, message);
                return new UserActionResult { Success = false, Message = message };
            }

            _logger.LogInformation("[UserManagement] Successfully updated role for {Username} to {Role}", username, role);
            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to update role for {Username}", username);
            throw;
        }
    }

    public async Task<UserActionResult> SetUserDisabledAsync(string id, bool isDisabled)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Setting disabled={IsDisabled} for user {UserId}", isDisabled, id);

            var request = new UpdateUserDisabledRequest { IsDisabled = isDisabled };
            var response = await client.PutAsJsonAsync($"/api/admin/users/{id}/disable", request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[UserManagement] Disable endpoint not available");
                return new UserActionResult
                {
                    Success = false,
                    EndpointNotFound = true,
                    Message = "Disable endpoint is not available."
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to disable user - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to disable users.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Disable toggle failed: {Status} - {Message}", response.StatusCode, message);
                return new UserActionResult { Success = false, Message = message };
            }

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to update disable status for {UserId}", id);
            throw;
        }
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content)
            ? $"Request failed with status {response.StatusCode}."
            : content;
    }
}
