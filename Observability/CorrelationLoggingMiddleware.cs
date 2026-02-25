using System.Diagnostics;
using Serilog.Context;

namespace Bellwood.AdminPortal.Observability;

public class CorrelationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationLoggingMiddleware> _logger;

    public CorrelationLoggingMiddleware(
        RequestDelegate next,
        ILogger<CorrelationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContextAccessor correlationContext)
    {
        var correlationId = ResolveCorrelationId(context);
        correlationContext.CorrelationId = correlationId;
        context.Items[CorrelationConstants.HttpContextItemKey] = correlationId;
        context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;

        using var correlationScope = LogContext.PushProperty("correlationId", correlationId);
        using var requestPathScope = LogContext.PushProperty("requestPath", context.Request.Path.Value ?? string.Empty);
        using var methodScope = LogContext.PushProperty("method", context.Request.Method);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);

            stopwatch.Stop();
            _logger.LogInformation(
                "Portal request completed with status {statusCode} in {elapsedMs}ms",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorId = Guid.NewGuid().ToString("N");

            _logger.LogError(
                ex,
                "Portal request failed with errorId {errorId} after {elapsedMs}ms",
                errorId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationConstants.HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString();
    }
}
