using System.Diagnostics;
using Serilog.Context;

namespace Bellwood.AdminPortal.Observability;

public class OutboundHttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<OutboundHttpLoggingHandler> _logger;
    private readonly ICorrelationContextAccessor _correlationContext;
    private readonly string _outboundService;

    public OutboundHttpLoggingHandler(
        ILogger<OutboundHttpLoggingHandler> logger,
        ICorrelationContextAccessor correlationContext,
        string outboundService)
    {
        _logger = logger;
        _correlationContext = correlationContext;
        _outboundService = outboundService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var outboundService = _outboundService;
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var method = request.Method.Method;
        var correlationId = _correlationContext.CorrelationId ?? string.Empty;

        using var serviceScope = LogContext.PushProperty("outboundService", outboundService);
        using var pathScope = LogContext.PushProperty("path", path);
        using var methodScope = LogContext.PushProperty("method", method);
        using var correlationScope = LogContext.PushProperty("correlationId", correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Outbound HTTP call completed with status {statusCode} in {elapsedMs}ms",
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Outbound HTTP call failed after {elapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
