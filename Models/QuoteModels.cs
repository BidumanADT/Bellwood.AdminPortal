namespace Bellwood.AdminPortal.Models;

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
