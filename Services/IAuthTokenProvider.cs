namespace Bellwood.AdminPortal.Services;

public interface IAuthTokenProvider
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task ClearTokenAsync();
    
    // Phase 2.2: Refresh token support
    Task<string?> GetRefreshTokenAsync();
    Task SetRefreshTokenAsync(string refreshToken);
}