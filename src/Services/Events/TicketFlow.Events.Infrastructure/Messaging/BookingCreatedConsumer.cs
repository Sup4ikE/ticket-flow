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

public class BookingCreatedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<BookingCreatedConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "ticketflow.events";
    private const string QueueName = "events.booking-created";
    private const string RoutingKey = "booking.bookingcreated";

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
                logger.LogError(ex, "Failed to process BookingCreated message");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(string json, CancellationToken ct)
    {
        var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(json)
            ?? throw new InvalidOperationException("Could not deserialize BookingCreated");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == bookingCreated.MessageId, ct);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {MessageId} already processed, skipping", bookingCreated.MessageId);
            return;
        }

        var @event = await db.Events.FirstOrDefaultAsync(e => e.Id == bookingCreated.EventId, ct);

        string routingKey;
        object responseEvent;

        if (@event is null)
        {
            routingKey = "events.seatsreservationfailed";
            responseEvent = new SeatsReservationFailed
            {
                BookingId = bookingCreated.BookingId,
                Reason = ReservationFailureReason.EventNotFound
            };
        }
        else
        {
            var result = @event.TryReserve(bookingCreated.BookingId, bookingCreated.Quantity, TimeSpan.FromMinutes(10));

            if (result.IsSuccess)
            {
                // The reservation is created with a client-assigned Guid, and Reservations isn't
                // eagerly loaded on @event. Left to EF's automatic change-tracker fixup, a non-default
                // key reachable from an Unchanged parent is inferred as Modified instead of Added, which
                // generates an UPDATE against a row that was never inserted (DbUpdateConcurrencyException
                // on every single reservation, causing an infinite nack/redeliver loop). Setting the state
                // explicitly here — before anything else can trigger DetectChanges — avoids that heuristic.
                db.Entry(result.Reservation!).State = EntityState.Added;

                routingKey = "events.seatsreserved";
                responseEvent = new SeatsReserved
                {
                    BookingId = bookingCreated.BookingId,
                    ReservationId = result.Reservation!.Id,
                    HoldExpiresAt = result.Reservation.ExpiresAt
                };
            }
            else
            {
                routingKey = "events.seatsreservationfailed";
                responseEvent = new SeatsReservationFailed
                {
                    BookingId = bookingCreated.BookingId,
                    Reason = result.FailureReason!.Value,
                    AvailableSeats = result.FailureReason == ReservationFailureReason.NotEnoughSeats
                        ? @event.AvailableSeats
                        : null
                };
            }
        }

        db.ProcessedMessages.Add(ProcessedMessage.Create(bookingCreated.MessageId));
        db.OutboxMessages.Add(OutboxMessage.Create(routingKey, JsonSerializer.Serialize(responseEvent)));
        await db.SaveChangesAsync(ct);
    }
}