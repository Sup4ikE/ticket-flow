namespace TicketFlow.Contracts.Events;

public record BookingConfirmed : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public required Guid BookingId { get; init; }
    public required Guid ReservationId { get; init; }
    public required string UserEmail { get; init; }
    public required string EventTitle { get; init; }
    public required DateTime EventStartsAt { get; init; }
    public required int Quantity { get; init; }
    public required decimal TotalPrice { get; init; }
}