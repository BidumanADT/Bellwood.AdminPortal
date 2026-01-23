using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Service to handle automatic JWT token refresh
/// </summary>
public interface ITokenRefreshService
{
    Task StartAutoRefreshAsync();
    void StopAutoRefresh();
    Task<bool> RefreshTokenAsync();
}

public class TokenRefreshService : ITokenRefreshService, IDisposable
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly ILogger<TokenRefreshService> _logger;
    
    private Timer? _refreshTimer;
    private bool _isRefreshing;
    
    public TokenRefreshService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        JwtAuthenticationStateProvider authStateProvider,
        ILogger<TokenRefreshService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// Start automatic token refresh timer
    /// Refreshes token 5 minutes before expiry (55 minutes into 1-hour lifetime)
    /// </summary>
    public async Task StartAutoRefreshAsync()
    {
        _logger.LogInformation("[TokenRefresh] Starting auto-refresh timer");
        
        // Get current token to determine when to refresh
        var token = await _tokenProvider.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[TokenRefresh] No token found, cannot start auto-refresh");
            return;
        }
        
        // Decode token to get expiration time
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            _logger.LogWarning("[TokenRefresh] Invalid token format");
            return;
        }
        
        var jsonToken = handler.ReadJwtToken(token);
        var expiresAt = jsonToken.ValidTo;
        
        // Calculate time until we should refresh (5 minutes before expiry)
        var refreshAt = expiresAt.AddMinutes(-5);
        var timeUntilRefresh = refreshAt - DateTime.UtcNow;
        
        if (timeUntilRefresh.TotalMilliseconds <= 0)
        {
            // Token expires soon, refresh immediately
            _logger.LogWarning("[TokenRefresh] Token expires soon, refreshing immediately");
            await RefreshTokenAsync();
            timeUntilRefresh = TimeSpan.FromMinutes(55); // Reset timer for next refresh
        }
        
        _logger.LogInformation($"[TokenRefresh] Token will be refreshed in {timeUntilRefresh.TotalMinutes:F1} minutes");
        
        // Set up timer to refresh token
        _refreshTimer = new Timer(
            async _ => await RefreshTokenAsync(),
            null,
            timeUntilRefresh,
            TimeSpan.FromMinutes(55) // Refresh every 55 minutes thereafter
        );
    }
    
    /// <summary>
    /// Stop automatic token refresh
    /// </summary>
    public void StopAutoRefresh()
    {
        _logger.LogInformation("[TokenRefresh] Stopping auto-refresh timer");
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
    
    /// <summary>
    /// Manually refresh the access token using the refresh token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        if (_isRefreshing)
        {
            _logger.LogDebug("[TokenRefresh] Refresh already in progress, skipping");
            return false;
        }
        
        _isRefreshing = true;
        
        try
        {
            _logger.LogInformation("[TokenRefresh] Attempting to refresh access token");
            
            var refreshToken = await _tokenProvider.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("[TokenRefresh] No refresh token available");
                return false;
            }
            
            var client = _httpFactory.CreateClient("AuthServer");
            
            // Call token refresh endpoint
            var response = await client.PostAsJsonAsync("/connect/token", new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken
            });
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[TokenRefresh] Token refresh failed: {response.StatusCode}");
                return false;
            }
            
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            
            if (result?.AccessToken == null)
            {
                _logger.LogError("[TokenRefresh] No access token in refresh response");
                return false;
            }
            
            _logger.LogInformation("[TokenRefresh] Token refreshed successfully");
            
            // Update stored tokens
            await _tokenProvider.SetTokenAsync(result.AccessToken);
            
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                await _tokenProvider.SetRefreshTokenAsync(result.RefreshToken);
            }
            
            // Extract username from new token for auth state update
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(result.AccessToken);
            var username = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "Unknown";
            
            // Update authentication state with new token
            await _authStateProvider.MarkUserAsAuthenticatedAsync(username, result.AccessToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TokenRefresh] Error during token refresh");
            return false;
        }
        finally
        {
            _isRefreshing = false;
        }
    }
    
    public void Dispose()
    {
        StopAutoRefresh();
    }
    
    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}
