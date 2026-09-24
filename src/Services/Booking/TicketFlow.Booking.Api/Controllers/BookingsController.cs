using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Application.Commands;
using TicketFlow.Booking.Domain.Exceptions;

namespace TicketFlow.Booking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IMediator mediator, IBookingRepository bookingRepository) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(id, ct);
        if (booking is null)
            return NotFound();

        return Ok(new
        {
            booking.Id,
            status = booking.Status.ToString(),
            booking.ReservationId,
            booking.HoldExpiresAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateBookingCommand command, CancellationToken ct)
    {
        try
        {
            var bookingId = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(Create), new { id = bookingId }, new { id = bookingId });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            var status = await mediator.Send(new CancelBookingCommand(id), ct);
            return Ok(new { id, status = status.ToString() });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
    {
        try
        {
            var status = await mediator.Send(new PayBookingCommand(id), ct);
            return Ok(new { id, status = status.ToString() });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}