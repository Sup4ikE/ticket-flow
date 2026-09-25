namespace TicketFlow.Booking.Api.Contracts;

public record BookingResponse(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTime EventStartsAt,
    string UserEmail,
    int Quantity,
    decimal PricePerTicket,
    decimal TotalPrice,
    string Status,
    Guid? ReservationId,
    DateTime? HoldExpiresAt,
    DateTime CreatedAt)
{
    public static BookingResponse From(Domain.Entities.Booking booking) => new(
        booking.Id,
        booking.EventId,
        booking.EventTitle,
        booking.EventStartsAt,
        booking.UserEmail,
        booking.Quantity,
        booking.PricePerTicket,
        booking.TotalPrice,
        booking.Status.ToString(),
        booking.ReservationId,
        booking.HoldExpiresAt,
        booking.CreatedAt);
}
