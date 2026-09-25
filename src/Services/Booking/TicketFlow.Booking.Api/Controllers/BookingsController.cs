using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Booking.Api.Contracts;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Application.Commands;
using TicketFlow.Booking.Application.Exceptions;
using TicketFlow.Booking.Domain.Exceptions;

namespace TicketFlow.Booking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(
    IMediator mediator,
    IBookingRepository bookingRepository,
    ILogger<BookingsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BookingResponse>>> GetByEmail([FromQuery] string email, CancellationToken ct)
    {
        var bookings = await bookingRepository.GetByUserEmailAsync(email, ct);
        return Ok(bookings.Select(BookingResponse.From).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetById(Guid id, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(id, ct);
        if (booking is null)
            return NotFound();

        return Ok(BookingResponse.From(booking));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateBookingCommand command, CancellationToken ct)
    {
        try
        {
            var bookingId = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = bookingId }, new { id = bookingId });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (EventsServiceUnavailableException ex)
        {
            logger.LogWarning(ex, "Could not create booking for event {EventId}: Events service unavailable", command.EventId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Event information is temporarily unavailable. Please try again shortly." });
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