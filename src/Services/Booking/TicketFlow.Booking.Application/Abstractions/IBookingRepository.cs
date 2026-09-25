namespace TicketFlow.Booking.Application.Abstractions;

public interface IBookingRepository
{
    void Add(Domain.Entities.Booking booking);
    Task<Domain.Entities.Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Domain.Entities.Booking>> GetByUserEmailAsync(string userEmail, CancellationToken ct = default);
}