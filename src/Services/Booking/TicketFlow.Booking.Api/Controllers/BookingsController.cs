using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Booking.Application.Commands;
using TicketFlow.Booking.Domain.Exceptions;

namespace TicketFlow.Booking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IMediator mediator) : ControllerBase
{
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
}