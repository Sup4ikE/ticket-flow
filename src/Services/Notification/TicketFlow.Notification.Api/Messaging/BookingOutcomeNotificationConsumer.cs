using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketFlow.Contracts.Events;
using TicketFlow.Notification.Api.Persistence;
using TicketFlow.Notification.Api.Persistence.Entities;

namespace TicketFlow.Notification.Api.Messaging;

public class BookingOutcomeNotificationConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingOutcomeNotificationConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "ticketflow.events";
    private const string QueueName = "notification.booking-outcome";
    private const string BookingConfirmedRoutingKey = "booking.bookingconfirmed";
    private const string BookingCancelledRoutingKey = "booking.bookingcancelled";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, BookingConfirmedRoutingKey, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, BookingCancelledRoutingKey, cancellationToken: stoppingToken);

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
                logger.LogError(ex, "Failed to process booking outcome notification with routing key {RoutingKey}", ea.RoutingKey);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string routingKey, string json, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var messageId = JsonSerializer.Deserialize<MessageEnvelope>(json)?.MessageId
            ?? throw new InvalidOperationException("Could not read MessageId from message");

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == messageId, ct);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", messageId);
            return;
        }

        // Deserialize before committing so a malformed message fails (nack+requeue) without being marked processed.
        Action sendEmail = routingKey switch
        {
            BookingConfirmedRoutingKey => SendConfirmationEmail(
                JsonSerializer.Deserialize<BookingConfirmed>(json)
                    ?? throw new InvalidOperationException("Could not deserialize BookingConfirmed")),
            BookingCancelledRoutingKey => SendCancellationEmail(
                JsonSerializer.Deserialize<BookingCancelled>(json)
                    ?? throw new InvalidOperationException("Could not deserialize BookingCancelled")),
            _ => throw new InvalidOperationException($"Unexpected routing key '{routingKey}' on queue {QueueName}")
        };

        // Commit first, side-effect second - same ordering as the rest of the project. A crash in between
        // drops the email rather than duplicating it on redelivery.
        db.ProcessedMessages.Add(ProcessedMessage.Create(messageId));
        await db.SaveChangesAsync(ct);

        sendEmail();
    }

    // Mock delivery: there is no real email/SMS provider, the structured log line stands in for the sent email.
    // Notification is the end of the chain - it publishes nothing, so there is no Outbox here.
    private Action SendConfirmationEmail(BookingConfirmed bookingConfirmed) => () =>
        logger.LogInformation(
            "📧 Confirmation email sent to {UserEmail} — BookingId: {BookingId}, ReservationId: {ReservationId}, Event: {EventTitle} at {EventStartsAt}, Quantity: {Quantity}, Total: {TotalPrice}",
            bookingConfirmed.UserEmail, bookingConfirmed.BookingId, bookingConfirmed.ReservationId,
            bookingConfirmed.EventTitle, bookingConfirmed.EventStartsAt, bookingConfirmed.Quantity,
            bookingConfirmed.TotalPrice);

    private Action SendCancellationEmail(BookingCancelled bookingCancelled) => () =>
        logger.LogInformation(
            "📧 Cancellation email sent to {UserEmail} — BookingId: {BookingId}, Event: {EventTitle}, Reason: {Reason}",
            bookingCancelled.UserEmail, bookingCancelled.BookingId, bookingCancelled.EventTitle,
            bookingCancelled.Reason);

    private sealed record MessageEnvelope(Guid MessageId);
}
