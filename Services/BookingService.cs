using Bellwood.AdminPortal.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bellwood.AdminPortal.Services;

public interface IBookingService
{
    /// <summary>
    /// Step 1: Acknowledge receipt of a booking request.
    /// Transitions booking status from Requested → Received.
    /// No request body. Calls POST /bookings/{id}/receive
    /// </summary>
    Task ReceiveBookingAsync(string bookingId);

    /// <summary>
    /// Step 2: Confirm the booking and notify the booker.
    /// Booking must be in Received status before calling this.
    /// Transitions booking status from Received → Confirmed.
    /// Calls POST /bookings/{id}/confirm
    /// </summary>
    Task ConfirmBookingAsync(string bookingId, ConfirmBookingRequest request);
}

public class BookingService : IBookingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        ILogger<BookingService> logger)
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

        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public async Task ReceiveBookingAsync(string bookingId)
    {
        var client = await GetAuthorizedClientAsync();
        // No request body — API expects POST with no Content-Type
        var response = await client.PostAsync($"/bookings/{bookingId}/receive", null);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[BookingService] Access denied acknowledging receipt of booking {BookingId}", bookingId);
            throw new UnauthorizedAccessException(
                "Access denied. You do not have permission to acknowledge bookings. Staff role required.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "[BookingService] Failed to acknowledge receipt of booking {BookingId}: {StatusCode} {Error}",
                bookingId, response.StatusCode, errorContent);
            throw new Exception($"Failed to acknowledge receipt: {response.StatusCode}. {errorContent}");
        }

        _logger.LogInformation("[BookingService] Acknowledged receipt of booking {BookingId}", bookingId);
    }

    public async Task ConfirmBookingAsync(string bookingId, ConfirmBookingRequest request)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"/bookings/{bookingId}/confirm", request);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("[BookingService] Access denied confirming booking {BookingId}", bookingId);
            throw new UnauthorizedAccessException(
                "Access denied. You do not have permission to confirm bookings. Staff role required.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "[BookingService] Failed to confirm booking {BookingId}: {StatusCode} {Error}",
                bookingId, response.StatusCode, errorContent);
            throw new Exception($"Failed to confirm booking: {response.StatusCode}. {errorContent}");
        }

        _logger.LogInformation("[BookingService] Successfully confirmed booking {BookingId}", bookingId);
    }
}
