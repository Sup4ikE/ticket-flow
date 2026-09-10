namespace TicketFlow.Contracts.Events;

public record BookingCreated : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public required Guid BookingId { get; init; }
    public required Guid EventId { get; init; }
    public required int Quantity { get; init; }
}