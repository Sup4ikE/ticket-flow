namespace TicketFlow.Contracts.Events;

public record ReservationExpired : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public required Guid BookingId { get; init; }
    public required Guid ReservationId { get; init; }
}
