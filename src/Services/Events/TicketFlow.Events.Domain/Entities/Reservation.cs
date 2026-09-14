using TicketFlow.Events.Domain.Enums;

namespace TicketFlow.Events.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid BookingId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private Reservation()
    {
    }

    internal static Reservation Create(Guid eventId, Guid bookingId, int quantity, TimeSpan holdDuration)
    {
        var now = DateTime.UtcNow;

        return new Reservation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            BookingId = bookingId,
            Quantity = quantity,
            Status = ReservationStatus.Held,
            CreatedAt = now,
            ExpiresAt = now + holdDuration
        };
    }

    internal void Release()
    {
        Status = ReservationStatus.Released;
    }

    internal void Confirm()
    {
        Status = ReservationStatus.Confirmed;
    }

    public bool IsExpired(DateTime now) => Status == ReservationStatus.Held && ExpiresAt <= now;
}