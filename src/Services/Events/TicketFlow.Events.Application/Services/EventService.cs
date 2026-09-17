using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Application.Mapping;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Application.Services;

public class EventService(IEventRepository repository, IUnitOfWork unitOfWork) : IEventService
{
    public async Task<List<EventDto>> GetPublishedEventsAsync(CancellationToken ct = default)
    {
        var events = await repository.GetPublishedAsync(ct);
        return events.Select(e => e.ToDto()).ToList();
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await repository.GetByIdAsync(id, ct);
        return @event?.ToDto();
    }

    public async Task<EventDto> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var @event = Event.Create(
            request.Title,
            request.Description,
            request.StartsAt,
            request.Venue,
            request.Capacity,
            request.Price);

        repository.Add(@event);
        await unitOfWork.SaveChangesAsync(ct);

        return @event.ToDto();
    }

    public async Task<bool> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var @event = await repository.GetByIdAsync(id, ct);
        if (@event is null)
            return false;

        @event.Publish();
        await unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}