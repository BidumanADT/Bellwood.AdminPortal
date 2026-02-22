namespace Bellwood.AdminPortal.Observability;

public class CorrelationIdPropagationHandler : DelegatingHandler
{
    private readonly ICorrelationContextAccessor _correlationContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdPropagationHandler(
        ICorrelationContextAccessor correlationContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _correlationContext = correlationContext;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _correlationContext.CorrelationId
            ?? _httpContextAccessor.HttpContext?.Items[CorrelationConstants.HttpContextItemKey]?.ToString()
            ?? _httpContextAccessor.HttpContext?.Request.Headers[CorrelationConstants.HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            _correlationContext.CorrelationId = correlationId;
        }

        request.Headers.Remove(CorrelationConstants.HeaderName);
        request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
