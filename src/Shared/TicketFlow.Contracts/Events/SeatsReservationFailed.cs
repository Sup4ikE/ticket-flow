namespace TicketFlow.Contracts.Events;

public enum ReservationFailureReason
{
    NotEnoughSeats = 1,
    EventNotFound = 2,
    EventCancelled = 3,
    EventAlreadyStarted = 4
}

public record SeatsReservationFailed : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public required Guid BookingId { get; init; }
    public required ReservationFailureReason Reason { get; init; }
    public int? AvailableSeats { get; init; }
}