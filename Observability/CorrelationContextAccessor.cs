namespace Bellwood.AdminPortal.Observability;

public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<string?> CorrelationHolder = new();

    public string? CorrelationId
    {
        get => CorrelationHolder.Value;
        set => CorrelationHolder.Value = value;
    }
}
