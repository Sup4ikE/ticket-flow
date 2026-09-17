namespace TicketFlow.Events.Application.DTOs;

public record EventDto(
    Guid Id,
    string Title,
    string Description,
    DateTime StartsAt,
    string Venue,
    int Capacity,
    int AvailableSeats,
    decimal Price);