using System.Text.Json;
using MediatR;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Domain.Entities;
using TicketFlow.Contracts.Events;

namespace TicketFlow.Booking.Application.Commands;

public class CreateBookingCommandHandler(
    IBookingRepository bookingRepository,
    IOutboxRepository outboxRepository,
    IEventCatalog eventCatalog,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBookingCommand, Guid>
{
    public async Task<Guid> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var eventInfo = await eventCatalog.GetEventAsync(request.EventId, cancellationToken)
            ?? throw new KeyNotFoundException($"Event {request.EventId} not found.");

        var booking = Domain.Entities.Booking.Create(
            eventInfo.Id,
            request.UserEmail,
            request.Quantity,
            eventInfo.Title,
            eventInfo.StartsAt,
            eventInfo.Price);

        var bookingCreatedEvent = new BookingCreated
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            Quantity = booking.Quantity
        };

        var outboxMessage = OutboxMessage.Create(
            type: nameof(BookingCreated),
            content: JsonSerializer.Serialize(bookingCreatedEvent));

        bookingRepository.Add(booking);
        outboxRepository.Add(outboxMessage);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.Id;
    }
}