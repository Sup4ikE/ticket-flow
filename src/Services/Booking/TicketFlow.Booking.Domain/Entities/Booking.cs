using TicketFlow.Booking.Domain.Enums;
using TicketFlow.Booking.Domain.Exceptions;

namespace TicketFlow.Booking.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? HoldExpiresAt { get; private set; }
    public Guid? ReservationId { get; private set; }
    /// <summary>Set only when Status is Cancelled; null for any other status (and for bookings cancelled before this was tracked).</summary>
    public BookingCancellationReason? CancellationReason { get; private set; }
    
    public string EventTitle { get; private set; } = string.Empty;
    public DateTime EventStartsAt { get; private set; }
    public decimal PricePerTicket { get; private set; }
    public decimal TotalPrice { get; private set; }

    private Booking() { }

    public static Booking Create(
        Guid eventId,
        string userEmail,
        int quantity,
        string eventTitle,
        DateTime eventStartsAt,
        decimal pricePerTicket)
    {
        if (quantity <= 0)
            throw new DomainException("Кількість квитків має бути більшою за нуль.");
        
        if (pricePerTicket < 0) 
            throw new DomainException("Ціна квитка не може бути відʼємною.");
        
        if (string.IsNullOrWhiteSpace(userEmail))
            throw new DomainException("Вкажіть email.");
        
        if (string.IsNullOrWhiteSpace(eventTitle))
            throw new DomainException("Назва події обовʼязкова.");
        
        if (eventStartsAt <= DateTime.UtcNow)
            throw new DomainException("Подія вже почалася — бронювання недоступне.");
        
        if (pricePerTicket * quantity > 10000)
            throw new DomainException("Загальна сума бронювання не може перевищувати 10 000.");

        return new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UserEmail = userEmail,
            Quantity = quantity,
            EventTitle = eventTitle,
            EventStartsAt = eventStartsAt,
            PricePerTicket = pricePerTicket,
            TotalPrice = pricePerTicket * quantity
        };
    }

    public void MarkAwaitingPayment(Guid reservationId, DateTime holdExpiresAt)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException($"Booking cannot await payment: current status is {Status}.");

        Status = BookingStatus.AwaitingPayment;
        ReservationId = reservationId;
        HoldExpiresAt = holdExpiresAt;
    }

    public void Pay()
    {
        if (Status != BookingStatus.AwaitingPayment)
            throw new DomainException($"Booking cannot be paid: current status is {Status}.");

        Status = BookingStatus.Confirmed;
    }

    public void Cancel(BookingCancellationReason reason)
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.AwaitingPayment))
            throw new DomainException($"Booking cannot be cancelled: current status is {Status}.");

        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
    }
}