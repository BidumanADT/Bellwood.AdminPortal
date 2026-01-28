using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

#pragma warning disable CS8601 // Possible null reference assignment - BindConverter.FormatValue handles nulls appropriately

namespace Bellwood.AdminPortal.Components.Pages;

public partial class QuoteDetail
{
    private void RenderPendingPanel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card border-info");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-header bg-info text-white");
        builder.OpenElement(4, "h5");
        builder.AddAttribute(5, "class", "mb-0");
        builder.AddContent(6, "?? New Quote Request");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "card-body");
        
        builder.OpenElement(9, "p");
        builder.AddAttribute(10, "class", "text-muted mb-3");
        builder.AddContent(11, "This quote is awaiting acknowledgment. Click below to acknowledge receipt and begin processing.");
        builder.CloseElement();
        
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "class", "mb-3");
        builder.OpenElement(14, "label");
        builder.AddAttribute(15, "class", "form-label");
        builder.OpenElement(16, "strong");
        builder.AddContent(17, "Acknowledgment Notes (Optional)");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(18, "textarea");
        builder.AddAttribute(19, "class", "form-control");
        builder.AddAttribute(20, "rows", 3);
        builder.AddAttribute(21, "placeholder", "Add any notes about this quote...");
        builder.AddAttribute(22, "value", BindConverter.FormatValue(acknowledgeNotes));
        builder.AddAttribute(23, "onchange", EventCallback.Factory.CreateBinder(this, value => acknowledgeNotes = value, acknowledgeNotes));
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(24, "button");
        builder.AddAttribute(25, "class", "btn btn-info w-100");
        builder.AddAttribute(26, "onclick", EventCallback.Factory.Create(this, AcknowledgeQuote));
        builder.AddAttribute(27, "disabled", isSaving);
        
        if (isSaving)
        {
            builder.OpenElement(28, "span");
            builder.AddAttribute(29, "class", "spinner-border spinner-border-sm me-2");
            builder.CloseElement();
        }
        
        builder.AddContent(30, "? Acknowledge Quote");
        builder.CloseElement(); // button
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    }
    
    private void RenderAcknowledgedPanel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card border-warning");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-header bg-warning text-dark");
        builder.OpenElement(4, "h5");
        builder.AddAttribute(5, "class", "mb-0");
        builder.AddContent(6, "? Quote Acknowledged - Enter Estimate");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "card-body");
        
        builder.OpenElement(9, "div");
        builder.AddAttribute(10, "class", "alert alert-warning mb-3");
        builder.OpenElement(11, "strong");
        builder.AddContent(12, "?? Placeholder Estimates");
        builder.CloseElement();
        builder.OpenElement(13, "br");
        builder.CloseElement();
        builder.AddContent(14, "These are manual estimates until Limo Anywhere integration is complete. Clearly label them as approximate when communicating with customers.");
        builder.CloseElement();
        
        // Estimated Price
        builder.OpenElement(15, "div");
        builder.AddAttribute(16, "class", "mb-3");
        builder.OpenElement(17, "label");
        builder.AddAttribute(18, "class", "form-label");
        builder.OpenElement(19, "strong");
        builder.AddContent(20, "Estimated Price ($)");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(21, "div");
        builder.AddAttribute(22, "class", "input-group");
        builder.OpenElement(23, "span");
        builder.AddAttribute(24, "class", "input-group-text");
        builder.AddContent(25, "$");
        builder.CloseElement();
        
        builder.OpenElement(26, "input");
        builder.AddAttribute(27, "type", "number");
        builder.AddAttribute(28, "class", "form-control");
        builder.AddAttribute(29, "placeholder", "Enter estimated price");
        builder.AddAttribute(30, "step", "0.01");
        builder.AddAttribute(31, "min", "0");
        var formattedPrice = estimatedPrice.HasValue 
            ? estimatedPrice.Value.ToString("F2") 
            : string.Empty;
        builder.AddAttribute(32, "value", formattedPrice);
        builder.AddAttribute(33, "onchange", EventCallback.Factory.CreateBinder<decimal?>(this, value => estimatedPrice = value, estimatedPrice));
        builder.CloseElement();
        builder.CloseElement(); // input-group
        
        builder.OpenElement(34, "small");
        builder.AddAttribute(35, "class", "text-muted");
        builder.AddContent(36, "This is a placeholder estimate pending Limo Anywhere integration");
        builder.CloseElement();
        builder.CloseElement(); // mb-3
        
        // Estimated Pickup Time
        builder.OpenElement(37, "div");
        builder.AddAttribute(38, "class", "mb-3");
        builder.OpenElement(39, "label");
        builder.AddAttribute(40, "class", "form-label");
        builder.OpenElement(41, "strong");
        builder.AddContent(42, "Estimated Pickup Time");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(43, "input");
        builder.AddAttribute(44, "type", "datetime-local");
        builder.AddAttribute(45, "class", "form-control");
        var formattedDateTime = estimatedPickupTime.HasValue 
            ? estimatedPickupTime.Value.ToString("yyyy-MM-ddTHH:mm") 
            : string.Empty;
        builder.AddAttribute(46, "value", formattedDateTime);
        builder.AddAttribute(47, "onchange", EventCallback.Factory.CreateBinder<DateTime?>(this, value => estimatedPickupTime = value, estimatedPickupTime));
        builder.CloseElement();
        
        builder.OpenElement(48, "small");
        builder.AddAttribute(49, "class", "text-muted");
        builder.AddContent(50, "Approximate pickup time for this booking");
        builder.CloseElement();
        builder.CloseElement(); // mb-3
        
        // Response Notes
        builder.OpenElement(51, "div");
        builder.AddAttribute(52, "class", "mb-3");
        builder.OpenElement(53, "label");
        builder.AddAttribute(54, "class", "form-label");
        builder.OpenElement(55, "strong");
        builder.AddContent(56, "Response Notes (Optional)");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(57, "textarea");
        builder.AddAttribute(58, "class", "form-control");
        builder.AddAttribute(59, "rows", 3);
        builder.AddAttribute(60, "placeholder", "Explain pricing, add special instructions...");
        builder.AddAttribute(61, "value", BindConverter.FormatValue(responseNotes));
        builder.AddAttribute(62, "onchange", EventCallback.Factory.CreateBinder(this, value => responseNotes = value, responseNotes));
        builder.CloseElement();
        builder.CloseElement(); // mb-3
        
        // Send Response Button
        builder.OpenElement(63, "button");
        builder.AddAttribute(64, "class", "btn btn-warning w-100");
        builder.AddAttribute(65, "onclick", EventCallback.Factory.Create(this, RespondToQuote));
        builder.AddAttribute(66, "disabled", isSaving || !estimatedPrice.HasValue || !estimatedPickupTime.HasValue);
        
        if (isSaving)
        {
            builder.OpenElement(67, "span");
            builder.AddAttribute(68, "class", "spinner-border spinner-border-sm me-2");
            builder.CloseElement();
        }
        
        builder.AddContent(69, "?? Send Response to Customer");
        builder.CloseElement(); // button
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    }
    
    private void RenderRespondedPanel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card border-success");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-header bg-success text-white");
        builder.OpenElement(4, "h5");
        builder.AddAttribute(5, "class", "mb-0");
        builder.AddContent(6, "? Response Sent - Awaiting Customer");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "card-body");
        
        builder.OpenElement(9, "p");
        builder.AddAttribute(10, "class", "text-muted mb-3");
        builder.AddContent(11, "The customer has been provided with the following estimate. Waiting for their acceptance or cancellation.");
        builder.CloseElement();
        
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "class", "p-3 mb-3");
        builder.AddAttribute(14, "style", "background: rgba(25, 135, 84, 0.1); border-radius: 8px;");
        
        builder.OpenElement(15, "div");
        builder.AddAttribute(16, "class", "mb-2");
        builder.OpenElement(17, "strong");
        builder.AddContent(18, "Estimated Price:");
        builder.CloseElement();
        builder.OpenElement(19, "span");
        builder.AddAttribute(20, "class", "text-success fs-4 ms-2");
        builder.AddContent(21, $"${quote?.EstimatedPrice?.ToString("N2") ?? "N/A"}");
        builder.CloseElement();
        builder.OpenElement(22, "span");
        builder.AddAttribute(23, "class", "badge bg-warning text-dark ms-2");
        builder.AddContent(24, "Placeholder");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(25, "div");
        builder.AddAttribute(26, "class", "mb-2");
        builder.OpenElement(27, "strong");
        builder.AddContent(28, "Estimated Pickup:");
        builder.CloseElement();
        builder.AddContent(29, $" {quote?.EstimatedPickupTime?.ToLocalTime().ToString("g") ?? "Not specified"}");
        builder.CloseElement();
        
        if (!string.IsNullOrEmpty(quote?.Notes))
        {
            builder.OpenElement(30, "div");
            builder.AddAttribute(31, "class", "mt-3");
            builder.OpenElement(32, "strong");
            builder.AddContent(33, "Notes to Customer:");
            builder.CloseElement();
            builder.OpenElement(34, "p");
            builder.AddAttribute(35, "class", "mb-0 mt-1");
            builder.AddContent(36, quote.Notes);
            builder.CloseElement();
            builder.CloseElement();
        }
        
        builder.CloseElement(); // p-3 box
        
        builder.OpenElement(37, "div");
        builder.AddAttribute(38, "class", "alert alert-info");
        builder.OpenElement(39, "strong");
        builder.AddContent(40, "?? Next Steps:");
        builder.CloseElement();
        builder.AddContent(41, " The customer will accept or cancel this quote via the passenger app.");
        builder.CloseElement();
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    }
    
    private void RenderAcceptedPanel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card border-primary");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-header bg-primary text-white");
        builder.OpenElement(4, "h5");
        builder.AddAttribute(5, "class", "mb-0");
        builder.AddContent(6, "?? Quote Accepted - Booking Created");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "card-body");
        
        builder.OpenElement(9, "p");
        builder.AddAttribute(10, "class", "text-success fw-bold mb-3");
        builder.AddContent(11, "? This quote has been accepted by the customer and converted to a booking.");
        builder.CloseElement();
        
        if (!string.IsNullOrEmpty(quote?.BookingId))
        {
            builder.OpenElement(12, "div");
            builder.AddAttribute(13, "class", "p-3 mb-3");
            builder.AddAttribute(14, "style", "background: rgba(13, 110, 253, 0.1); border-radius: 8px;");
            builder.OpenElement(15, "strong");
            builder.AddContent(16, "Booking ID:");
            builder.CloseElement();
            builder.OpenElement(17, "span");
            builder.AddAttribute(18, "class", "ms-2 font-monospace");
            builder.AddContent(19, quote.BookingId);
            builder.CloseElement();
            builder.CloseElement();
            
            builder.OpenElement(20, "button");
            builder.AddAttribute(21, "class", "btn btn-primary w-100");
            builder.AddAttribute(22, "onclick", EventCallback.Factory.Create(this, () => ViewBooking(quote.BookingId)));
            builder.AddContent(23, "?? View Booking Details");
            builder.CloseElement();
        }
        else
        {
            builder.OpenElement(24, "div");
            builder.AddAttribute(25, "class", "alert alert-warning");
            builder.AddContent(26, "Booking creation in progress. Refresh the page in a moment.");
            builder.CloseElement();
        }
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    }
    
    private void RenderCancelledPanel(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card border-danger");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-header bg-danger text-white");
        builder.OpenElement(4, "h5");
        builder.AddAttribute(5, "class", "mb-0");
        builder.AddContent(6, "? Quote Cancelled");
        builder.CloseElement();
        builder.CloseElement();
        
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "card-body");
        
        builder.OpenElement(9, "p");
        builder.AddAttribute(10, "class", "text-muted");
        builder.AddContent(11, "This quote has been cancelled and is now closed. No further action is required.");
        builder.CloseElement();
        
        if (quote?.UpdatedUtc.HasValue == true)
        {
            builder.OpenElement(12, "div");
            builder.AddAttribute(13, "class", "text-muted small");
            builder.OpenElement(14, "strong");
            builder.AddContent(15, "Cancelled:");
            builder.CloseElement();
            builder.AddContent(16, $" {quote.UpdatedUtc.Value.ToLocalTime().ToString("g")}");
            builder.CloseElement();
        }
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    }
}
