using MediatR;
using TicketFlow.Booking.Domain.Enums;

namespace TicketFlow.Booking.Application.Commands;

public record CancelBookingCommand(Guid BookingId) : IRequest<BookingStatus>;
