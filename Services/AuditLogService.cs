using Bellwood.AdminPortal.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Bellwood.AdminPortal.Services;

/// <summary>
/// Service for querying audit logs from AdminAPI.
/// Phase 3: Admin-only audit log viewer implementation.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IAuthTokenProvider _tokenProvider;
    private readonly IAdminApiKeyProvider _apiKeyProvider;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IHttpClientFactory httpFactory,
        IAuthTokenProvider tokenProvider,
        IAdminApiKeyProvider apiKeyProvider,
        ILogger<AuditLogService> logger)
    {
        _httpFactory = httpFactory;
        _tokenProvider = tokenProvider;
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets an HTTP client with authorization headers.
    /// </summary>
    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var client = _httpFactory.CreateClient("AdminAPI");

        // Add API key
        var apiKey = _apiKeyProvider.GetApiKey();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-ApiKey", apiKey);

        // Add JWT token
        var token = await _tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    /// <summary>
    /// Queries audit logs with optional filtering and pagination.
    /// </summary>
    public async Task<AuditLogResponse> GetAuditLogsAsync(AuditLogQuery query)
    {
        _logger.LogInformation("[AuditLog] Querying audit logs - Skip: {Skip}, Take: {Take}", 
            query.Skip, query.Take);

        try
        {
            var client = await GetAuthorizedClientAsync();

            // Build query string matching AdminAPI specification
            var queryParams = new List<string>();

            if (query.StartDate.HasValue)
                queryParams.Add($"startDate={query.StartDate.Value:yyyy-MM-ddTHH:mm:ss}Z");

            if (query.EndDate.HasValue)
                queryParams.Add($"endDate={query.EndDate.Value:yyyy-MM-ddTHH:mm:ss}Z");

            if (!string.IsNullOrEmpty(query.Action))
                queryParams.Add($"action={Uri.EscapeDataString(query.Action)}");

            if (!string.IsNullOrEmpty(query.UserId))
                queryParams.Add($"userId={Uri.EscapeDataString(query.UserId)}");

            if (!string.IsNullOrEmpty(query.EntityType))
                queryParams.Add($"entityType={Uri.EscapeDataString(query.EntityType)}");

            queryParams.Add($"skip={query.Skip}");
            queryParams.Add($"take={query.Take}");

            var queryString = string.Join("&", queryParams);
            var url = $"/api/admin/audit-logs?{queryString}";

            _logger.LogDebug("[AuditLog] Request URL: {Url}", url);

            var response = await client.GetAsync(url);

            // Phase 3: Handle 403 Forbidden
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("[AuditLog] Access denied (403 Forbidden)");
                throw new UnauthorizedAccessException("Access denied. You do not have permission to view audit logs. Admin role required.");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AuditLogResponse>();

            if (result == null)
            {
                _logger.LogWarning("[AuditLog] Null response from API");
                return new AuditLogResponse 
                { 
                    Pagination = new AuditLogPagination 
                    { 
                        Skip = query.Skip, 
                        Take = query.Take 
                    } 
                };
            }

            _logger.LogInformation("[AuditLog] Retrieved {Count} logs (Total: {Total}, Page: {Page})",
                result.Logs.Count, result.Pagination.Total, result.CurrentPage);

            return result;
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[AuditLog] HTTP request failed");
            throw new Exception($"Failed to retrieve audit logs: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLog] Unexpected error");
            throw new Exception($"An error occurred while retrieving audit logs: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Exports audit logs to CSV format.
    /// Phase 3: Export functionality for audit log analysis.
    /// </summary>
    public async Task<string> ExportAuditLogsToCsvAsync(AuditLogQuery query)
    {
        _logger.LogInformation("[AuditLog] Exporting audit logs to CSV");

        try
        {
            // Get all logs (ignore pagination for export)
            var exportQuery = new AuditLogQuery
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                Action = query.Action,
                EntityType = query.EntityType,
                UserId = query.UserId,
                Skip = 0,
                Take = 10000 // Large page size to get all results
            };

            var result = await GetAuditLogsAsync(exportQuery);

            // Build CSV
            var csv = new StringBuilder();

            // Header
            csv.AppendLine("Timestamp,Username,User Role,Action,HTTP Method,Endpoint,Entity Type,Entity ID,Result,IP Address,Details");

            // Rows
            foreach (var log in result.Logs)
            {
                csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"\"{EscapeCsv(log.Username)}\"," +
                              $"\"{EscapeCsv(log.UserRole)}\"," +
                              $"\"{EscapeCsv(log.Action)}\"," +
                              $"\"{EscapeCsv(log.HttpMethod)}\"," +
                              $"\"{EscapeCsv(log.EndpointPath)}\"," +
                              $"\"{EscapeCsv(log.EntityType)}\"," +
                              $"\"{EscapeCsv(log.EntityId ?? "")}\"," +
                              $"\"{EscapeCsv(log.Result)}\"," +
                              $"\"{EscapeCsv(log.IpAddress)}\"," +
                              $"\"{EscapeCsv(log.Details ?? "")}\"");
            }

            _logger.LogInformation("[AuditLog] Exported {Count} logs to CSV", result.Logs.Count);

            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLog] CSV export failed");
            throw new Exception($"Failed to export audit logs: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Escapes special characters for CSV format.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape quotes by doubling them
        return value.Replace("\"", "\"\"");
    }
}
