using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketFlow.Contracts.Events;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Infrastructure.Persistence;

namespace TicketFlow.Events.Infrastructure.Messaging;

public class SeatsReleaseRequestedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<SeatsReleaseRequestedConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "ticketflow.events";
    private const string QueueName = "events.seats-release-requested";
    private const string RoutingKey = "booking.seatsreleaserequested";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                await HandleMessageAsync(json, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process SeatsReleaseRequested message");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string json, CancellationToken ct)
    {
        var seatsReleaseRequested = JsonSerializer.Deserialize<SeatsReleaseRequested>(json)
            ?? throw new InvalidOperationException("Could not deserialize SeatsReleaseRequested");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == seatsReleaseRequested.MessageId, ct);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", seatsReleaseRequested.MessageId);
            return;
        }

        // Fire-and-forget command: Booking already cancelled itself synchronously before publishing this,
        // so there is no response to send back - we only need to release the hold if there's still one to release.
        var reservation = await db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == seatsReleaseRequested.ReservationId, ct);

        if (reservation is null)
        {
            logger.LogWarning(
                "Reservation {ReservationId} not found for SeatsReleaseRequested (booking {BookingId})",
                seatsReleaseRequested.ReservationId, seatsReleaseRequested.BookingId);
        }
        else
        {
            var @event = await db.Events
                .Include(e => e.Reservations)
                .FirstOrDefaultAsync(e => e.Id == reservation.EventId, ct);

            if (@event is null)
            {
                logger.LogWarning(
                    "Event {EventId} not found for reservation {ReservationId}", reservation.EventId, reservation.Id);
            }
            else if (!@event.Release(seatsReleaseRequested.ReservationId))
            {
                logger.LogInformation(
                    "Reservation {ReservationId} was already released or confirmed, nothing to do",
                    seatsReleaseRequested.ReservationId);
            }
        }

        db.ProcessedMessages.Add(ProcessedMessage.Create(seatsReleaseRequested.MessageId));
        await db.SaveChangesAsync(ct);
    }
}
