using TicketFlow.Events.Application.Abstractions;

namespace TicketFlow.Events.Infrastructure.Persistence;

public class UnitOfWork(EventsDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}