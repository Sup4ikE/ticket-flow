using MediatR;
using TicketFlow.Booking.Domain.Enums;

namespace TicketFlow.Booking.Application.Commands;

public record PayBookingCommand(Guid BookingId) : IRequest<BookingStatus>;
