using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IUserManagementService
{
    Task<List<UserDto>> GetUsersAsync(int take = 50, int skip = 0);
    Task<UserActionResult> CreateUserAsync(CreateUserRequest request);
    Task<UserActionResult> UpdateUserRolesAsync(string id, List<string> roles);
    Task<UserActionResult> SetUserDisabledAsync(string id, bool isDisabled);
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
        var client = _httpFactory.CreateClient("AdminAPI");

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

            var url = $"/users/list?take={take}&skip={skip}";

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

    public async Task<UserActionResult> UpdateUserRolesAsync(string id, List<string> roles)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();

            _logger.LogInformation("[UserManagement] Updating roles for user {UserId}", id);

            var request = new UpdateUserRolesRequest { Roles = roles };

            var response = await client.PutAsJsonAsync($"/users/{id}/roles", request);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[UserManagement] Access denied to update roles - requires admin role");
                throw new UnauthorizedAccessException("Access denied. Admin role required to update roles.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response);
                _logger.LogError("[UserManagement] Role update failed: {Status} - {Message}", response.StatusCode, message);
                return new UserActionResult { Success = false, Message = message };
            }

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserManagement] Failed to update roles for {UserId}", id);
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
            var response = await client.PutAsJsonAsync($"/users/{id}/disable", request);

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
