using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Domain.Entities;

namespace TicketFlow.Booking.Infrastructure.Persistence.Repositories;

public class OutboxRepository(BookingDbContext context): IOutboxRepository
{
    public void Add(OutboxMessage message) => context.OutboxMessages.Add(message);
}