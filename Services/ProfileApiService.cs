using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bellwood.AdminPortal.Services;

// ?????????????????????????????????????????????????????????????????????????????
// Interface
// ?????????????????????????????????????????????????????????????????????????????

public interface IProfileApiService
{
    // ?? Profile ??????????????????????????????????????????????????????????????

    /// <summary>GET /profile — returns the caller's profile, or null on 404.</summary>
    Task<BookerProfileDto?> GetProfileAsync(string? staffOverrideUserId = null);

    /// <summary>PUT /profile — upserts the caller's (or booker's) profile.</summary>
    Task<UserActionResult> UpdateProfileAsync(UpdateBookerProfileRequest request, string? staffOverrideUserId = null);

    // ?? Saved passengers ?????????????????????????????????????????????????????

    Task<List<SavedPassengerDto>> GetSavedPassengersAsync(string? staffOverrideUserId = null);
    Task<UserActionResult> AddPassengerAsync(SavedPassengerRequest request, string? staffOverrideUserId = null);
    Task<UserActionResult> UpdatePassengerAsync(string passengerId, SavedPassengerRequest request, string? staffOverrideUserId = null);
    Task<UserActionResult> DeletePassengerAsync(string passengerId, string? staffOverrideUserId = null);

    // ?? Saved locations ??????????????????????????????????????????????????????

    Task<List<SavedLocationDto>> GetSavedLocationsAsync(string? staffOverrideUserId = null);
    Task<UserActionResult> AddLocationAsync(SavedLocationRequest request, string? staffOverrideUserId = null);
    Task<UserActionResult> UpdateLocationAsync(string locationId, SavedLocationRequest request, string? staffOverrideUserId = null);
    Task<UserActionResult> DeleteLocationAsync(string locationId, string? staffOverrideUserId = null);
}

// ?????????????????????????????????????????????????????????????????????????????
// Implementation
// ?????????????????????????????????????????????????????????????????????????????

public class ProfileApiService : IProfileApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly ILogger<ProfileApiService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public ProfileApiService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        ILogger<ProfileApiService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var client = _httpFactory.CreateClient("AdminAPI");

        var apiKey = _apiKeyProvider.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-ApiKey", apiKey);

        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Appends ?userId=… when a staff member is acting on behalf of a booker.
    /// </summary>
    private static string WithUserId(string url, string? staffOverrideUserId) =>
        string.IsNullOrWhiteSpace(staffOverrideUserId)
            ? url
            : $"{url}{(url.Contains('?') ? '&' : '?')}userId={Uri.EscapeDataString(staffOverrideUserId)}";

    private static async Task<string> ReadErrorAsync(HttpResponseMessage r)
    {
        var body = await r.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Request failed with status {(int)r.StatusCode} {r.StatusCode}."
            : body;
    }

    private void ThrowIfForbidden(HttpResponseMessage r, string context)
    {
        if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[Profile] 403 Forbidden on {Context}", context);
            throw new UnauthorizedAccessException($"Access denied. You do not have permission to {context}.");
        }
        if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("[Profile] 401 Unauthorized on {Context}", context);
            throw new UnauthorizedAccessException("Session expired. Please log in again.");
        }
    }

    // ?? Profile ???????????????????????????????????????????????????????????????

    public async Task<BookerProfileDto?> GetProfileAsync(string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile", staffOverrideUserId);
            _logger.LogDebug("[Profile] GET {Url}", url);

            var response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;  // No profile yet — caller should prompt to complete it

            ThrowIfForbidden(response, "view profile");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BookerProfileDto>(_jsonOpts);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] GetProfileAsync failed");
            throw;
        }
    }

    public async Task<UserActionResult> UpdateProfileAsync(
        UpdateBookerProfileRequest request, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile", staffOverrideUserId);
            _logger.LogInformation("[Profile] PUT {Url}", url);

            var response = await client.PutAsJsonAsync(url, request);

            ThrowIfForbidden(response, "update profile");

            if (!response.IsSuccessStatusCode)
            {
                var msg = await ReadErrorAsync(response);
                _logger.LogError("[Profile] UpdateProfile failed: {Msg}", msg);
                return new UserActionResult { Success = false, Message = msg };
            }

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] UpdateProfileAsync failed");
            throw;
        }
    }

    // ?? Saved passengers ??????????????????????????????????????????????????????

    public async Task<List<SavedPassengerDto>> GetSavedPassengersAsync(string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile/passengers", staffOverrideUserId);
            _logger.LogDebug("[Profile] GET {Url}", url);

            var response = await client.GetAsync(url);
            ThrowIfForbidden(response, "view saved passengers");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<SavedPassengerDto>>(_jsonOpts)
                   ?? new List<SavedPassengerDto>();
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] GetSavedPassengersAsync failed");
            throw;
        }
    }

    public async Task<UserActionResult> AddPassengerAsync(
        SavedPassengerRequest request, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile/passengers", staffOverrideUserId);

            // Trim blanks to null per API contract
            var sanitised = Sanitise(request);

            _logger.LogInformation("[Profile] POST {Url}", url);
            var response = await client.PostAsJsonAsync(url, sanitised);

            ThrowIfForbidden(response, "add saved passenger");

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] AddPassengerAsync failed");
            throw;
        }
    }

    public async Task<UserActionResult> UpdatePassengerAsync(
        string passengerId, SavedPassengerRequest request, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId($"/profile/passengers/{passengerId}", staffOverrideUserId);
            var sanitised = Sanitise(request);

            _logger.LogInformation("[Profile] PUT {Url}", url);
            var response = await client.PutAsJsonAsync(url, sanitised);

            ThrowIfForbidden(response, "update saved passenger");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new UserActionResult { Success = false, Message = "Passenger not found." };

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] UpdatePassengerAsync failed for {Id}", passengerId);
            throw;
        }
    }

    public async Task<UserActionResult> DeletePassengerAsync(
        string passengerId, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId($"/profile/passengers/{passengerId}", staffOverrideUserId);

            _logger.LogInformation("[Profile] DELETE {Url}", url);
            var response = await client.DeleteAsync(url);

            ThrowIfForbidden(response, "delete saved passenger");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new UserActionResult { Success = false, Message = "Passenger not found." };

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] DeletePassengerAsync failed for {Id}", passengerId);
            throw;
        }
    }

    // ?? Saved locations ???????????????????????????????????????????????????????

    public async Task<List<SavedLocationDto>> GetSavedLocationsAsync(string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile/locations", staffOverrideUserId);
            _logger.LogDebug("[Profile] GET {Url}", url);

            var response = await client.GetAsync(url);
            ThrowIfForbidden(response, "view saved locations");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<SavedLocationDto>>(_jsonOpts)
                   ?? new List<SavedLocationDto>();
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] GetSavedLocationsAsync failed");
            throw;
        }
    }

    public async Task<UserActionResult> AddLocationAsync(
        SavedLocationRequest request, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId("/profile/locations", staffOverrideUserId);

            _logger.LogInformation("[Profile] POST {Url}", url);
            var response = await client.PostAsJsonAsync(url, request);

            ThrowIfForbidden(response, "add saved location");

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] AddLocationAsync failed");
            throw;
        }
    }

    public async Task<UserActionResult> UpdateLocationAsync(
        string locationId, SavedLocationRequest request, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            // NOTE: useCount is server-preserved; do not send it in PUT.
            var url = WithUserId($"/profile/locations/{locationId}", staffOverrideUserId);

            _logger.LogInformation("[Profile] PUT {Url}", url);
            var response = await client.PutAsJsonAsync(url, request);

            ThrowIfForbidden(response, "update saved location");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new UserActionResult { Success = false, Message = "Location not found." };

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] UpdateLocationAsync failed for {Id}", locationId);
            throw;
        }
    }

    public async Task<UserActionResult> DeleteLocationAsync(
        string locationId, string? staffOverrideUserId = null)
    {
        try
        {
            var client = await GetAuthorizedClientAsync();
            var url = WithUserId($"/profile/locations/{locationId}", staffOverrideUserId);

            _logger.LogInformation("[Profile] DELETE {Url}", url);
            var response = await client.DeleteAsync(url);

            ThrowIfForbidden(response, "delete saved location");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new UserActionResult { Success = false, Message = "Location not found." };

            if (!response.IsSuccessStatusCode)
                return new UserActionResult { Success = false, Message = await ReadErrorAsync(response) };

            return new UserActionResult { Success = true };
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Profile] DeleteLocationAsync failed for {Id}", locationId);
            throw;
        }
    }

    // ?? Private helpers ???????????????????????????????????????????????????????

    /// <summary>
    /// Returns a copy of the request with blank optional strings converted to null,
    /// as required by the API contract.
    /// </summary>
    private static SavedPassengerRequest Sanitise(SavedPassengerRequest r) => new()
    {
        FirstName = r.FirstName.Trim(),
        LastName = r.LastName.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(r.PhoneNumber) ? null : r.PhoneNumber.Trim(),
        EmailAddress = string.IsNullOrWhiteSpace(r.EmailAddress) ? null : r.EmailAddress.Trim()
    };
}
