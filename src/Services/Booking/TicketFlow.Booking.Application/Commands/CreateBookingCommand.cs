using MediatR;

namespace TicketFlow.Booking.Application.Commands;

// Event title, start time and price are deliberately NOT part of the request: they are looked up from the Events
// service so a client can't book at a price of its own choosing.
public record CreateBookingCommand(
    Guid EventId,
    string UserEmail,
    int Quantity) : IRequest<Guid>;
