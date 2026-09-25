using TicketFlow.Events.Application.DTOs;

namespace TicketFlow.Events.Application.Services;

public interface IEventService
{
    Task<List<EventSummaryDto>> GetPublishedEventsAsync(CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventDto> CreateAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<bool> PublishAsync(Guid id, CancellationToken ct = default);
}