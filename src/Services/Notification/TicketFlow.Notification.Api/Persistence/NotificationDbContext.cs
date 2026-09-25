using Microsoft.EntityFrameworkCore;
using TicketFlow.Notification.Api.Persistence.Entities;

namespace TicketFlow.Notification.Api.Persistence;

// Idempotency-only store: Notification owns no business data, just the set of MessageIds it has already handled.
public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
