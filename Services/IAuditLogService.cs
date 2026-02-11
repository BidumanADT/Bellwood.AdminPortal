using Bellwood.AdminPortal.Models;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Service for querying audit logs from AdminAPI.
/// Phase 3: Audit log viewer functionality.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Queries audit logs with optional filtering and pagination.
    /// </summary>
    /// <param name="query">Query parameters for filtering logs.</param>
    /// <returns>Paginated audit log response.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin permissions (403 Forbidden).</exception>
    Task<AuditLogResponse> GetAuditLogsAsync(AuditLogQuery query);

    /// <summary>
    /// Exports audit logs to CSV format.
    /// </summary>
    /// <param name="query">Query parameters for filtering logs (pagination ignored for export).</param>
    /// <returns>CSV content as string.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin permissions (403 Forbidden).</exception>
    Task<string> ExportAuditLogsToCsvAsync(AuditLogQuery query);

    /// <summary>
    /// Gets statistics about audit logs (total count, date range, etc.).
    /// </summary>
    /// <returns>Audit log statistics.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin permissions (403 Forbidden).</exception>
    Task<AuditLogStats> GetAuditLogStatsAsync();

    /// <summary>
    /// Clears all audit logs from the system.
    /// DESTRUCTIVE ACTION - Requires admin role and typed confirmation.
    /// </summary>
    /// <returns>Result indicating success and number of logs deleted.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user lacks admin permissions (403 Forbidden).</exception>
    Task<AuditLogClearResult> ClearAuditLogsAsync();
}
