# Quote Management

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 17, 2026  
**Status**: ? Production Ready

---

## ?? Overview

The Quote Management feature enables administrative staff to review quote requests from customers, add pricing, manage quote status, and track the quote-to-booking conversion workflow.

**Key Capabilities**:
- ?? View and filter quote requests
- ?? Add pricing to quotes
- ?? Manage quote status lifecycle
- ?? Internal notes for quote history
- ?? Customer notifications when quotes are priced
- ? Quote-to-booking conversion support

**Target Audience**: Developers, administrative staff  
**Prerequisites**: Understanding of booking workflow, AdminAPI integration

---

## ?? Business Use Case

### For Customers

1. **Submit Quote Request** (via PassengerApp or website)
   - Enter trip details (pickup, dropoff, date/time)
   - Select vehicle class
   - Provide passenger count and luggage
   - Add special requests

2. **Receive Pricing** (automated notification)
   - Get email when quote is priced
   - View quoted amount in customer portal
   - Accept or decline quote

3. **Convert to Booking** (if accepted)
   - Quote becomes confirmed booking
   - Payment processed
   - Driver assigned

---

### For Administrative Staff

1. **Review Requests**
   - View incoming quote requests
   - Filter by status (Submitted, InReview, Priced)
   - Prioritize urgent quotes

2. **Price Quotes**
   - Review trip details and requirements
   - Calculate pricing based on distance, vehicle class, etc.
   - Enter quoted amount
   - Add internal notes for context

3. **Manage Workflow**
   - Mark quotes as "InReview" while calculating
   - Change status to "Priced" to notify customer
   - Reject quotes that can't be fulfilled
   - Close completed quotes

4. **Track Conversions**
   - Monitor which quotes convert to bookings
   - Analyze pricing trends
   - Follow up on declined quotes

---

## ??? Architecture

### Component Overview

```
???????????????????????????????????????????????????????????????
?                     AdminPortal                              ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? Quotes.razor                                   ?        ?
?  ?  - List view with filters                      ?        ?
?  ?  - Search functionality                        ?        ?
?  ?  - Quote cards with status badges              ?        ?
?  ??????????????????????????????????????????????????        ?
?                ? Click quote ? Navigate                     ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? QuoteDetail.razor                              ?        ?
?  ?  - Quote information display                   ?        ?
?  ?  - Pricing form                                ?        ?
?  ?  - Status management                           ?        ?
?  ?  - Admin notes                                 ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
?  ??????????????????????????????????????????????????        ?
?  ? QuoteService                                   ?        ?
?  ?  - GetQuotesAsync()                            ?        ?
?  ?  - GetQuoteAsync(id)                           ?        ?
?  ?  - UpdateQuoteAsync(id, dto)                   ?        ?
?  ??????????????????????????????????????????????????        ?
?                ?                                             ?
????????????????????????????????????????????????????????????????
                 ? HTTP + JWT + API Key
                 ?
???????????????????????????????????????????????????????????????
?                     AdminAPI                                 ?
?                                                              ?
?  ??????????????????????????????????????????????????        ?
?  ? Quote Endpoints                                ?        ?
?  ?  - GET /quotes/list?take=100                   ?        ?
?  ?  - GET /quotes/{id}                            ?        ?
?  ?  - PUT /quotes/{id}                            ?        ?
?  ??????????????????????????????????????????????????        ?
?                                                              ?
????????????????????????????????????????????????????????????????
```

---

## ?? Quote Status Lifecycle

### Status Flow

```
????????????????
?  Submitted   ? ? Customer submits quote request
????????????????
       ?
       ?
????????????????
?   InReview   ? ? Admin reviewing, calculating price
????????????????
       ?
       ???????????????
       ?             ?
       ?             ?
????????????????  ????????????????
?    Priced    ?  ?   Rejected   ?
????????????????  ????????????????
       ?                 ?
       ?                 ?
       ???????????????????
       ?                 ?
       ?                 ?
????????????????  ????????????????
?    Closed    ?  ?    Closed    ?
????????????????  ????????????????
  (Converted to      (Quote not
   booking or         fulfilled)
   expired)
```

---

### Status Definitions

| Status | Description | Visible to Customer | Actions Available |
|--------|-------------|---------------------|-------------------|
| **Submitted** | Initial state when quote request arrives | Yes (pending) | Review, Price, Reject |
| **InReview** | Admin is processing the quote | Yes (processing) | Price, Reject |
| **Priced** | Price set, customer can view and accept | Yes (with price) | Close |
| **Rejected** | Quote declined by admin | Yes (declined) | Close |
| **Closed** | Quote completed (accepted, expired, or archived) | Yes (final) | None |

---

## ??? Data Models

### QuoteDetailDto

**File**: `Models/QuoteModels.cs`

```csharp
public class QuoteDetailDto
{
    // Metadata
    public string Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string? Status { get; set; }

    // Booker Information
    public string? BookerName { get; set; }
    public string? BookerEmail { get; set; }
    public string? BookerPhone { get; set; }

    // Passenger Information
    public string? PassengerName { get; set; }
    public string? PassengerPhone { get; set; }

    // Trip Details
    public string? PickupLocation { get; set; }
    public string? DropoffLocation { get; set; }
    public DateTime? PickupDateTime { get; set; }
    public string? VehicleClass { get; set; }
    public int? PassengerCount { get; set; }
    public int? LuggageCount { get; set; }
    public string? SpecialRequests { get; set; }

    // Pricing & Notes
    public decimal? QuotedPrice { get; set; }
    public string? AdminNotes { get; set; }

    // Phase 1: Audit Fields
    public string? CreatedByUserId { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }
}
```

---

### UpdateQuoteDto

**Purpose**: Focused DTO for update operations (Command-Query Separation)

```csharp
public class UpdateQuoteDto
{
    /// <summary>
    /// Quoted price in dollars (decimal for precision)
    /// </summary>
    public decimal? QuotedPrice { get; set; }

    /// <summary>
    /// Quote status (Submitted, InReview, Priced, Rejected, Closed)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Internal admin notes (not visible to customers)
    /// </summary>
    public string? AdminNotes { get; set; }
}
```

**See**: [22-Data-Models.md](22-Data-Models.md) for complete model documentation

---

## ??? Quote Service

### IQuoteService Interface

**File**: `Services/QuoteService.cs`

```csharp
public interface IQuoteService
{
    /// <summary>
    /// Get list of quote requests (up to specified limit)
    /// </summary>
    Task<List<QuoteDetailDto>> GetQuotesAsync(int take = 100);

    /// <summary>
    /// Get detailed information for a specific quote
    /// </summary>
    /// <returns>Quote details, or null if not found</returns>
    Task<QuoteDetailDto?> GetQuoteAsync(string id);

    /// <summary>
    /// Update quote pricing, status, or admin notes
    /// </summary>
    /// <exception cref="HttpRequestException">API call failed</exception>
    /// <exception cref="UnauthorizedAccessException">403 Forbidden</exception>
    Task UpdateQuoteAsync(string id, UpdateQuoteDto updateDto);
}
```

---

### Authorization

All API calls include:

```csharp
private async Task<HttpClient> GetAuthorizedClientAsync()
{
    var client = _httpFactory.CreateClient("AdminAPI");
    
    // Add API key
    var apiKey = _apiKeyProvider.GetApiKey();
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Admin-ApiKey", apiKey);
    }
    
    // Add JWT token
    var token = await _tokenProvider.GetTokenAsync();
    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    return client;
}
```

---

### Error Handling

**403 Forbidden**:
```csharp
if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
{
    throw new UnauthorizedAccessException(
        "You don't have permission to update this quote.");
}
```

**Network Errors**:
```csharp
catch (HttpRequestException ex)
{
    Console.WriteLine($"[QuoteService] HTTP error: {ex.Message}");
    throw new Exception("Failed to connect to AdminAPI. Please try again.", ex);
}
```

---

## ?? Quotes List Page

### Quotes.razor

**Route**: `/quotes`

**Features**:
- View all quote requests
- Filter by status (All, Submitted, InReview, Priced, Rejected, Closed)
- Search by passenger name, booker name, or location
- Click quote card to view details

**UI Components**:

```
???????????????????????????????????????????????????????????
? Quotes Dashboard                                        ?
???????????????????????????????????????????????????????????
? [All (12)] [Submitted (5)] [InReview (3)] [Priced (4)] ?
?                                                          ?
? Search: [________________________] ??                   ?
?                                                          ?
? ????????????????????????????????????????????????????   ?
? ? ? Maria Garcia ? Business Meeting               ?   ?
? ? O'Hare Airport ? Downtown Chicago                ?   ?
? ? SUV • 4 passengers • 6 luggage                   ?   ?
? ? Dec 28, 2025 3:00 PM                             ?   ?
? ? [Submitted]                              [View ?]?   ?
? ????????????????????????????????????????????????????   ?
?                                                          ?
? ????????????????????????????????????????????????????   ?
? ? ? Robert Chen ? Airport Transfer                ?   ?
? ? Langham Hotel ? Midway Airport                   ?   ?
? ? Sedan • 2 passengers • 2 luggage                 ?   ?
? ? Dec 29, 2025 10:00 AM                            ?   ?
? ? [InReview]                               [View ?]?   ?
? ????????????????????????????????????????????????????   ?
???????????????????????????????????????????????????????????
```

---

### Filter Logic

```csharp
private List<QuoteDetailDto> FilterQuotes(string filter)
{
    if (filter == "All")
        return allQuotes;

    return allQuotes.Where(q => 
        string.Equals(q.Status, filter, StringComparison.OrdinalIgnoreCase)
    ).ToList();
}
```

**Filters**:
- **All** - Show all quotes
- **Submitted** - New quotes awaiting review
- **InReview** - Quotes being processed by admin
- **Priced** - Quotes with pricing ready for customer
- **Rejected** - Declined quote requests
- **Closed** - Completed/archived quotes

---

### Search Functionality

```csharp
private List<QuoteDetailDto> SearchQuotes(string query)
{
    if (string.IsNullOrWhiteSpace(query))
        return filteredQuotes;

    query = query.ToLower();

    return filteredQuotes.Where(q =>
        (q.PassengerName?.ToLower().Contains(query) ?? false) ||
        (q.BookerName?.ToLower().Contains(query) ?? false) ||
        (q.PickupLocation?.ToLower().Contains(query) ?? false) ||
        (q.DropoffLocation?.ToLower().Contains(query) ?? false)
    ).ToList();
}
```

---

## ?? Quote Detail Page

### QuoteDetail.razor

**Route**: `/quotes/{QuoteId}`

**Layout**: Dual-column design

---

### Left Column - Information Display

**Quote Metadata**:
- Quote ID (unique identifier)
- Status badge with color coding
- Created timestamp
- Last updated timestamp

**Booker Contact**:
- Name
- Email
- Phone number

**Trip Details**:
- Pickup location
- Dropoff location
- Pickup date and time
- Vehicle class (Sedan, SUV, S-Class, etc.)
- Passenger count
- Luggage count
- Special requests (if any)

**Example UI**:
```
???????????????????????????????????????????
? Quote Request Details                   ?
???????????????????????????????????????????
? Quote ID: QT-2025-001234                ?
? Status: [Submitted]                     ?
? Created: Dec 24, 2025 2:30 PM          ?
?                                         ?
? Booker Information                      ?
? ?? Name: Maria Garcia                  ?
? ?? Email: maria@company.com            ?
? ?? Phone: (312) 555-1234               ?
?                                         ?
? Trip Details                            ?
? ?? From: O'Hare International Airport  ?
? ?? To: 123 N Michigan Ave, Chicago     ?
? ?? When: Dec 28, 2025 3:00 PM         ?
? ?? Vehicle: SUV                        ?
? ?? Passengers: 4                       ?
? ?? Luggage: 6                          ?
? ?? Special: Meet at arrivals terminal  ?
???????????????????????????????????????????
```

---

### Right Column - Management Panel

**Pricing Section**:
```razor
<div class="mb-3">
    <label for="quotedPrice" class="form-label">Quoted Price ($)</label>
    <input type="number" 
           class="form-control" 
           id="quotedPrice"
           @bind="quotedPrice" 
           min="0" 
           step="0.01"
           placeholder="Enter price in dollars" />
</div>
```

**Features**:
- Numeric input with validation
- Min: $0.00
- Step: $0.01 (penny precision)
- Currency formatting on blur

---

**Status Management**:
```razor
<div class="mb-3">
    <label for="status" class="form-label">Status</label>
    <select class="form-select" id="status" @bind="selectedStatus">
        <option value="">-- Select Status --</option>
        <option value="Submitted">Submitted</option>
        <option value="InReview">In Review</option>
        <option value="Priced">Priced (Customer Notified)</option>
        <option value="Rejected">Rejected</option>
        <option value="Closed">Closed</option>
    </select>
</div>
```

---

**Admin Notes**:
```razor
<div class="mb-3">
    <label for="adminNotes" class="form-label">
        Admin Notes (Internal Only)
    </label>
    <textarea class="form-control" 
              id="adminNotes"
              @bind="adminNotes" 
              rows="4"
              placeholder="Add internal notes about this quote..."></textarea>
    <small class="text-muted">
        These notes are not visible to the customer.
    </small>
</div>
```

**Use Cases**:
- Document pricing calculations
- Note special arrangements
- Record follow-up actions
- Track quote history

---

**Action Buttons**:
```razor
<div class="d-grid gap-2">
    <button class="btn btn-primary" 
            @onclick="SaveQuote" 
            disabled="@isSaving">
        @if (isSaving)
        {
            <span class="spinner-border spinner-border-sm me-2"></span>
            <text>Saving...</text>
        }
        else
        {
            <text>?? Save Changes</text>
        }
    </button>
    
    <button class="btn btn-outline-secondary" 
            @onclick="ResetForm"
            disabled="@isSaving">
        Reset
    </button>
</div>
```

**Loading States**:
- Disable buttons during save
- Show spinner during async operations
- Prevent double-submission

---

**Quick Actions**:
```razor
<div class="mt-3">
    <h6>Quick Actions</h6>
    <div class="d-grid gap-2">
        <button class="btn btn-success btn-sm" 
                @onclick="() => QuickSetStatus('Priced')">
            ? Mark as Priced
        </button>
        <button class="btn btn-warning btn-sm" 
                @onclick="() => QuickSetStatus('InReview')">
            ?? Mark In Review
        </button>
        <button class="btn btn-danger btn-sm" 
                @onclick="() => QuickSetStatus('Rejected')">
            ? Reject Quote
        </button>
    </div>
</div>
```

**Quick Action Logic**:
```csharp
private async Task QuickSetStatus(string newStatus)
{
    selectedStatus = newStatus;
    await SaveQuote();
}
```

---

### User Feedback

**Success Messages**:
```razor
@if (!string.IsNullOrEmpty(successMessage))
{
    <div class="alert alert-success alert-dismissible fade show">
        @successMessage
        <button type="button" class="btn-close" @onclick="() => successMessage = null"></button>
    </div>
}
```

**Error Messages**:
```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger alert-dismissible fade show">
        @errorMessage
        <button type="button" class="btn-close" @onclick="() => errorMessage = null"></button>
    </div>
}
```

---

### Save Operation

```csharp
private async Task SaveQuote()
{
    if (quote == null) return;

    isSaving = true;
    errorMessage = null;
    successMessage = null;

    try
    {
        // Prepare update DTO
        var updateDto = new UpdateQuoteDto
        {
            QuotedPrice = quotedPrice,
            Status = string.IsNullOrWhiteSpace(selectedStatus) ? null : selectedStatus,
            AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes
        };

        // Call service
        await QuoteService.UpdateQuoteAsync(QuoteId, updateDto);

        // Update local state
        quote.QuotedPrice = quotedPrice;
        quote.Status = selectedStatus;
        quote.AdminNotes = adminNotes;
        quote.UpdatedUtc = DateTime.UtcNow;

        // Show success
        successMessage = "? Quote updated successfully!";
        
        // Auto-dismiss after 3 seconds
        await Task.Delay(3000);
        successMessage = null;
    }
    catch (UnauthorizedAccessException ex)
    {
        errorMessage = $"Access denied: {ex.Message}";
    }
    catch (Exception ex)
    {
        errorMessage = $"Failed to update quote: {ex.Message}";
        Console.WriteLine($"[QuoteDetail] Save error: {ex}");
    }
    finally
    {
        isSaving = false;
    }
}
```

---

## ?? Customer Notifications

### When Status Changes to "Priced"

**Backend Behavior** (AdminAPI):
1. Detect status change to "Priced"
2. Trigger email notification service
3. Send email to booker with:
   - Quote details
   - Quoted price
   - Accept/Decline buttons
   - Link to customer portal

**Email Template** (Example):
```
Subject: Your Quote is Ready - Bellwood Global

Dear Maria,

Your quote request for:
- Pickup: O'Hare Airport
- Dropoff: Downtown Chicago
- Date: Dec 28, 2025 3:00 PM
- Vehicle: SUV

Quoted Price: $150.00

[Accept Quote] [View Details] [Decline]

This quote is valid for 48 hours.

Thank you,
Bellwood Global Team
```

**Customer Portal Integration**:
- Quote appears in "My Quotes" section
- Shows pricing and details
- "Accept" button converts to booking
- "Decline" button marks quote as closed

---

## ?? Testing

### Manual Testing Procedures

**Test 1: Quote List & Navigation**:
1. Navigate to `/quotes`
2. **Verify**: Quote requests display
3. Click a quote card
4. **Verify**: Navigates to `/quotes/{id}`
5. **Verify**: Back button returns to list

**Test 2: Pricing**:
1. Open quote detail page
2. Enter price: `150.50`
3. Click "Save Changes"
4. **Verify**: Success message displays
5. **Verify**: Price persists on refresh

**Test 3: Status Update**:
1. Change status dropdown to "Priced"
2. Click "Save Changes"
3. **Verify**: Status updates
4. **Verify**: Customer receives notification email

**Test 4: Quick Actions**:
1. Click "Mark as Priced" button
2. **Verify**: Status changes to "Priced"
3. **Verify**: No price change if already set

**Test 5: Admin Notes**:
1. Enter notes: "Customer requested premium vehicle"
2. Save quote
3. **Verify**: Notes save and display
4. **Verify**: Notes not visible in customer portal

**Test 6: Error Handling**:
1. Disconnect AdminAPI
2. Try to save quote
3. **Verify**: Error message displays
4. **Verify**: No data corruption

**See**: [02-Testing-Guide.md](02-Testing-Guide.md) for comprehensive testing procedures

---

## ?? Quote Analytics (Future)

### Planned Metrics

**Conversion Rate**:
- Quotes submitted vs. accepted
- Average time to quote pricing
- Rejection reasons

**Pricing Trends**:
- Average quote price by vehicle class
- Price variance by route
- Seasonal pricing patterns

**Customer Behavior**:
- Most common routes
- Average passenger count
- Special request frequency

**Admin Performance**:
- Time to first response
- Quote volume per admin
- Acceptance rate by admin

---

## ?? Future Enhancements

### Planned Features

1. **Batch Operations**
   - Select multiple quotes
   - Bulk status updates
   - Mass rejection with template message

2. **Quote Templates**
   - Save pricing structures
   - Quick-apply for common routes
   - Distance-based pricing calculator

3. **Email Integration**
   - Preview email before sending
   - Custom message templates
   - Attachment support (T&Cs, route map)

4. **Quote Comparison**
   - Side-by-side quote view
   - Historical pricing for similar trips
   - Competitor pricing reference

5. **Real-Time Updates**
   - SignalR for live quote submissions
   - Notifications when customer responds
   - Collaborative quote review

6. **Advanced Filtering**
   - Filter by date range
   - Filter by vehicle class
   - Filter by price range

---

## ?? Related Documentation

- [System Architecture](01-System-Architecture.md) - Overall design
- [Data Models](22-Data-Models.md) - Quote DTOs
- [API Reference](20-API-Reference.md) - Quote endpoints
- [Security Model](23-Security-Model.md) - Authentication requirements
- [User Access Control](13-User-Access-Control.md) - RBAC for quotes

---

**Last Updated**: January 17, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*Quote Management enables efficient pricing and workflow management, converting customer inquiries into confirmed bookings.* ???
