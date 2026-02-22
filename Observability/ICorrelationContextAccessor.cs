namespace Bellwood.AdminPortal.Observability;

public interface ICorrelationContextAccessor
{
    string? CorrelationId { get; set; }
}
