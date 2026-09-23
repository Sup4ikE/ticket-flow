using MediatR;

namespace TicketFlow.Booking.Application.Commands;

public record CreateBookingCommand(
    Guid EventId,
    string UserEmail,
    int Quantity,
    string EventTitle,
    DateTime EventStartsAt,
    decimal PricePerTicket) : IRequest<Guid>;