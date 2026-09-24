namespace TicketFlow.Booking.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, string content, CancellationToken ct = default);
}