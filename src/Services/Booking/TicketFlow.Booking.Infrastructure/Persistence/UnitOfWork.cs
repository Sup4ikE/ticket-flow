using TicketFlow.Booking.Application.Abstractions;

namespace TicketFlow.Booking.Infrastructure.Persistence;

public class UnitOfWork(BookingDbContext context): IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}