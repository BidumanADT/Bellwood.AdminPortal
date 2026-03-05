namespace Bellwood.AdminPortal.Models;

/// <summary>
/// Nested draft object returned inside GET /quotes/{id}.
/// The API stores passenger count and luggage breakdown here, not at the top level.
/// </summary>
public class QuoteDraftDto
{
    [System.Text.Json.Serialization.JsonPropertyName("passengerCount")]
    public int PassengerCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("checkedBags")]
    public int CheckedBags { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("carryOnBags")]
    public int CarryOnBags { get; set; }
}

/// <summary>
/// Detailed quote information for viewing and editing
/// </summary>
public class QuoteDetailDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string? Status { get; set; }
    public string BookerName { get; set; } = string.Empty;
    public string BookerEmail { get; set; } = string.Empty;
    public string? BookerPhone { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string? PassengerPhone { get; set; }
    public string VehicleClass { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string? DropoffLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public int PassengerCount { get; set; }
    public int Luggage { get; set; }
    public string? SpecialRequests { get; set; }
    public decimal? QuotedPrice { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Phase 1: Audit trail fields (added January 2026)
    /// <summary>
    /// User ID (GUID) of the user who created this quote.
    /// Null for legacy quotes created before Phase 1.
    /// </summary>
    public string? CreatedByUserId { get; set; }
    
    /// <summary>
    /// User ID (GUID) of the user who last modified this quote.
    /// Null if never modified or for legacy quotes.
    /// </summary>
    public string? ModifiedByUserId { get; set; }
    
    /// <summary>
    /// Timestamp of the last modification to this quote.
    /// Null if never modified.
    /// </summary>
    public DateTime? ModifiedOnUtc { get; set; }
    
    // Phase B: Alpha test preparation fields (added January 2026)
    /// <summary>
    /// Timestamp when dispatcher acknowledged receipt of the quote.
    /// Null if not yet acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
    
    /// <summary>
    /// User ID of the dispatcher who acknowledged the quote.
    /// Null if not yet acknowledged.
    /// </summary>
    public string? AcknowledgedByUserId { get; set; }
    
    /// <summary>
    /// Timestamp when dispatcher responded with price/ETA estimate.
    /// Null if not yet responded.
    /// </summary>
    public DateTime? RespondedAt { get; set; }
    
    /// <summary>
    /// User ID of the dispatcher who responded to the quote.
    /// Null if not yet responded.
    /// </summary>
    public string? RespondedByUserId { get; set; }
    
    /// <summary>
    /// Estimated price provided by dispatcher (placeholder until Limo Anywhere integration).
    /// Null if not yet provided.
    /// </summary>
    public decimal? EstimatedPrice { get; set; }
    
    /// <summary>
    /// Estimated pickup time provided by dispatcher (placeholder).
    /// Null if not yet provided.
    /// </summary>
    public DateTime? EstimatedPickupTime { get; set; }
    
    /// <summary>
    /// Notes added during quote lifecycle (acknowledge/respond).
    /// Distinct from AdminNotes for workflow tracking.
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Booking ID if this quote was accepted and converted to a booking.
    /// Null if quote not yet accepted or was rejected/cancelled.
    /// </summary>
    public string? BookingId { get; set; }

    /// <summary>
    /// Nested draft object from the API — contains passenger count and luggage breakdown.
    /// Always present on GET /quotes/{id}; null on list endpoint.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("draft")]
    public QuoteDraftDto? Draft { get; set; }

    // ?? Computed helpers so existing RenderTripDetails code needs no changes ??

    /// <summary>
    /// Total passenger count — read from draft when available, falls back to own field.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int EffectivePassengerCount => Draft?.PassengerCount ?? PassengerCount;

    /// <summary>
    /// Total luggage pieces (checked + carry-on) for display in Trip Details.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int EffectiveLuggage => Draft != null
        ? Draft.CheckedBags + Draft.CarryOnBags
        : Luggage;
}

/// <summary>
/// DTO for updating quote pricing and status
/// </summary>
public class UpdateQuoteDto
{
    public decimal? QuotedPrice { get; set; }
    public string? Status { get; set; }
    public string? AdminNotes { get; set; }
}

/// <summary>
/// DTO for acknowledging a quote (Phase B)
/// </summary>
public class AcknowledgeQuoteDto
{
    /// <summary>
    /// Optional notes from dispatcher acknowledging the quote
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for responding to a quote with price/ETA estimate (Phase B)
/// </summary>
public class RespondToQuoteDto
{
    /// <summary>
    /// Estimated price (placeholder until Limo Anywhere integration)
    /// </summary>
    public decimal EstimatedPrice { get; set; }
    
    /// <summary>
    /// Estimated pickup time (placeholder)
    /// </summary>
    public DateTime EstimatedPickupTime { get; set; }
    
    /// <summary>
    /// Optional notes explaining the estimate
    /// </summary>
    public string? Notes { get; set; }
}
