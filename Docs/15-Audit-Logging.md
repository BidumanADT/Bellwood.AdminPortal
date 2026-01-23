# Audit Logging & Compliance

**Document Type**: Living Document - Feature Documentation  
**Last Updated**: January 19, 2026  
**Status**: ? Production Ready (Phase 3 Complete)

---

## ?? Overview

This document describes the **Audit Logging** system in the Bellwood AdminPortal, providing administrators with comprehensive visibility into all system activities for compliance, security monitoring, and troubleshooting.

**Initiative**: Enterprise-grade audit logging for alpha testing and production compliance  
**Priority**: ?? **CRITICAL** - Required for alpha testing and regulatory compliance  
**Status**: Phase 3 ? **COMPLETE**

**Target Audience**: Administrators, compliance officers, security engineers, QA team  
**Prerequisites**: Understanding of audit logging concepts, compliance requirements

---

## ?? Problem Statement

Pre-Phase 3, the AdminPortal lacked visibility into user activities and system changes:

### Critical Issues

**Issue 1: No Audit Trail**
- No record of who performed what actions
- Impossible to investigate security incidents
- No compliance trail for regulatory requirements

**Issue 2: No Activity Monitoring**
- Admins couldn't see user login patterns
- No visibility into role changes
- Couldn't track booking/quote modifications

**Issue 3: No Compliance Reporting**
- No way to generate compliance reports
- Couldn't demonstrate audit trail to auditors
- No export functionality for external analysis

**Issue 4: Debugging Challenges**
- Troubleshooting required log file analysis
- No user-friendly interface for investigating issues
- Time-consuming manual log correlation

---

## ? Solution: Audit Log Viewer

### Features Delivered (Phase 3)

**1. Comprehensive Filtering**
- ? Date range (default: last 30 days)
- ? Action type (Booking.Created, User.RoleChanged, etc.)
- ? Entity type (Booking, Quote, User, Affiliate, Driver)
- ? User filter (by userId or username)

**2. Pagination**
- ? Skip/take pagination (100 records per page)
- ? Total count display
- ? Page navigation (Previous/Next + page numbers)

**3. CSV Export**
- ? Export all filtered results
- ? Includes all audit log fields
- ? Auto-generated filename with timestamp

**4. Admin-Only Access**
- ? `[Authorize(Roles = "admin")]` attribute
- ? 403 Forbidden for non-admin users
- ? User-friendly access denied messages

**5. Professional UX**
- ? Toast notifications for success/error
- ? Loading spinners during API calls
- ? Empty state message when no logs found
- ? Responsive Bootstrap 5 design

---

## ??? Architecture

### Components

```
Audit Logging System
??? Models/AuditLogModels.cs
?   ??? AuditLogEntry
?   ??? AuditLogQuery
?   ??? AuditLogPagination
?   ??? AuditLogFilters
?   ??? AuditLogResponse
??? Services/IAuditLogService.cs
??? Services/AuditLogService.cs
??? Components/Pages/Admin/AuditLogs.razor
??? wwwroot/js/utils.js (CSV download)
```

### Data Flow

```
???????????????????????????????????????????????????????????????
? 1. Admin User                                                ?
?    ??> Navigates to /admin/audit-logs                       ?
???????????????????????????????????????????????????????????????
                           ?
???????????????????????????????????????????????????????????????
? 2. AuditLogs.razor                                           ?
?    ??> Sets default filters (last 30 days)                  ?
?    ??> Calls LoadLogsAsync()                                ?
???????????????????????????????????????????????????????????????
                           ?
???????????????????????????????????????????????????????????????
? 3. AuditLogService                                           ?
?    ??> Builds query string from filters                     ?
?    ??> Adds JWT token + API key headers                     ?
?    ??> GET /api/admin/audit-logs?skip=0&take=100...         ?
???????????????????????????????????????????????????????????????
                           ?
???????????????????????????????????????????????????????????????
? 4. AdminAPI                                                  ?
?    ??> Validates JWT (admin role required)                  ?
?    ??> Queries audit logs from database/storage             ?
?    ??> Returns paginated response                           ?
???????????????????????????????????????????????????????????????
                           ?
???????????????????????????????????????????????????????????????
? 5. AuditLogs.razor                                           ?
?    ??> Displays logs in table                               ?
?    ??> Shows pagination controls                            ?
?    ??> Enables CSV export                                   ?
???????????????????????????????????????????????????????????????
```

---

## ?? Audit Log Data Model

### AuditLogEntry Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Id` | string | Unique log entry ID | "audit-001" |
| `Timestamp` | DateTime | When action occurred (UTC) | "2026-01-19T15:30:00Z" |
| `UserId` | string | User ID (GUID) | "a1b2c3d4-..." |
| `Username` | string | Username | "alice" |
| `UserRole` | string | User's role at time of action | "admin" |
| `Action` | string | Action type | "Booking.Created" |
| `EntityType` | string | Entity affected | "Booking" |
| `EntityId` | string? | Entity ID (null for system actions) | "booking-xyz" |
| `IpAddress` | string | User's IP address | "192.168.1.100" |
| `HttpMethod` | string | HTTP method | "POST" |
| `EndpointPath` | string | API endpoint | "/bookings" |
| `Result` | string | Action result | "Success" |
| `Details` | string? | Human-readable details | "Status changed from Requested to Confirmed" |

---

## ?? Filtering Capabilities

### Date Range Filter

**Default**: Last 30 days

**Usage**:
```
Start Date: 01/01/2026
End Date: 01/31/2026
```

**Query**:
```
?startDate=2026-01-01T00:00:00Z&endDate=2026-01-31T23:59:59Z
```

**Use Cases**:
- Monthly compliance reports
- Incident investigation (specific day)
- Trend analysis over time

---

### Action Type Filter

**Options**:
- `Booking.Created` - New booking created
- `Booking.Updated` - Booking modified
- `User.RoleChanged` - User role assignment changed
- `Quote.Priced` - Quote received pricing
- `Driver.Assigned` - Driver assigned to booking
- Plus many more...

**Example**: Find all role changes in the last month
```
Action: User.RoleChanged
Date Range: Last 30 days
```

---

### Entity Type Filter

**Options**:
- `Booking` - Booking-related actions
- `Quote` - Quote-related actions
- `User` - User management actions
- `Affiliate` - Affiliate management actions
- `Driver` - Driver management actions
- `System` - System-level events

**Example**: Find all user management activities
```
Entity Type: User
Date Range: Last 7 days
```

---

### User Filter

**Accepts**:
- Username (e.g., "alice")
- User ID (GUID)

**Example**: Find all actions by a specific user
```
User: alice
Date Range: All time
```

**Use Cases**:
- Investigate specific user's activities
- Verify user performed required actions
- Audit administrator actions

---

## ?? CSV Export

### Features

**Export Button**: Top-right of page

**File Format**: CSV with all fields

**Filename**: Auto-generated with timestamp
```
audit-logs-2026-01-19-143052.csv
```

**Fields Exported**:
1. Timestamp
2. Username
3. User Role
4. Action
5. HTTP Method
6. Endpoint
7. Entity Type
8. Entity ID
9. Result
10. IP Address
11. Details

**Use Cases**:
- Monthly compliance reports
- Security incident investigation
- External audit trail
- Data analysis in Excel/PowerBI

**Limitations**:
- Maximum 10,000 records per export
- Respects current filters
- Downloads immediately (no email)

---

## ?? User Interface

### Page Layout

```
???????????????????????????????????????????????????????????????
? Audit Logs                                   [Export to CSV] ?
???????????????????????????????????????????????????????????????
? Filters:                                                     ?
? Start Date: [01/01/2026]  End Date: [01/31/2026]           ?
? Action: [All Actions ?]  Entity: [All Entities ?]          ?
? User: [Username or ID]                                      ?
? [Apply Filters] [Clear]                                     ?
???????????????????????????????????????????????????????????????
? Showing 100 of 1,234 logs (Page 1 of 13)                   ?
???????????????????????????????????????????????????????????????
? Timestamp       ? User   ? Action     ? Entity ? Details    ?
? 01/19 15:30:15 ? alice  ? Booking.Cr ? Book.  ? New book.  ?
? 01/19 15:25:10 ? diana  ? User.RoleC ? User   ? Role chang ?
???????????????????????????????????????????????????????????????
? [Previous] [1] [2] [3] ... [13] [Next]                     ?
???????????????????????????????????????????????????????????????
```

### Color-Coded Badges

**Actions**:
- Created ? Green
- Updated ? Blue
- Deleted ? Red
- Login ? Gray
- Priced ? Yellow
- Assigned ? Cyan

**Results**:
- Success ? Green
- Failed ? Red
- Unauthorized ? Yellow

---

## ?? Security & Access Control

### Authorization

**Page-Level**:
```csharp
@attribute [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
```

**Service-Level**:
```csharp
if (response.StatusCode == HttpStatusCode.Forbidden)
{
    throw new UnauthorizedAccessException("Access denied. Admin role required.");
}
```

**Result**: Only admin users can access audit logs

### 403 Forbidden Handling

**Dispatcher Access**:
```
Request: GET /admin/audit-logs (as diana)
Result: 403 Forbidden
Message: "Access denied. You do not have permission to view audit logs. Admin role required."
UI: Toast error notification + inline error message
```

### Data Privacy

**Sensitive Fields**:
- IP addresses logged (required for security)
- User IDs tracked (required for audit)
- No passwords or payment details logged

**Retention**: Controlled by AdminAPI (not portal)

---

## ?? Testing Procedures

### Manual Testing

**Prerequisites**:
- ? AdminAPI running
- ? AdminAPI has audit logs in database
- ? AuthServer provides admin JWT tokens

**Test Scenarios**:

**1. Load Default View**
```
Steps:
1. Login as alice (admin)
2. Navigate to Admin ? Audit Logs
3. Verify logs load (last 30 days)
4. Verify pagination shows (if > 100 logs)

Expected: Logs display in table, paginated if needed
```

**2. Filter by Action**
```
Steps:
1. Select "User.RoleChanged" from Action dropdown
2. Click "Apply Filters"
3. Verify only role change actions shown

Expected: Table shows only User.RoleChanged entries
```

**3. Filter by Date Range**
```
Steps:
1. Set Start Date: 01/15/2026
2. Set End Date: 01/19/2026
3. Click "Apply Filters"

Expected: Only logs from 01/15-01/19 shown
```

**4. Export to CSV**
```
Steps:
1. Apply desired filters
2. Click "Export to CSV"
3. Verify file downloads

Expected: CSV file downloads with filtered results
```

**5. Dispatcher Denied Access**
```
Steps:
1. Logout alice
2. Login as diana (dispatcher)
3. Try to navigate to /admin/audit-logs

Expected:
- 403 error or redirect
- Toast error: "Access denied. Admin role required."
- Cannot access audit logs
```

**6. Pagination**
```
Steps:
1. Ensure > 100 logs exist
2. Verify first 100 shown
3. Click "Next"
4. Verify next 100 shown

Expected: Pagination works correctly
```

**7. Empty State**
```
Steps:
1. Set date range with no logs (e.g., year 2020)
2. Click "Apply Filters"

Expected: Message "No audit logs found for the selected filters"
```

**8. Clear Filters**
```
Steps:
1. Apply various filters
2. Click "Clear" button
3. Verify filters reset to defaults

Expected: Filters reset to last 30 days, all actions
```

---

## ?? Common Use Cases

### Monthly Compliance Report

**Scenario**: Generate report for auditors

**Steps**:
1. Set date range: First to last day of month
2. Leave all other filters blank (all actions)
3. Click "Export to CSV"
4. Send CSV to auditor

**Example**:
```
Start Date: 01/01/2026
End Date: 01/31/2026
Action: (All)
Entity: (All)
? Export to CSV
? Share audit-logs-2026-01-31.csv
```

---

### Security Incident Investigation

**Scenario**: Investigate unauthorized access attempt

**Steps**:
1. Set date range: Day of incident
2. Filter by User: suspected user
3. Review all actions
4. Export for evidence

**Example**:
```
Start Date: 01/18/2026
End Date: 01/18/2026
User: suspect-user-id
? Review login attempts, failed actions
? Export for security team
```

---

### User Activity Audit

**Scenario**: Verify user performed required actions

**Steps**:
1. Filter by User: target user
2. Filter by Action: specific action type
3. Verify timestamp and details

**Example**:
```
User: alice
Action: User.RoleChanged
? Verify alice changed roles as required
? Export for compliance documentation
```

---

### Trend Analysis

**Scenario**: Analyze booking creation patterns

**Steps**:
1. Set date range: Last 90 days
2. Filter by Action: Booking.Created
3. Export to CSV
4. Analyze in Excel/PowerBI

**Example**:
```
Start Date: 3 months ago
End Date: Today
Action: Booking.Created
Entity: Booking
? Export
? Analyze peak booking times, users creating most bookings
```

---

## ?? Future Enhancements

**Planned (Post-Alpha)**:

**1. Real-Time Notifications**
- Alert admins of critical actions
- Email digest of daily activities

**2. Advanced Filtering**
- Result filter (Success/Failed)
- IP address filter
- Multiple action types (OR logic)

**3. Audit Log Detail View**
- Click log entry to see full details
- View before/after values
- Related entity links

**4. Retention Policies**
- Configure auto-archival
- Compliance-driven retention rules

**5. Analytics Dashboard**
- Visual charts of activity trends
- Top users by activity
- Action type distribution

---

## ?? Related Documentation

- [User Access Control](13-User-Access-Control.md) - RBAC implementation
- [Security Model](23-Security-Model.md) - Authentication & authorization
- [Testing Guide](02-Testing-Guide.md) - Test procedures
- [API Reference](20-API-Reference.md) - AdminAPI endpoints
- [Troubleshooting](32-Troubleshooting.md) - Common issues

---

## ?? Troubleshooting

### Issue 1: "No audit logs found"

**Symptom**: Empty state shown despite logs existing

**Possible Causes**:
1. Date range too narrow
2. Filters too restrictive
3. No logs match criteria

**Solutions**:
1. Widen date range (try last 90 days)
2. Click "Clear" to reset filters
3. Verify logs exist in AdminAPI database

---

### Issue 2: CSV Export Fails

**Symptom**: Error message on export

**Possible Causes**:
1. Too many logs (> 10,000)
2. Network timeout
3. Browser popup blocker

**Solutions**:
1. Narrow date range to reduce record count
2. Retry export
3. Allow popups for portal domain

---

### Issue 3: "Access Denied" Error

**Symptom**: 403 error when accessing audit logs

**Possible Causes**:
1. User is not admin
2. JWT token expired
3. JWT missing admin role claim

**Solutions**:
1. Verify user has admin role
2. Re-login to refresh token
3. Contact admin for role assignment

---

## ?? Support

**For Questions**:
- Check [Troubleshooting](32-Troubleshooting.md)
- Review [Testing Guide](02-Testing-Guide.md)
- Contact development team

**For Compliance**:
- Export audit logs to CSV
- Provide to compliance officer
- Demonstrate audit trail capability

---

**Last Updated**: January 19, 2026  
**Status**: ? Production Ready (Phase 3 Complete)  
**Version**: 1.0

---

*The Audit Logging system provides enterprise-grade visibility into all system activities, enabling compliance, security monitoring, and troubleshooting. The comprehensive filtering and CSV export capabilities make it easy to generate reports and investigate incidents.* ?
