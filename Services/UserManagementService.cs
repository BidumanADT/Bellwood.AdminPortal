using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IUserManagementService
{
    Task<List<UserDto>> GetUsersAsync(int take = 50, int skip = 0);
    Task<UserActionResult> CreateUserAsync(CreateUserRequest request);
    Task<UserActionResult> UpdateUserRoleAsync(string userId, string role);
    Task<UserActionResult> SetUserDisabledAsync(string userId, bool isDisabled);
}

public class UserManagementService : IUserManagementService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        ILogger<UserManagementService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
    }

    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        // FIXED: User management endpoints are on AdminAPI, not AuthServer
        var client = _httpFactory.CreateClient("AdminAPI");

        // Add API key
        var apiKey = _apiKeyProvider.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-ApiKey", apiKey);
        }

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

            // AdminAPI endpoint: GET /users/list
            var url = $"/users/list?take={take}&skip={skip}";

            _logger.LogDebug("[UserManagement] Fetching users from {Url}", url);

            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to user list - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to view users.");
            }

            response.EnsureSuccessStatusCode();

            // AdminAPI returns direct array (no wrapper) matching AuthServer format
            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (users == null)
            {
                _logger.LogWarning("[UserManagement] No users returned from API");
                return new List<UserDto>();
            }

            _logger.LogInformation("[UserManagement] Loaded {Count} users", users.Count);

            return users;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "[UserManagement] JSON deserialization failed - Check if AdminAPI response format matches UserDto");
            throw;
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

            // AdminAPI endpoint: POST /users
            var response = await client.PostAsJsonAsync("/users", request);

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

    public async Task<UserActionResult> UpdateUserRoleAsync(string userId, string role)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Updating role for user {UserId} to {Role}", userId, role);

            // AdminAPI endpoint: PUT /users/{userId}/roles
            // Request body: { "role": "admin" }
            var request = new { role = role };

            var response = await client.PutAsJsonAsync($"/users/{userId}/roles", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to update role - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to update roles.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Role update failed for {UserId}: {Status} - {Message}", 
                    userId, response.StatusCode, message);
                return new UserActionResult { Success = false, Message = message };
            }

            _logger.LogInformation("[UserManagement] Successfully updated role for {UserId} to {Role}", userId, role);
            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to update role for {UserId}", userId);
            throw;
        }
    }

    public async Task<UserActionResult> SetUserDisabledAsync(string userId, bool isDisabled)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Setting disabled={IsDisabled} for user {UserId}", isDisabled, userId);

            // AdminAPI endpoint: PUT /users/{userId}/disable or /users/{userId}/enable
            var endpoint = isDisabled ? $"/users/{userId}/disable" : $"/users/{userId}/enable";
            var response = await client.PutAsync(endpoint, null);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("[UserManagement] Disable/enable endpoint not available");
                return new UserActionResult
                {
                    Success = false,
                    EndpointNotFound = true,
                    Message = "User disable/enable feature is not yet available."
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to disable/enable user - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to manage user status.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Disable/enable toggle failed: {Status} - {Message}", response.StatusCode, message);
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
            _logger.LogError(ex, "[UserManagement] Failed to update disable status for {UserId}", userId);
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
