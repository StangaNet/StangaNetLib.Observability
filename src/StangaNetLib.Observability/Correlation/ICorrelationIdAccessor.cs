namespace StangaNetLib.Observability.Correlation;

/// <summary>
/// Provides access to the current request's correlation ID.
/// Inject this service in use cases or services that need to include the correlation ID
/// in outgoing HTTP calls, audit logs, or external service requests.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// The correlation ID for the current HTTP request.
    /// Returns null if called outside of an HTTP request context.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Scoped implementation that stores the correlation ID for the duration of a single request.
/// </summary>
internal sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? CorrelationId { get; set; }
}
