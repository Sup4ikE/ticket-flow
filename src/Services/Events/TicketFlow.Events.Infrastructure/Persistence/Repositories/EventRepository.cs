using Microsoft.EntityFrameworkCore;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Enums;

namespace TicketFlow.Events.Infrastructure.Persistence.Repositories;

public class EventRepository(EventsDbContext context) : IEventRepository
{
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<Event>> GetPublishedAsync(CancellationToken ct = default) =>
        context.Events
            .Where(e => e.Status == EventStatus.Published)
            .OrderBy(e => e.StartsAt)
            .ToListAsync(ct);

    public void Add(Event @event) => context.Events.Add(@event);
}