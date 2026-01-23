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
}
