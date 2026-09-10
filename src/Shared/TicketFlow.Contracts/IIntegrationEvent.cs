namespace TicketFlow.Contracts;

public interface IIntegrationEvent
{
    Guid MessageId { get; }
    DateTime OccurredAt { get; }
}