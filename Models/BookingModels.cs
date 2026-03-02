namespace Bellwood.AdminPortal.Models;

/// <summary>
/// Request payload for confirming a booking (step 2 of the two-step workflow).
/// Booking must already be in Received status before calling POST /bookings/{id}/confirm.
/// </summary>
public class ConfirmBookingRequest
{
    /// <summary>
    /// Message to send to the booker confirming receipt of their request.
    /// Defaults to the standard acknowledgment template which staff may edit before sending.
    /// </summary>
    public string MessageToPassenger { get; set; } = string.Empty;

    /// <summary>
    /// Internal staff notes — not visible to the booker.
    /// Use this for operational details such as vehicle upgrades, special accommodations,
    /// or anything that needs to be tracked internally before the booking is confirmed.
    /// e.g., "Passenger count requires SUV — sedan requested, confirming upgrade availability."
    /// </summary>
    public string? Notes { get; set; }
}
