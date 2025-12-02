using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IAffiliateService
{
    Task<List<AffiliateDto>> GetAffiliatesAsync();
    Task<AffiliateDto?> GetAffiliateAsync(string id);
    Task<string> CreateAffiliateAsync(AffiliateDto affiliate);
    Task UpdateAffiliateAsync(string id, AffiliateDto affiliate);
    Task DeleteAffiliateAsync(string id);
    Task<string> AddDriverToAffiliateAsync(string affiliateId, DriverDto driver);
    Task AssignDriverToBookingAsync(string bookingId, string driverId);
}

public class AffiliateService : IAffiliateService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;

    public AffiliateService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
    }

    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var client = _httpFactory.CreateClient("AdminAPI");

        // Attach API key
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

    public async Task<List<AffiliateDto>> GetAffiliatesAsync()
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync("/affiliates/list");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AffiliateDto>>() ?? new();
    }

    public async Task<AffiliateDto?> GetAffiliateAsync(string id)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync($"/affiliates/{id}");
        
        if (!response.IsSuccessStatusCode)
            return null;
            
        return await response.Content.ReadFromJsonAsync<AffiliateDto>();
    }

    public async Task<string> CreateAffiliateAsync(AffiliateDto affiliate)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync("/affiliates", affiliate);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result?["id"] ?? throw new Exception("No ID returned from create");
    }

    public async Task UpdateAffiliateAsync(string id, AffiliateDto affiliate)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PutAsJsonAsync($"/affiliates/{id}", affiliate);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAffiliateAsync(string id)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.DeleteAsync($"/affiliates/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> AddDriverToAffiliateAsync(string affiliateId, DriverDto driver)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"/affiliates/{affiliateId}/drivers", driver);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result?["id"] ?? throw new Exception("No ID returned from driver creation");
    }

    public async Task AssignDriverToBookingAsync(string bookingId, string driverId)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"/bookings/{bookingId}/assign-driver", new
        {
            driverId = driverId
        });
        response.EnsureSuccessStatusCode();
    }
}
