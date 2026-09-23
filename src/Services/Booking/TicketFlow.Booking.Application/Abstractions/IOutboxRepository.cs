namespace TicketFlow.Booking.Application.Abstractions;

public interface IOutboxRepository
{
    void Add(Domain.Entities.OutboxMessage message);
}