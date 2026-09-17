using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Event>> GetPublishedAsync(CancellationToken ct = default);
    void Add(Event @event);
}