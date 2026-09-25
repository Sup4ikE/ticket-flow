using System.Text.Json;
using MediatR;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Domain.Entities;
using TicketFlow.Booking.Domain.Enums;
using TicketFlow.Contracts.Events;

namespace TicketFlow.Booking.Application.Commands;

public class PayBookingCommandHandler(
    IBookingRepository bookingRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<PayBookingCommand, BookingStatus>
{
    public async Task<BookingStatus> Handle(PayBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Booking {request.BookingId} not found.");

        // Throws DomainException (mapped to 409 by the controller) unless AwaitingPayment - including
        // when the hold already expired and ReservationExpiryWorker moved this booking to Cancelled.
        booking.Pay();

        outboxRepository.Add(OutboxMessage.Create(
            nameof(BookingConfirmed),
            JsonSerializer.Serialize(new BookingConfirmed
            {
                BookingId = booking.Id,
                // Pay() only succeeds from AwaitingPayment, which MarkAwaitingPayment always sets
                // ReservationId for - guaranteed non-null here by that domain invariant.
                ReservationId = booking.ReservationId!.Value,
                UserEmail = booking.UserEmail,
                EventTitle = booking.EventTitle,
                EventStartsAt = booking.EventStartsAt,
                Quantity = booking.Quantity,
                TotalPrice = booking.TotalPrice
            })));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.Status;
    }
}
