using Microsoft.EntityFrameworkCore;
using TicketFlow.Booking.Application.Abstractions;

namespace TicketFlow.Booking.Infrastructure.Persistence.Repositories;

public class BookingRepository(BookingDbContext context): IBookingRepository
{
    public void Add(Domain.Entities.Booking booking) => context.Bookings.Add(booking);

    public Task<Domain.Entities.Booking?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);
}