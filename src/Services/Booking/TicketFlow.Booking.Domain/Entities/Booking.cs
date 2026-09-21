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
            throw new DomainException("Quantity must be greater than zero.");
        
        if (pricePerTicket < 0) 
            throw new DomainException("Price per ticket must be greater than zero.");
        
        if (string.IsNullOrWhiteSpace(userEmail))
            throw new DomainException("User email is required.");
        
        if (string.IsNullOrWhiteSpace(eventTitle))
            throw new DomainException("Event title is required.");
        
        if (eventStartsAt <= DateTime.UtcNow)
            throw new DomainException("Event cannot start in the past.");
        
        if (pricePerTicket * quantity > 10000)
            throw new DomainException("Total price cannot exceed $10,000.");

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

    public void Confirm(DateTime holdExpiresAt)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Booking is not pending.");
        
        Status = BookingStatus.Confirmed;
        HoldExpiresAt = holdExpiresAt;
    }

    public void Cancel()
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Booking is not pending.");
        
        Status = BookingStatus.Cancelled;
    }
}