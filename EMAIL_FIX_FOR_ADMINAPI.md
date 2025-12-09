# Email Greeting Fix for AdminAPI

## Location
**File:** `Services\SmtpEmailSender.cs` in the AdminAPI project

## Current Implementation (Incorrect)
```csharp
var emailBody = $@"
Hello {driver.Name},

A driver from your affiliate has been assigned to a booking:
...
";
```

## Fixed Implementation (Correct)
```csharp
var emailBody = $@"
Hello {affiliate.Name} Team,

A driver from your affiliate has been assigned to a booking:
...
";
```

## Detailed Fix

### Method: `SendDriverAssignmentAsync`

**Find this section:**
```csharp
public async Task SendDriverAssignmentAsync(BookingRecord booking, Driver driver, Affiliate affiliate)
{
    var subject = $"Bellwood Elite - Driver Assignment - {booking.PickupDateTime:g}";
    
    var emailBody = $@"
Hello {driver.Name},  // ? WRONG! This addresses the driver
```

**Replace with:**
```csharp
public async Task SendDriverAssignmentAsync(BookingRecord booking, Driver driver, Affiliate affiliate)
{
    var subject = $"Bellwood Elite - Driver Assignment - {booking.PickupDateTime:g}";
    
    var emailBody = $@"
Hello {affiliate.Name} Team,  // ? CORRECT! This addresses the affiliate team
```

## Complete Email Template

The full email body should be:

```csharp
var emailBody = $@"
Hello {affiliate.Name} Team,

A driver from your affiliate has been assigned to a booking:

Driver Information:
------------------
Name: {driver.Name}
Phone: {driver.Phone}

Booking Details:
---------------
Reference ID: {booking.Id}
Passenger: {booking.PassengerName}
Pickup Date/Time: {booking.PickupDateTime:f}
Pickup Location: {booking.PickupLocation}
Dropoff Location: {booking.DropoffLocation ?? "Not specified"}
Vehicle Class: {booking.VehicleClass}
Passenger Count: {booking.PassengerCount}

Please ensure the driver is prepared and available for this assignment.

Thank you,
Bellwood Elite Team
";
```

## HTML Version (if using HTML emails)

```csharp
var htmlBody = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #CBA135;'>Bellwood Elite - Driver Assignment</h2>
    
    <p>Hello <strong>{affiliate.Name} Team</strong>,</p>
    
    <p>A driver from your affiliate has been assigned to a booking:</p>
    
    <h3>Driver Information</h3>
    <ul>
        <li><strong>Name:</strong> {driver.Name}</li>
        <li><strong>Phone:</strong> {driver.Phone}</li>
    </ul>
    
    <h3>Booking Details</h3>
    <ul>
        <li><strong>Reference ID:</strong> {booking.Id}</li>
        <li><strong>Passenger:</strong> {booking.PassengerName}</li>
        <li><strong>Pickup Date/Time:</strong> {booking.PickupDateTime:f}</li>
        <li><strong>Pickup Location:</strong> {booking.PickupLocation}</li>
        <li><strong>Dropoff Location:</strong> {booking.DropoffLocation ?? "Not specified"}</li>
        <li><strong>Vehicle Class:</strong> {booking.VehicleClass}</li>
        <li><strong>Passenger Count:</strong> {booking.PassengerCount}</li>
    </ul>
    
    <p>Please ensure the driver is prepared and available for this assignment.</p>
    
    <p>Thank you,<br/>
    <strong>Bellwood Elite Team</strong></p>
</body>
</html>
";
```

## Example Emails

### Before (Wrong):
```
To: dispatch@chicagolimo.com
Subject: Bellwood Elite - Driver Assignment - 1/15/2024 2:30 PM

Hello Michael Johnson,  ? Addressing the driver (WRONG)

A driver from your affiliate has been assigned...
```

### After (Correct):
```
To: dispatch@chicagolimo.com
Subject: Bellwood Elite - Driver Assignment - 1/15/2024 2:30 PM

Hello Chicago Limo Service Team,  ? Addressing the affiliate team (CORRECT)

A driver from your affiliate has been assigned...
```

## Testing

After making this change:

1. Restart AdminAPI
2. Assign a driver to a booking in AdminPortal
3. Check AdminAPI console logs for email content
4. Verify greeting says "{Affiliate Name} Team"

## Notes

- The email is sent to **affiliate.Email** (e.g., dispatch@chicagolimo.com)
- The affiliate dispatch team receives the notification
- The driver's name appears in the **Driver Information** section
- This makes more sense as the affiliate coordinates driver assignments
