using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Infrastructure.Persistence;

namespace TicketFlow.Booking.Infrastructure.Messaging;

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
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            var routingKey = $"booking.{message.Type}".ToLowerInvariant();

            try
            {
                await publisher.PublishAsync(routingKey, message.Content, ct);
                message.MarkAsProcessed();
                logger.LogInformation("Published outbox message {MessageId} of type {Type}", message.Id, message.Type);
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