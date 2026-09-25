using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Application.Mapping;

public static class EventMappingExtensions
{
    public static EventDto ToDto(this Event @event) =>
        new(
            @event.Id,
            @event.Title,
            @event.Description,
            @event.StartsAt,
            @event.Venue,
            @event.Capacity,
            @event.AvailableSeats,
            @event.Price);

    public static EventSummaryDto ToSummaryDto(this Event @event) =>
        new(
            @event.Id,
            @event.Title,
            @event.Description,
            @event.StartsAt,
            @event.Venue,
            @event.Capacity,
            @event.Price);
}