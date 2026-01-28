using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Bellwood.AdminPortal.Models;
using Bellwood.AdminPortal.Services;

#pragma warning disable ASP0006 // RenderTreeBuilder sequence numbers - using index++ for maintainability in dynamic rendering

namespace Bellwood.AdminPortal.Components.Pages;

public partial class QuoteDetail
{
    // Phase B: Quote lifecycle workflow methods
    
    private async Task AcknowledgeQuote()
    {
        if (quote == null) return;

        isSaving = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var dto = new AcknowledgeQuoteDto
            {
                Notes = string.IsNullOrWhiteSpace(acknowledgeNotes) ? null : acknowledgeNotes
            };

            await QuoteService.AcknowledgeQuoteAsync(QuoteId, dto);

            successMessage = "Quote acknowledged successfully! You can now enter price and ETA estimates.";
            
            // Reload to get updated status
            await LoadQuoteAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            errorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to acknowledge quote: {ex.Message}";
            Console.WriteLine($"[QuoteDetail] Error acknowledging quote: {ex}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    private async Task RespondToQuote()
    {
        if (quote == null || !estimatedPrice.HasValue || !estimatedPickupTime.HasValue) return;

        isSaving = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var dto = new RespondToQuoteDto
            {
                EstimatedPrice = estimatedPrice.Value,
                EstimatedPickupTime = estimatedPickupTime.Value,
                Notes = string.IsNullOrWhiteSpace(responseNotes) ? null : responseNotes
            };

            await QuoteService.RespondToQuoteAsync(QuoteId, dto);

            successMessage = $"Response sent to customer with estimate: ${estimatedPrice.Value:N2}. Awaiting their acceptance.";
            
            // Reload to get updated status
            await LoadQuoteAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            errorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to respond to quote: {ex.Message}";
            Console.WriteLine($"[QuoteDetail] Error responding to quote: {ex}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    // Render fragment methods
    
    private RenderFragment RenderQuoteInformation() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "card");
        
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "card-body");
        
        builder.OpenElement(4, "h4");
        builder.AddAttribute(5, "class", "card-title mb-3");
        builder.AddContent(6, "Quote Information");
        builder.CloseElement(); // h4
        
        // Quote ID
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "mb-3");
        builder.OpenElement(9, "strong");
        builder.AddContent(10, "Quote ID:");
        builder.CloseElement();
        builder.AddContent(11, $" {quote?.Id}");
        builder.CloseElement(); // div
        
        // Status
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "class", "mb-3");
        builder.OpenElement(14, "strong");
        builder.AddContent(15, "Status:");
        builder.CloseElement();
        builder.AddContent(16, " ");
        builder.OpenElement(17, "span");
        builder.AddAttribute(18, "class", $"status-chip status-{GetStatusClass(quote?.Status)}");
        builder.AddContent(19, FormatStatus(quote?.Status));
        builder.CloseElement();
        builder.CloseElement(); // div
        
        // Created date
        builder.OpenElement(20, "div");
        builder.AddAttribute(21, "class", "mb-3");
        builder.OpenElement(22, "strong");
        builder.AddContent(23, "Created:");
        builder.CloseElement();
        builder.AddContent(24, $" {quote?.CreatedUtc.ToLocalTime().ToString("g")}");
        builder.CloseElement(); // div
        
        // Workflow timestamps
        if (quote?.AcknowledgedAt.HasValue == true)
        {
            builder.OpenElement(25, "div");
            builder.AddAttribute(26, "class", "mb-3");
            builder.OpenElement(27, "strong");
            builder.AddContent(28, "Acknowledged:");
            builder.CloseElement();
            builder.AddContent(29, $" {quote.AcknowledgedAt.Value.ToLocalTime().ToString("g")}");
            builder.CloseElement(); // div
        }
        
        if (quote?.RespondedAt.HasValue == true)
        {
            builder.OpenElement(30, "div");
            builder.AddAttribute(31, "class", "mb-3");
            builder.OpenElement(32, "strong");
            builder.AddContent(33, "Responded:");
            builder.CloseElement();
            builder.AddContent(34, $" {quote.RespondedAt.Value.ToLocalTime().ToString("g")}");
            builder.CloseElement(); // div
        }
        
        // Separator
        builder.OpenElement(35, "hr");
        builder.AddAttribute(36, "class", "my-3");
        builder.CloseElement();
        
        // Booker Information Section
        builder.OpenElement(37, "h5");
        builder.AddAttribute(38, "class", "mb-3");
        builder.AddContent(39, "Booker Information");
        builder.CloseElement();
        
        builder.OpenElement(40, "div");
        builder.AddAttribute(41, "class", "mb-2");
        builder.OpenElement(42, "strong");
        builder.AddContent(43, "Name:");
        builder.CloseElement();
        builder.AddContent(44, $" {quote?.BookerName}");
        builder.CloseElement();
        
        builder.OpenElement(45, "div");
        builder.AddAttribute(46, "class", "mb-2");
        builder.OpenElement(47, "strong");
        builder.AddContent(48, "Email:");
        builder.CloseElement();
        builder.AddContent(49, $" {quote?.BookerEmail}");
        builder.CloseElement();
        
        if (!string.IsNullOrEmpty(quote?.BookerPhone))
        {
            builder.OpenElement(50, "div");
            builder.AddAttribute(51, "class", "mb-2");
            builder.OpenElement(52, "strong");
            builder.AddContent(53, "Phone:");
            builder.CloseElement();
            builder.AddContent(54, $" {quote.BookerPhone}");
            builder.CloseElement();
        }
        
        // Trip Details Section
        builder.OpenElement(55, "hr");
        builder.AddAttribute(56, "class", "my-3");
        builder.CloseElement();
        
        builder.OpenElement(57, "h5");
        builder.AddAttribute(58, "class", "mb-3");
        builder.AddContent(59, "Trip Details");
        builder.CloseElement();
        
        RenderTripDetails(builder);
        
        // Notes sections
        if (!string.IsNullOrEmpty(quote?.AdminNotes))
        {
            builder.OpenElement(100, "hr");
            builder.AddAttribute(101, "class", "my-3");
            builder.CloseElement();
            
            builder.OpenElement(102, "h5");
            builder.AddAttribute(103, "class", "mb-3");
            builder.AddContent(104, "Admin Notes");
            builder.CloseElement();
            
            builder.OpenElement(105, "div");
            builder.AddAttribute(106, "class", "p-3");
            builder.AddAttribute(107, "style", "background: rgba(108, 117, 125, 0.1); border-radius: 8px;");
            builder.AddContent(108, quote.AdminNotes);
            builder.CloseElement();
        }
        
        if (!string.IsNullOrEmpty(quote?.Notes))
        {
            builder.OpenElement(109, "hr");
            builder.AddAttribute(110, "class", "my-3");
            builder.CloseElement();
            
            builder.OpenElement(111, "h5");
            builder.AddAttribute(112, "class", "mb-3");
            builder.AddContent(113, "Workflow Notes");
            builder.CloseElement();
            
            builder.OpenElement(114, "div");
            builder.AddAttribute(115, "class", "p-3");
            builder.AddAttribute(116, "style", "background: rgba(13, 110, 253, 0.1); border-radius: 8px;");
            builder.AddContent(117, quote.Notes);
            builder.CloseElement();
        }
        
        builder.CloseElement(); // card-body
        builder.CloseElement(); // card
    };
    
    private void RenderTripDetails(RenderTreeBuilder builder)
    {
        var index = 60;
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "Passenger:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.PassengerName}");
        builder.CloseElement();
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "Vehicle:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.VehicleClass}");
        builder.CloseElement();
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "Passengers:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.PassengerCount}");
        builder.CloseElement();
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "Luggage:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.Luggage} pieces");
        builder.CloseElement();
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "From:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.PickupLocation}");
        builder.CloseElement();
        
        if (!string.IsNullOrEmpty(quote?.DropoffLocation))
        {
            builder.OpenElement(index++, "div");
            builder.AddAttribute(index++, "class", "mb-2");
            builder.OpenElement(index++, "strong");
            builder.AddContent(index++, "To:");
            builder.CloseElement();
            builder.AddContent(index++, $" {quote.DropoffLocation}");
            builder.CloseElement();
        }
        
        builder.OpenElement(index++, "div");
        builder.AddAttribute(index++, "class", "mb-2");
        builder.OpenElement(index++, "strong");
        builder.AddContent(index++, "Pickup Time:");
        builder.CloseElement();
        builder.AddContent(index++, $" {quote?.PickupDateTime.ToLocalTime().ToString("g")}");
        builder.CloseElement();
        
        if (!string.IsNullOrEmpty(quote?.SpecialRequests))
        {
            builder.OpenElement(index++, "hr");
            builder.AddAttribute(index++, "class", "my-3");
            builder.CloseElement();
            
            builder.OpenElement(index++, "h5");
            builder.AddAttribute(index++, "class", "mb-3");
            builder.AddContent(index++, "Special Requests");
            builder.CloseElement();
            
            builder.OpenElement(index++, "div");
            builder.AddAttribute(index++, "class", "p-3");
            builder.AddAttribute(index++, "style", "background: rgba(203, 161, 53, 0.1); border-radius: 8px;");
            builder.AddContent(index++, quote.SpecialRequests);
            builder.CloseElement();
        }
    }
    
    private RenderFragment RenderActionPanel() => builder =>
    {
        if (quote == null) return;
        
        switch (quote.Status)
        {
            case "Pending":
                RenderPendingPanel(builder);
                break;
            case "Acknowledged":
                RenderAcknowledgedPanel(builder);
                break;
            case "Responded":
                RenderRespondedPanel(builder);
                break;
            case "Accepted":
                RenderAcceptedPanel(builder);
                break;
            case "Cancelled":
                RenderCancelledPanel(builder);
                break;
            default:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "alert alert-info");
                builder.AddContent(2, $"Status: {FormatStatus(quote.Status)}");
                builder.CloseElement();
                break;
        }
    };
    
    // Status-specific panel renderers continue in next file...
}
