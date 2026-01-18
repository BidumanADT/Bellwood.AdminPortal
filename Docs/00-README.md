# Bellwood AdminPortal - Documentation

**Type**: Admin Web Portal (Blazor Server)  
**Framework**: .NET 8.0  
**Status**: ? Production Ready

---

## ?? Overview

The **Bellwood AdminPortal** is a Blazor Server web application that provides administrative staff with a comprehensive interface to manage bookings, quotes, drivers, affiliates, and real-time GPS tracking for the Bellwood Global executive car service platform.

**Key Capabilities**:
- ? Real-time GPS tracking with SignalR
- ? Booking and quote management
- ? Driver assignment and affiliate management
- ? Role-based access control (RBAC)
- ? Live status updates from drivers
- ? Professional UI/UX design

---

## ? Features

### Core Features
- **?? Booking Management** - View, filter, and manage customer bookings
- **?? Quote Management** - Review and price quote requests
- **?? Driver Assignment** - Assign drivers to bookings with email notifications
- **?? Affiliate Management** - Manage affiliate companies and their drivers
- **?? Real-Time GPS Tracking** - Live driver location on interactive map
- **?? User Access Control** - Role-based data filtering (Admin, Dispatcher, Booker)

### Technical Features
- **? SignalR Integration** - Real-time status updates and location tracking
- **?? JWT Authentication** - Secure authentication via AuthServer
- **?? Role-Based Authorization** - Admin-only and staff-only policies
- **?? Responsive Design** - Professional UI with Bellwood branding
- **?? Mobile-Friendly** - Works on tablets and desktops

---

## ?? Quick Start

### Prerequisites

**Required Software**:
- ? .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download))
- ? Visual Studio 2022 or VS Code
- ? AuthServer running on `https://localhost:5001`
- ? AdminAPI running on `https://localhost:5206`

**Verification**:
```bash
dotnet --version
# Expected: 8.0.x or higher
```

---

### Run Locally (5 Minutes)

#### Step 1: Clone and Navigate
```bash
git clone https://github.com/BidumanADT/Bellwood.AdminPortal
cd Bellwood.AdminPortal
```

#### Step 2: Restore Dependencies
```bash
dotnet restore
```

#### Step 3: Configure Settings

**File**: `appsettings.Development.json`

```json
{
  "AdminAPI": {
    "BaseUrl": "https://localhost:5206",
    "ApiKey": "dev-secret-123"
  },
  "GoogleMaps": {
    "ApiKey": "your-google-maps-key"
  }
}
```

#### Step 4: Seed Test Data (Optional)
```powershell
.\Scripts\seed-admin-api.ps1
```

#### Step 5: Run the Portal
```bash
dotnet run
```

**Access**: Navigate to `https://localhost:7257`

**Login**: Use `alice` / `password` or `bob` / `password`

---

## ?? Complete Documentation

### ?? Overview & Architecture
- **[00-README.md](00-README.md)** - This document (Quick start & feature overview)
- **[01-System-Architecture.md](01-System-Architecture.md)** - Technical design & components
- **[02-Testing-Guide.md](02-Testing-Guide.md)** - Testing strategies & workflows

### ?? Feature Documentation
- **[10-Real-Time-Tracking.md](10-Real-Time-Tracking.md)** - GPS tracking & SignalR implementation
- **[11-Quote-Management.md](11-Quote-Management.md)** - Quote workflow & pricing
- **[12-Driver-Assignment.md](12-Driver-Assignment.md)** - Driver assignment & notifications
- **[13-User-Access-Control.md](13-User-Access-Control.md)** - RBAC, Phase 1 & Phase 2 implementation
- **[14-Visual-Design.md](14-Visual-Design.md)** - UI/UX design system & branding

### ?? Technical References
- **[20-API-Reference.md](20-API-Reference.md)** - AdminAPI endpoints used by portal
- **[21-SignalR-Reference.md](21-SignalR-Reference.md)** - Real-time events & hub methods
- **[22-Data-Models.md](22-Data-Models.md)** - DTOs & entity schemas
- **[23-Security-Model.md](23-Security-Model.md)** - Authentication & authorization

### ?? Deployment & Operations
- **[30-Deployment-Guide.md](30-Deployment-Guide.md)** - Build, publish, deploy
- **[31-Scripts-Reference.md](31-Scripts-Reference.md)** - Automation scripts
- **[32-Troubleshooting.md](32-Troubleshooting.md)** - Common issues & solutions

### ?? Additional Resources
- **[BELLWOOD-DOCUMENTATION-STANDARD.md](BELLWOOD-DOCUMENTATION-STANDARD.md)** - Documentation guidelines
- **[Archive/](Archive/)** - Historical documentation (pre-Jan 2026 restructuring)

---

## ?? Integration Resources

### For External Teams

These guides are archived but available for reference:

**Driver Mobile App Team**:
- **[Archive/DRIVER_APP_COMPLETE_INTEGRATION_GUIDE.md](Archive/DRIVER_APP_COMPLETE_INTEGRATION_GUIDE.md)** - How to integrate with AdminAPI's GPS tracking and status updates

**AdminAPI Team**:
- **[Archive/EMAIL_FIX_FOR_ADMINAPI.md](Archive/EMAIL_FIX_FOR_ADMINAPI.md)** - Email notification template fixes

---

## ??? Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 8.0 | Runtime framework |
| **Blazor Server** | 8.0 | Web UI framework |
| **SignalR** | 8.0 | Real-time communication |
| **Bootstrap** | 5.3 | CSS framework |
| **Google Maps API** | - | Location visualization |

---

## ??? Project Structure

```
Bellwood.AdminPortal/
??? Components/
?   ??? Layout/              # Shared layouts (MainLayout, NavMenu)
?   ??? Pages/               # Blazor pages (Bookings, Quotes, etc.)
??? Services/                # Business logic services
?   ??? JwtAuthenticationStateProvider.cs
?   ??? QuoteService.cs
?   ??? AffiliateService.cs
?   ??? DriverTrackingService.cs
??? Models/                  # DTOs and data models
?   ??? QuoteModels.cs
?   ??? AffiliateModels.cs
?   ??? DriverTrackingModels.cs
??? Auth/                    # Authorization attributes
??? wwwroot/                 # Static files (CSS, JS, images)
??? Scripts/                 # PowerShell automation scripts
??? Docs/                    # This documentation
??? Program.cs               # Application entry point
```

---

## ?? Security & Authentication

**Authentication Method**: JWT Bearer tokens from AuthServer

**Authorization Policies**:
- `AdminOnly` - Requires admin role
- `StaffOnly` - Requires admin or dispatcher role

**Default Test Accounts**:
- **alice** / **password** - Admin role
- **bob** / **password** - Admin role

**See**: [23-Security-Model.md](23-Security-Model.md) for complete security documentation

---

## ?? Key Workflows

### Booking Management Workflow
```
1. Staff logs in ? JWT authentication
2. View bookings dashboard ? Filtered by role
3. Click booking ? View details
4. Assign driver ? Email sent to affiliate
5. Track driver ? Real-time GPS updates via SignalR
6. Status updates ? Displayed in real-time
```

### Quote Management Workflow
```
1. View quote requests ? From customer submissions
2. Click quote ? View trip details
3. Set price ? Enter quoted amount
4. Change status to "Priced" ? Customer notified
5. Customer accepts ? Convert to booking
```

---

## ?? Testing

### Quick Test
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit
```

### Manual Testing
1. Start AuthServer, AdminAPI, and AdminPortal
2. Login with `alice` / `password`
3. Navigate to Bookings ? Verify data loads
4. Navigate to Live Tracking ? Verify map displays
5. Assign a driver ? Verify email sent

**See**: [02-Testing-Guide.md](02-Testing-Guide.md) for comprehensive testing procedures

---

## ?? Deployment

### Quick Deploy to Production
```bash
# Build release
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release -o ./publish

# Deploy to IIS/Azure/Docker (see deployment guide)
```

**See**: [30-Deployment-Guide.md](30-Deployment-Guide.md) for complete deployment instructions

---

## ?? Troubleshooting

### Common Issues

**Issue**: "Failed to load bookings"
- **Check**: Are AuthServer and AdminAPI running?
- **Check**: Is API key configured correctly?

**Issue**: SignalR connection fails
- **Check**: Firewall blocking WebSocket connections?
- **Check**: HTTPS certificates valid?

**See**: [32-Troubleshooting.md](32-Troubleshooting.md) for complete troubleshooting guide

---

## ?? Contributing

### Documentation Updates

When updating code, please update corresponding documentation:

1. **Feature changes** ? Update relevant 10-19 series doc
2. **API changes** ? Update `20-API-Reference.md`
3. **Deployment changes** ? Update `30-Deployment-Guide.md`
4. **New issues** ? Update `32-Troubleshooting.md`

**Follow**: [BELLWOOD-DOCUMENTATION-STANDARD.md](BELLWOOD-DOCUMENTATION-STANDARD.md) for guidelines

---

## ?? Support & Contact

**Development Team**: Bellwood Platform Team  
**Repository**: https://github.com/BidumanADT/Bellwood.AdminPortal  
**Issues**: Use GitHub Issues for bug reports and feature requests

---

## ?? Version History

### v2.0 - January 2026
- ? Phase 1 RBAC implementation (audit fields, 403 handling)
- ? Real-time GPS tracking with SignalR
- ? Quote management feature
- ? Driver assignment with email notifications
- ? Documentation restructured to standard format

### v1.0 - December 2025
- ? Initial release
- ? Basic booking and quote management
- ? JWT authentication
- ? Affiliate and driver management

---

**Last Updated**: January 17, 2026  
**Status**: ? Production Ready  
**Version**: 2.0

---

*Welcome to the Bellwood AdminPortal! For questions or issues, check the documentation above or contact the development team.* ??
