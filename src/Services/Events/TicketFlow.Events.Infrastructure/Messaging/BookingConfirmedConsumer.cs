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

public class BookingConfirmedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingConfirmedConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "ticketflow.events";
    private const string QueueName = "events.booking-confirmed";
    private const string RoutingKey = "booking.bookingconfirmed";

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
                logger.LogError(ex, "Failed to process BookingConfirmed message");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string json, CancellationToken ct)
    {
        var bookingConfirmed = JsonSerializer.Deserialize<BookingConfirmed>(json)
            ?? throw new InvalidOperationException("Could not deserialize BookingConfirmed");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == bookingConfirmed.MessageId, ct);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", bookingConfirmed.MessageId);
            return;
        }

        // Fire-and-forget command, same shape as SeatsReleaseRequestedConsumer: no response to send back,
        // we just need to mark the hold Confirmed so ReservationExpiryWorker's Status == Held filter
        // stops picking it up and releasing seats out from under an already-paid booking.
        var reservation = await db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == bookingConfirmed.ReservationId, ct);

        if (reservation is null)
        {
            logger.LogWarning(
                "Reservation {ReservationId} not found for BookingConfirmed (booking {BookingId})",
                bookingConfirmed.ReservationId, bookingConfirmed.BookingId);
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
            else if (!@event.ConfirmReservation(bookingConfirmed.ReservationId))
            {
                logger.LogWarning(
                    "Reservation {ReservationId} was not Held (already Released or Confirmed), nothing to do",
                    bookingConfirmed.ReservationId);
            }
            else
            {
                logger.LogInformation(
                    "Reservation {ReservationId} for booking {BookingId} confirmed, no longer subject to TTL expiry",
                    bookingConfirmed.ReservationId, bookingConfirmed.BookingId);
            }
        }

        db.ProcessedMessages.Add(ProcessedMessage.Create(bookingConfirmed.MessageId));
        await db.SaveChangesAsync(ct);
    }
}
