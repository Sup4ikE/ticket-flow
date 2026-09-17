namespace TicketFlow.Events.Application.DTOs;

public record CreateEventRequest(
    string Title,
    string Description,
    DateTime StartsAt,
    string Venue,
    int Capacity,
    decimal Price);