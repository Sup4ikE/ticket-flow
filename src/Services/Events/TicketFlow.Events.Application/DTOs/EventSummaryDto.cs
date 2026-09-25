namespace TicketFlow.Events.Application.DTOs;

// Catalog entry for the (cached) published-events list. Deliberately has no AvailableSeats: that number changes on
// every reservation/release/expiry and the cache is only invalidated on Create/Publish, so it would go stale.
// The live seat count comes from GET /api/events/{id} (EventDto), which is never cached.
public record EventSummaryDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string Venue,
    int Capacity,
    decimal Price);
