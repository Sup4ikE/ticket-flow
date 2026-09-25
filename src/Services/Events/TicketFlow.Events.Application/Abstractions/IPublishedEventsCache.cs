using TicketFlow.Events.Application.DTOs;

namespace TicketFlow.Events.Application.Abstractions;

// Cache-aside store for the published-events list. Implementations must never throw on cache failures:
// a miss (null) or a no-op is the fallback, the database stays the source of truth.
public interface IPublishedEventsCache
{
    Task<List<EventSummaryDto>?> GetAsync(CancellationToken ct = default);
    Task SetAsync(List<EventSummaryDto> events, CancellationToken ct = default);
    Task InvalidateAsync(CancellationToken ct = default);
}
