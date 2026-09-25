using TicketFlow.Contracts.Events;
using TicketFlow.Events.Domain.Enums;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Domain.Entities;

public class Event
{
    private readonly List<Reservation> _reservations = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartsAt { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public int AvailableSeats { get; private set; }
    public decimal Price { get; private set; }
    public EventStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Reservation> Reservations => _reservations;

    private Event()
    {
    }

    public static Event Create(
        string title,
        string description,
        DateTime startsAt,
        string venue,
        int capacity,
        decimal price,
        TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

        if (startsAt.Kind != DateTimeKind.Utc)
            startsAt = startsAt.ToUniversalTime();
        
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");

        if (string.IsNullOrWhiteSpace(venue))
            throw new DomainException("Venue is required.");

        if (startsAt <= now)
            throw new DomainException("Event cannot start in the past.");

        if (capacity <= 0)
            throw new DomainException("Capacity must be greater than zero.");

        if (price < 0)
            throw new DomainException("Price cannot be negative.");

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartsAt = startsAt,
            Venue = venue,
            Capacity = capacity,
            AvailableSeats = capacity,
            Price = price,
            Status = EventStatus.Draft,
            CreatedAt = now
        };
    }

    public ReservationResult TryReserve(
        Guid bookingId, int quantity, TimeSpan holdDuration, TimeProvider? timeProvider = null)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

        // Deliberately checked BEFORE the event-status/start-time guards below: the first outcome for a bookingId
        // is final. A redelivered BookingCreated for an already-reserved booking gets its existing hold back even if
        // the event was cancelled or started since - we don't re-judge an already-processed booking retroactively.
        // (Released holds are excluded, so a booking whose hold expired goes through the full checks again.)
        var existing = _reservations.FirstOrDefault(r =>
            r.BookingId == bookingId && r.Status != ReservationStatus.Released);
        if (existing is not null)
            return ReservationResult.Success(existing);

        if (Status == EventStatus.Cancelled)
            return ReservationResult.Failure(ReservationFailureReason.EventCancelled);

        if (Status != EventStatus.Published)
            return ReservationResult.Failure(ReservationFailureReason.EventNotFound);

        if (StartsAt <= now)
            return ReservationResult.Failure(ReservationFailureReason.EventAlreadyStarted);

        if (quantity > AvailableSeats)
            return ReservationResult.Failure(ReservationFailureReason.NotEnoughSeats);

        var reservation = Reservation.Create(Id, bookingId, quantity, holdDuration, now);
        _reservations.Add(reservation);
        AvailableSeats -= quantity;

        return ReservationResult.Success(reservation);
    }

    public bool Release(Guid reservationId)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is null || reservation.Status != ReservationStatus.Held)
            return false;

        reservation.Release();
        AvailableSeats += reservation.Quantity;

        return true;
    }

    public bool ConfirmReservation(Guid reservationId)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is null || reservation.Status != ReservationStatus.Held)
            return false;

        reservation.Confirm();
        return true;
    }

    public void Publish(TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

        if (Status == EventStatus.Cancelled)
            throw new DomainException("Cannot publish a cancelled event.");

        if (StartsAt <= now)
            throw new DomainException("Cannot publish an event that already started.");

        Status = EventStatus.Published;
    }

    public void Cancel() => Status = EventStatus.Cancelled;
}