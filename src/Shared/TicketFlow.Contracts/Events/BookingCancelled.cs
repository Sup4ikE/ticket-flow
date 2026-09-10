namespace TicketFlow.Contracts.Events;

public record BookingCancelled : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public required Guid BookingId { get; init; }
    public required string UserEmail { get; init; }
    public required string EventTitle { get; init; }
    public required string Reason { get; init; }
}