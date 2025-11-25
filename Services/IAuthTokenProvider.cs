namespace Bellwood.AdminPortal.Services;

public interface IAuthTokenProvider
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task ClearTokenAsync();
}