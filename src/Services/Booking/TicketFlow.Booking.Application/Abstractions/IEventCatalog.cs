namespace TicketFlow.Booking.Application.Abstractions;

public interface IEventCatalog
{
    /// <summary>Returns the event, or null if Events doesn't know it.</summary>
    /// <exception cref="Exceptions.EventsServiceUnavailableException">Events can't be reached or answered with an error.</exception>
    Task<EventInfo?> GetEventAsync(Guid eventId, CancellationToken ct = default);
}

public record EventInfo(Guid Id, string Title, DateTime StartsAt, decimal Price);
