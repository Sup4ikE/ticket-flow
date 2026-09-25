using System.Text;
using RabbitMQ.Client;
using TicketFlow.Booking.Application.Abstractions;

namespace TicketFlow.Booking.Infrastructure.Messaging;

public class RabbitMqEventPublisher(IConnection connection) : IEventPublisher
{
    private const string ExchangeName = "ticketflow.events";

    public async Task PublishAsync(string routingKey, string content, CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(content);

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            body: body,
            cancellationToken: ct);
    }
}