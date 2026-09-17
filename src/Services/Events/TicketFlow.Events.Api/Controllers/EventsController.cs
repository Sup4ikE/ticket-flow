using Microsoft.AspNetCore.Mvc;
using TicketFlow.Events.Application.DTOs;
using TicketFlow.Events.Application.Services;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EventDto>>> GetAll(CancellationToken ct) =>
        Ok(await eventService.GetPublishedEventsAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var @event = await eventService.GetByIdAsync(id, ct);
        return @event is null ? NotFound() : Ok(@event);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create(CreateEventRequest request, CancellationToken ct)
    {
        try
        {
            var created = await eventService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var success = await eventService.PublishAsync(id, ct);
        return success ? NoContent() : NotFound();
    }
}