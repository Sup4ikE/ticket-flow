using System.Text.Json;
using MediatR;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Domain.Entities;
using TicketFlow.Booking.Domain.Enums;
using TicketFlow.Contracts.Events;

namespace TicketFlow.Booking.Application.Commands;

public class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelBookingCommand, BookingStatus>
{
    public async Task<BookingStatus> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking {request.BookingId} not found.");

        var reservationId = booking.ReservationId;

        booking.Cancel();

        // Only a booking that already got SeatsReserved holds seats that need releasing on the
        // Events side; a still-Pending booking never reserved anything there in the first place.
        if (reservationId is not null)
        {
            outboxRepository.Add(OutboxMessage.Create(
                nameof(SeatsReleaseRequested),
                JsonSerializer.Serialize(new SeatsReleaseRequested
                {
                    BookingId = booking.Id,
                    ReservationId = reservationId.Value
                })));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.Status;
    }
}
