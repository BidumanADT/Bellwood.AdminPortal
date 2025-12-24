using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IQuoteService
{
    Task<List<QuoteDetailDto>> GetQuotesAsync(int take = 100);
    Task<QuoteDetailDto?> GetQuoteAsync(string id);
    Task UpdateQuoteAsync(string id, UpdateQuoteDto updateDto);
}

public class QuoteService : IQuoteService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;

    public QuoteService(
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

    public async Task<List<QuoteDetailDto>> GetQuotesAsync(int take = 100)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync($"/quotes/list?take={take}");
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get quotes: {response.StatusCode}. {errorContent}");
        }
        
        return await response.Content.ReadFromJsonAsync<List<QuoteDetailDto>>() ?? new();
    }

    public async Task<QuoteDetailDto?> GetQuoteAsync(string id)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync($"/quotes/{id}");
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
                
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get quote: {response.StatusCode}. {errorContent}");
        }
        
        return await response.Content.ReadFromJsonAsync<QuoteDetailDto>();
    }

    public async Task UpdateQuoteAsync(string id, UpdateQuoteDto updateDto)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PutAsJsonAsync($"/quotes/{id}", updateDto);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update quote: {response.StatusCode}. {errorContent}");
        }
    }
}
