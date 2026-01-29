using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IQuoteService
{
    Task<List<QuoteDetailDto>> GetQuotesAsync(int take = 100);
    Task<QuoteDetailDto?> GetQuoteAsync(string id);
    Task UpdateQuoteAsync(string id, UpdateQuoteDto updateDto);
    
    // Phase B: Alpha test quote lifecycle methods
    Task AcknowledgeQuoteAsync(string id, AcknowledgeQuoteDto dto);
    Task RespondToQuoteAsync(string id, RespondToQuoteDto dto);
    Task AcceptQuoteAsync(string id);
    Task CancelQuoteAsync(string id);
}

public class QuoteService : IQuoteService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        ILogger<QuoteService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
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
        
        // Phase 2: Handle 403 Forbidden
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[QuoteService] Access denied to quote list");
            throw new UnauthorizedAccessException("Access denied. You do not have permission to view quotes.");
        }
        
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
        
        // Phase 1: Handle 403 Forbidden responses
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Access denied. You don't have permission to view this quote.");
        }
        
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
        
        // Phase 1: Handle 403 Forbidden responses
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Access denied. You don't have permission to update this quote.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update quote: {response.StatusCode}. {errorContent}");
        }
    }
    
    // Phase B: Alpha test quote lifecycle methods
    
    public async Task AcknowledgeQuoteAsync(string id, AcknowledgeQuoteDto dto)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"/quotes/{id}/acknowledge", dto);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[QuoteService] Access denied to acknowledge quote {QuoteId}", id);
            throw new UnauthorizedAccessException("Access denied. You do not have permission to acknowledge quotes. Staff role required.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("[QuoteService] Failed to acknowledge quote {QuoteId}: {Error}", id, errorContent);
            throw new Exception($"Failed to acknowledge quote: {response.StatusCode}. {errorContent}");
        }
        
        _logger.LogInformation("[QuoteService] Successfully acknowledged quote {QuoteId}", id);
    }
    
    public async Task RespondToQuoteAsync(string id, RespondToQuoteDto dto)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"/quotes/{id}/respond", dto);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[QuoteService] Access denied to respond to quote {QuoteId}", id);
            throw new UnauthorizedAccessException("Access denied. You do not have permission to respond to quotes. Staff role required.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("[QuoteService] Failed to respond to quote {QuoteId}: {Error}", id, errorContent);
            throw new Exception($"Failed to respond to quote: {response.StatusCode}. {errorContent}");
        }
        
        _logger.LogInformation("[QuoteService] Successfully responded to quote {QuoteId} with price ${Price}", id, dto.EstimatedPrice);
    }
    
    public async Task AcceptQuoteAsync(string id)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsync($"/quotes/{id}/accept", null);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[QuoteService] Access denied to accept quote {QuoteId}", id);
            throw new UnauthorizedAccessException("Access denied. You do not have permission to accept this quote.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("[QuoteService] Failed to accept quote {QuoteId}: {Error}", id, errorContent);
            throw new Exception($"Failed to accept quote: {response.StatusCode}. {errorContent}");
        }
        
        _logger.LogInformation("[QuoteService] Successfully accepted quote {QuoteId}", id);
    }
    
    public async Task CancelQuoteAsync(string id)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsync($"/quotes/{id}/cancel", null);
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[QuoteService] Access denied to cancel quote {QuoteId}", id);
            throw new UnauthorizedAccessException("Access denied. You do not have permission to cancel this quote.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("[QuoteService] Failed to cancel quote {QuoteId}: {Error}", id, errorContent);
            throw new Exception($"Failed to cancel quote: {response.StatusCode}. {errorContent}");
        }
        
        _logger.LogInformation("[QuoteService] Successfully cancelled quote {QuoteId}", id);
    }
}
