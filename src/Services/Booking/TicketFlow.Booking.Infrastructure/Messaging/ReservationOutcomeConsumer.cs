using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketFlow.Booking.Domain.Entities;
using TicketFlow.Booking.Domain.Enums;
using TicketFlow.Booking.Infrastructure.Persistence;
using TicketFlow.Contracts.Events;

namespace TicketFlow.Booking.Infrastructure.Messaging;

public class ReservationOutcomeConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationOutcomeConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "ticketflow.events";
    private const string QueueName = "booking.reservation-outcome";
    private const string SeatsReservedRoutingKey = "events.seatsreserved";
    private const string SeatsReservationFailedRoutingKey = "events.seatsreservationfailed";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, SeatsReservedRoutingKey, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, SeatsReservationFailedRoutingKey, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                await HandleMessageAsync(ea.RoutingKey, json, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process reservation outcome message with routing key {RoutingKey}", ea.RoutingKey);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        var messageId = JsonSerializer.Deserialize<MessageEnvelope>(json)?.MessageId
            ?? throw new InvalidOperationException("Could not read MessageId from message");

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == messageId, ct);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
            return;
        }

        string outboxType;
        object outboxEvent;

        switch (routingKey)
        {
            case SeatsReservedRoutingKey:
            {
                var seatsReserved = JsonSerializer.Deserialize<SeatsReserved>(json)
                    ?? throw new InvalidOperationException("Could not deserialize SeatsReserved");

                var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == seatsReserved.BookingId, ct)
                    ?? throw new InvalidOperationException($"Booking {seatsReserved.BookingId} not found");

                // Confirm/Cancel only accept a Pending booking and throw otherwise. A booking can already be
                // out of Pending here if this outcome is a duplicate produced upstream (e.g. Events redelivering
                // under a new MessageId). Without this guard that throw would nack+requeue forever - the exact
                // infinite-retry-loop bug we hit and fixed on the Events side.
                if (booking.Status != BookingStatus.Pending)
                {
                    logger.LogWarning(
                        "Booking {BookingId} is already {Status}, skipping SeatsReserved outcome for message {MessageId}",
                        booking.Id, booking.Status, messageId);
                    db.ProcessedMessages.Add(ProcessedMessage.Create(messageId));
                    await db.SaveChangesAsync(ct);
                    return;
                }

                booking.Confirm(seatsReserved.HoldExpiresAt);

                outboxType = nameof(BookingConfirmed);
                outboxEvent = new BookingConfirmed
                {
                    BookingId = booking.Id,
                    UserEmail = booking.UserEmail,
                    EventTitle = booking.EventTitle,
                    EventStartsAt = booking.EventStartsAt,
                    Quantity = booking.Quantity,
                    TotalPrice = booking.TotalPrice
                };
                break;
            }
            case SeatsReservationFailedRoutingKey:
            {
                var seatsReservationFailed = JsonSerializer.Deserialize<SeatsReservationFailed>(json)
                    ?? throw new InvalidOperationException("Could not deserialize SeatsReservationFailed");

                var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == seatsReservationFailed.BookingId, ct)
                    ?? throw new InvalidOperationException($"Booking {seatsReservationFailed.BookingId} not found");

                if (booking.Status != BookingStatus.Pending)
                {
                    logger.LogWarning(
                        "Booking {BookingId} is already {Status}, skipping SeatsReservationFailed outcome for message {MessageId}",
                        booking.Id, booking.Status, messageId);
                    db.ProcessedMessages.Add(ProcessedMessage.Create(messageId));
                    await db.SaveChangesAsync(ct);
                    return;
                }

                booking.Cancel();

                outboxType = nameof(BookingCancelled);
                outboxEvent = new BookingCancelled
                {
                    BookingId = booking.Id,
                    UserEmail = booking.UserEmail,
                    EventTitle = booking.EventTitle,
                    Reason = seatsReservationFailed.Reason.ToString()
                };
                break;
            }
            default:
                throw new InvalidOperationException($"Unexpected routing key '{routingKey}' on queue {QueueName}");
        }

        db.ProcessedMessages.Add(ProcessedMessage.Create(messageId));
        db.OutboxMessages.Add(OutboxMessage.Create(outboxType, JsonSerializer.Serialize(outboxEvent)));
        await db.SaveChangesAsync(ct);
    }

    private sealed record MessageEnvelope(Guid MessageId);
}
