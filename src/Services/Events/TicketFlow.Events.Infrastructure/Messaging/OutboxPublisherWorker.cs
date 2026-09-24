using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Infrastructure.Persistence;

namespace TicketFlow.Events.Infrastructure.Messaging;

public class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing outbox messages.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            // Unlike Booking (always one event type -> one derived routing key), Events publishes
            // different response types per message (SeatsReserved vs SeatsReservationFailed), so the
            // consumer stores the actual routing key directly in Type instead of a type name to derive one from.
            var routingKey = message.Type;

            try
            {
                await publisher.PublishAsync(routingKey, message.Content, ct);
                message.MarkAsProcessed();
                logger.LogInformation("Published outbox message {MessageId} with routing key {RoutingKey}", message.Id, routingKey);
            }
            catch (Exception ex)
            {
                message.MarkAsFailed(ex.Message);
                logger.LogWarning(ex, "Failed to publish outbox message {MessageId}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
