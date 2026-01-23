# Scripts Reference

**Document Type**: Living Document - Deployment & Operations  
**Last Updated**: January 18, 2026  
**Status**: ? Production Ready

---

## ?? Overview

This document provides a complete reference for all PowerShell automation scripts in the Bellwood AdminPortal project, used for testing, development, and deployment.

**Scripts Location**: `/Scripts/`

**PowerShell Version**: 5.1+ compatible (Windows PowerShell and PowerShell Core)

**Target Audience**: Developers, DevOps engineers, QA team  
**Prerequisites**: PowerShell 5.1 or higher, AdminAPI and AuthServer running

---

## ?? Quick Reference

| Script | Purpose | Dependencies | Use Case |
|--------|---------|--------------|----------|
| [seed-admin-api.ps1](#seed-admin-apips1) | Seed test bookings | AdminAPI | Development, testing |
| [seed-affiliates-drivers.ps1](#seed-affiliates-driversps1) | Seed affiliates & drivers | AdminAPI, AuthServer | Development, testing |
| [seed-quotes.ps1](#seed-quotesps1) | Seed test quotes | AdminAPI | Development, testing |
| [clear-test-data.ps1](#clear-test-dataps1) | Wipe all test data | AdminAPI, AuthServer | Reset environment |
| [test-api-connection.ps1](#test-api-connectionps1) | Test AdminAPI connectivity | AdminAPI | Troubleshooting |

---

## ?? Script Details

### seed-admin-api.ps1

**Purpose**: Seed AdminAPI with test booking data for development and testing

**Location**: `Scripts/seed-admin-api.ps1`

**Dependencies**:
- AdminAPI running on `https://localhost:5206`
- `/bookings/seed` endpoint available

**Usage**:
```powershell
.\Scripts\seed-admin-api.ps1
```

**What It Does**:
1. Configures SSL certificate trust (development only)
2. Calls POST `/bookings/seed` endpoint
3. Displays seeding results

**Expected Output**:
```
Seeding AdminAPI with test bookings...
? Bookings seeded successfully!
{"added":3}
```

**Test Data Created**:
- 3 sample bookings with different statuses:
  - Taylor Reed (SUV, Requested)
  - Jordan Chen (Sedan, Confirmed)
  - Derek James (S-Class, Completed)

**When to Use**:
- Setting up development environment
- Testing bookings list page
- After clearing test data
- Demo/presentation preparation

**Troubleshooting**:
- **Error**: "Failed to seed bookings: Connection refused"
  - **Fix**: Ensure AdminAPI is running on port 5206
- **Error**: "SSL/TLS certificate validation failed"
  - **Fix**: Script should auto-handle, check certificate trust code

---

### seed-affiliates-drivers.ps1

**Purpose**: Comprehensive seeding of affiliates, drivers, and Charlie (test driver for integration)

**Location**: `Scripts/seed-affiliates-drivers.ps1`

**Dependencies**:
- AuthServer running on `https://localhost:5001`
- AdminAPI running on `https://localhost:5206`
- Test account `alice` / `password` in AuthServer

**Usage**:
```powershell
.\Scripts\seed-affiliates-drivers.ps1
```

**What It Does**:
1. Authenticates with AuthServer as `alice`
2. Seeds default affiliates via `/dev/seed-affiliates` endpoint
3. Creates "Downtown Express" affiliate
4. Adds "Charlie" as driver with UserUID `charlie-uid-001`
5. Lists all affiliates and drivers

**Expected Output**:
```
========================================
Bellwood Elite - Seed Affiliates & Drivers
========================================

Step 1: Authenticating with AuthServer...
? Authentication successful!

Step 2: Seeding default affiliates and drivers...
? Default affiliates seeded!
  Added: 2 affiliate(s)

Step 3: Creating Charlie's affiliate (Downtown Express)...
? Downtown Express affiliate created!
  Affiliate ID: aff-003

Step 4: Adding Charlie as a driver...
? Charlie added as driver!
  Driver ID: drv-004
  UserUID: charlie-uid-001

Step 5: Listing all affiliates...
? Current affiliates in system:

  ?? Chicago Limo Service
     Contact: John Smith
     Phone: (312) 555-1234
     Email: dispatch@chicagolimo.com
     Drivers: 2
       ?? Michael Johnson - (312) 555-0001
          UserUID: driver-001
       ?? Sarah Lee - (312) 555-0002
          UserUID: driver-002

  ?? Suburban Chauffeurs
     Contact: Emily Davis
     Phone: (847) 555-9876
     Email: emily@suburbanchauffeurs.com
     Drivers: 1
       ?? Robert Brown - (847) 555-1000
          UserUID: driver-003

  ?? Downtown Express
     Contact: Charlie Manager
     Phone: (312) 555-7890
     Email: charlie@downtownexpress.com
     Drivers: 1
       ?? Charlie - (312) 555-CHAS
          UserUID: charlie-uid-001

========================================
Seeding Complete!
========================================
```

**Test Data Created**:
- **Chicago Limo Service** (2 drivers)
  - Michael Johnson (UserUID: driver-001)
  - Sarah Lee (UserUID: driver-002)
- **Suburban Chauffeurs** (1 driver)
  - Robert Brown (UserUID: driver-003)
- **Downtown Express** (1 driver)
  - Charlie (UserUID: charlie-uid-001) ? Special test driver

**Charlie's Purpose**:
- Test driver for DriverApp integration
- UserUID `charlie-uid-001` enables login to DriverApp
- Used in end-to-end testing workflows
- Demonstrates driver assignment and GPS tracking

**When to Use**:
- Initial development setup
- Testing driver assignment feature
- DriverApp integration testing
- After clearing all data

**Troubleshooting**:
- **Error**: "Authentication failed"
  - **Fix**: Ensure AuthServer is running and `alice` account exists
- **Error**: "Failed to seed defaults"
  - **Fix**: Check AdminAPI `/dev/seed-affiliates` endpoint availability

---

### seed-quotes.ps1

**Purpose**: Seed AdminAPI with test quote requests for development and testing

**Location**: `Scripts/seed-quotes.ps1`

**Dependencies**:
- AdminAPI running on `https://localhost:5206`
- `/quotes/seed` endpoint available

**Usage**:
```powershell
.\Scripts\seed-quotes.ps1
```

**What It Does**:
1. Configures SSL certificate trust
2. Calls POST `/quotes/seed` endpoint
3. Displays seeding results

**Expected Output**:
```
Seeding AdminAPI with test quotes...
? Quotes seeded successfully!
{"added":3}
```

**Test Data Created**:
- 3 sample quote requests with various statuses:
  - Submitted quotes (new requests)
  - InReview quotes (being evaluated)
  - Priced quotes (ready for customer)

**When to Use**:
- Testing quote management page
- Testing quote pricing workflow
- Demo/presentation preparation
- After clearing test data

**Troubleshooting**:
- **Error**: "Failed to seed quotes"
  - **Fix**: Ensure AdminAPI is running on port 5206

---

### clear-test-data.ps1

**Purpose**: Wipe all affiliates and drivers from the system (with cascade delete)

**Location**: `Scripts/clear-test-data.ps1`

**Dependencies**:
- AuthServer running on `https://localhost:5001`
- AdminAPI running on `https://localhost:5206`
- Test account `alice` / `password` in AuthServer

**Usage**:
```powershell
.\Scripts\clear-test-data.ps1
```

**What It Does**:
1. Prompts for confirmation (requires typing "YES")
2. Authenticates with AuthServer
3. Fetches all affiliates
4. Deletes each affiliate (cascade deletes drivers)
5. Shows summary of deleted items

**Expected Output**:
```
========================================
Bellwood Elite - Clear All Test Data
========================================

WARNING: This will delete ALL affiliates and drivers!

Are you sure you want to proceed? Type 'YES' to confirm: YES

Proceeding with data wipe...

Step 1: Authenticating with AuthServer...
? Authentication successful!

Step 2: Fetching all affiliates...
? Found 3 affiliate(s)

Step 3: Deleting all affiliates and drivers...
  ? Deleted: Chicago Limo Service (and 2 driver(s))
  ? Deleted: Suburban Chauffeurs (and 1 driver(s))
  ? Deleted: Downtown Express (and 1 driver(s))

========================================
Data Wipe Complete!
========================================

Summary:
  Affiliates deleted: 3
  Failed deletions: 0

All test data has been cleared!

Next Steps:
1. Run seed script to add fresh data:
   .\seed-affiliates-drivers.ps1
```

**Safety Features**:
- Requires explicit "YES" confirmation (case-sensitive)
- Shows warning about cascade deletion
- Displays count of drivers to be deleted
- Color-coded output (red for warnings)

**When to Use**:
- Starting fresh after testing
- Resetting environment to clean state
- Before seeding new test data
- Debugging data corruption issues

**?? Warning**: 
- Does **not** delete bookings or quotes (historical data preserved)
- **Cannot be undone** - all affiliates and drivers permanently deleted
- Requires manual re-seeding after clearing

**Troubleshooting**:
- **Script cancels**: Ensure you type "YES" exactly (case-sensitive)
- **Partial deletions**: Some affiliates deleted but not all
  - **Fix**: Check AdminAPI logs for errors, re-run script

---

### test-api-connection.ps1

**Purpose**: Verify AdminAPI connectivity and basic functionality

**Location**: `Scripts/test-api-connection.ps1`

**Dependencies**:
- AdminAPI running on `https://localhost:5206`

**Usage**:
```powershell
.\Scripts\test-api-connection.ps1
```

**What It Does**:
1. Tests `/health` endpoint (no auth required)
2. Seeds test bookings via `/bookings/seed`
3. Fetches bookings list with API key
4. Displays sample booking data

**Expected Output**:
```
Testing Bellwood AdminPortal ? AdminAPI Connection
=================================================

[1/3] Testing health endpoint...
? Health check passed

[2/3] Seeding test bookings...
? Seeded 3 bookings

[3/3] Fetching bookings list...
? Fetched 3 bookings

Sample bookings:
  - Taylor Reed | SUV | Requested
  - Jordan Chen | Sedan | Confirmed
  - Derek James | S-Class | Completed

=================================================
? All tests passed! AdminAPI is ready.

Next steps:
1. Ensure AuthServer is running on https://localhost:5001
2. Run the AdminPortal: dotnet run
3. Navigate to https://localhost:7257
4. Login with alice/password or bob/password
```

**Test Coverage**:
- ? Network connectivity
- ? SSL/HTTPS configuration
- ? Health endpoint availability
- ? Seed endpoints functionality
- ? API key authentication
- ? JSON serialization/deserialization

**When to Use**:
- Verifying AdminAPI is running correctly
- Troubleshooting connection issues
- Post-deployment smoke test
- Before running AdminPortal

**Troubleshooting**:
- **Error**: "Health check failed"
  - **Fix**: Ensure AdminAPI is running on port 5206
- **Error**: "Seed failed"
  - **Fix**: Check AdminAPI logs for errors
- **Error**: "Fetch failed"
  - **Fix**: Verify API key is `dev-secret-123` in AdminAPI config

---

## ?? Common Scenarios

### Fresh Environment Setup

**Goal**: Set up complete test environment from scratch

**Steps**:
```powershell
# 1. Test API connectivity
.\Scripts\test-api-connection.ps1

# 2. Seed affiliates and drivers
.\Scripts\seed-affiliates-drivers.ps1

# 3. Seed quotes (optional)
.\Scripts\seed-quotes.ps1
```

**Result**: Full test data ready for development

---

### Reset Environment

**Goal**: Clear all data and start fresh

**Steps**:
```powershell
# 1. Clear all affiliates and drivers
.\Scripts\clear-test-data.ps1
# Type "YES" to confirm

# 2. Re-seed fresh data
.\Scripts\seed-affiliates-drivers.ps1

# 3. Seed bookings
.\Scripts\seed-admin-api.ps1
```

**Result**: Clean slate with fresh test data

---

### End-to-End Testing Prep

**Goal**: Prepare for complete E2E testing including DriverApp

**Steps**:
```powershell
# 1. Clear existing data
.\Scripts\clear-test-data.ps1

# 2. Seed affiliates WITH Charlie
.\Scripts\seed-affiliates-drivers.ps1

# 3. Seed bookings
.\Scripts\seed-admin-api.ps1

# 4. Verify Charlie exists
# Look for: Charlie (UserUID: charlie-uid-001)

# 5. In AdminPortal: Assign Charlie to a booking
# 6. In DriverApp: Login as charlie-uid-001
# 7. Verify Charlie sees assigned booking
```

**Result**: Ready for full integration testing

---

### Troubleshooting Connection Issues

**Goal**: Diagnose AdminAPI connectivity problems

**Steps**:
```powershell
# 1. Test basic connectivity
.\Scripts\test-api-connection.ps1

# If health check fails:
# - Verify AdminAPI is running
# - Check port 5206 is not in use
# - Review AdminAPI console for errors

# If seed fails:
# - Check AdminAPI logs
# - Verify seed endpoints exist
# - Test with Postman/curl

# If fetch fails:
# - Verify API key matches
# - Check authentication middleware
```

---

## ?? Security Considerations

### Development Only Features

**Certificate Trust**:
```powershell
# ?? This code bypasses SSL validation
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
```

**?? Never use in production**
- Only safe for local development
- Production should use valid SSL certificates
- Remove before deploying to staging/production

---

### API Keys

**Hardcoded Values**:
```powershell
$apiKey = "dev-secret-123"
```

**Recommendations**:
- Use environment variables in production
- Rotate keys regularly
- Never commit production keys to source control

**Better Approach**:
```powershell
$apiKey = $env:ADMIN_API_KEY ?? "dev-secret-123"
```

---

## ?? Script Output Reference

### Success Indicators

**Health Check**: `? Health check passed`  
**Authentication**: `? Authentication successful!`  
**Seeding**: `? Seeded X items`  
**Deletion**: `? Deleted: [name]`

### Error Indicators

**Connection Failed**: `? Failed to connect`  
**Authentication Failed**: `? Authentication failed`  
**Seed Failed**: `? Failed to seed: [error]`

### Color Coding

- **Green** (?): Success messages
- **Red** (?): Error messages
- **Yellow** (??): Warning messages
- **Cyan**: Section headers
- **White/Gray**: Informational text

---

## ?? Testing Scripts

### Unit Testing Scripts

**Not implemented yet**

**Future Enhancement**:
- Add Pester tests for script validation
- Mock API responses
- Automated regression testing

**Example**:
```powershell
# test-scripts.ps1 (future)
Describe "seed-admin-api.ps1" {
    It "Should seed bookings successfully" {
        # Test implementation
    }
}
```

---

## ?? Related Documentation

- [Deployment Guide](30-Deployment-Guide.md) - Production deployment procedures
- [Testing Guide](02-Testing-Guide.md) - Comprehensive testing workflows
- [Troubleshooting](32-Troubleshooting.md) - Common issues and solutions
- [API Reference](20-API-Reference.md) - Endpoint documentation

---

## ?? Maintenance

### Adding New Scripts

**When creating a new script**:
1. Add to `/Scripts/` folder
2. Include clear header comment with purpose
3. Add to [Quick Reference](#-quick-reference) table above
4. Document parameters and usage
5. Add troubleshooting section
6. Update this reference document

**Template**:
```powershell
# Script Name: my-new-script.ps1
# Purpose: [Clear description]
# Dependencies: [List required services]
# Usage: .\Scripts\my-new-script.ps1

# [Script implementation]
```

---

### Versioning

**Current Script Versions**:
- All scripts: v1.0 (January 2026)
- Compatible with: PowerShell 5.1+
- Tested on: Windows 10/11, PowerShell Core 7.x

**Changelog**:
- **v1.0** (Jan 2026): Initial release, Phase 1 documentation

---

**Last Updated**: January 18, 2026  
**Status**: ? Production Ready  
**Version**: 2.0 (Post-reorganization)

---

*This scripts reference documents all automation scripts for the AdminPortal. Keep this updated when adding or modifying scripts.* ???
