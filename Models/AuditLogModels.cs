namespace Bellwood.AdminPortal.Models;

/// <summary>
/// Represents a single audit log entry from the system.
/// Phase 3: Audit logging for admin transparency.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID (GUID) of the user who performed the action.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the user who performed the action.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's role at the time of the action.
    /// </summary>
    public string UserRole { get; set; } = string.Empty;

    /// <summary>
    /// Type of action performed.
    /// Examples: Booking.Created, User.RoleChanged, Quote.Priced
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected by the action.
    /// Examples: Booking, Quote, User, Affiliate, Driver
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the entity affected.
    /// Null for system-level actions (e.g., Login).
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// IP address of the user who performed the action.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE).
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint path accessed.
    /// </summary>
    public string EndpointPath { get; set; } = string.Empty;

    /// <summary>
    /// Result of the action (Success, Failed, Unauthorized, etc.).
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable details about the action (optional).
    /// Examples: "Status changed from Requested to Confirmed", "Role changed from driver to dispatcher"
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Query parameters for filtering audit logs.
/// Phase 3: Comprehensive filtering for audit log viewer.
/// </summary>
public sealed class AuditLogQuery
{
    /// <summary>
    /// Start date for log query (inclusive). UTC.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for log query (inclusive). UTC.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by action type.
    /// Examples: Booking.Created, User.RoleChanged, Quote.Priced
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by user ID (GUID).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// Examples: Booking, Quote, User, Affiliate, Driver
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Number of records to skip (for pagination).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of records to take (max 1000).
    /// </summary>
    public int Take { get; set; } = 100;
}

/// <summary>
/// Pagination metadata from AdminAPI.
/// </summary>
public sealed class AuditLogPagination
{
    /// <summary>
    /// Total number of log entries matching the query.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Number of records skipped.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Number of records requested.
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Number of records returned in this response.
    /// </summary>
    public int Returned { get; set; }
}

/// <summary>
/// Applied filters from the query.
/// </summary>
public sealed class AuditLogFilters
{
    public string? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Response containing audit logs from AdminAPI.
/// Phase 3: Matches AdminAPI response structure.
/// </summary>
public sealed class AuditLogResponse
{
    /// <summary>
    /// List of audit log entries.
    /// </summary>
    public List<AuditLogEntry> Logs { get; set; } = new();

    /// <summary>
    /// Pagination metadata.
    /// </summary>
    public AuditLogPagination Pagination { get; set; } = new();

    /// <summary>
    /// Filters that were applied to the query.
    /// </summary>
    public AuditLogFilters? Filters { get; set; }

    // Helper properties for UI
    public int TotalPages => Pagination.Take > 0 ? (int)Math.Ceiling(Pagination.Total / (double)Pagination.Take) : 0;
    public int CurrentPage => Pagination.Take > 0 ? (Pagination.Skip / Pagination.Take) + 1 : 1;
    public bool HasPreviousPage => Pagination.Skip > 0;
    public bool HasNextPage => (Pagination.Skip + Pagination.Returned) < Pagination.Total;
}

/// <summary>
/// Statistics about audit logs in the system.
/// Phase 3: For audit log management and monitoring.
/// </summary>
public sealed class AuditLogStats
{
    /// <summary>
    /// Total number of audit log entries in the system.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Timestamp of the oldest audit log entry (UTC).
    /// </summary>
    public DateTime? OldestEntry { get; set; }

    /// <summary>
    /// Timestamp of the most recent audit log entry (UTC).
    /// </summary>
    public DateTime? NewestEntry { get; set; }

    /// <summary>
    /// Approximate storage size in bytes (optional).
    /// </summary>
    public long? StorageSizeBytes { get; set; }
}

/// <summary>
/// Result of clearing audit logs.
/// Phase 3: Confirmation of destructive operation.
/// </summary>
public sealed class AuditLogClearResult
{
    /// <summary>
    /// Whether the clear operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of audit log entries that were deleted.
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Predefined action types for audit logging.
/// Phase 3: Common action patterns.
/// </summary>
public static class AuditAction
{
    public const string BookingCreated = "Booking.Created";
    public const string BookingUpdated = "Booking.Updated";
    public const string BookingDeleted = "Booking.Deleted";
    public const string QuoteCreated = "Quote.Created";
    public const string QuotePriced = "Quote.Priced";
    public const string QuoteRejected = "Quote.Rejected";
    public const string UserCreated = "User.Created";
    public const string UserRoleChanged = "User.RoleChanged";
    public const string UserLogin = "User.Login";
    public const string UserLogout = "User.Logout";
    public const string DriverAssigned = "Driver.Assigned";
    public const string AffiliateCreated = "Affiliate.Created";
}

/// <summary>
/// Predefined entity types for audit logging.
/// Phase 3: Standard entity types for consistency.
/// </summary>
public static class AuditEntityType
{
    public const string Booking = "Booking";
    public const string Quote = "Quote";
    public const string User = "User";
    public const string Affiliate = "Affiliate";
    public const string Driver = "Driver";
    public const string System = "System";
}
