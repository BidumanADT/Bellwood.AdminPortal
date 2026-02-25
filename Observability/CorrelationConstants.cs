namespace Bellwood.AdminPortal.Observability;

public static class CorrelationConstants
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "CorrelationId";
}
