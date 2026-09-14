using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TicketFlow.Events.Infrastructure.Persistence;

public class EventsDbContextFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<EventsDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("EventsDb")
            ?? throw new InvalidOperationException(
                "Connection string 'EventsDb' not found. Set it via user-secrets or the ConnectionStrings__EventsDb environment variable.");

        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EventsDbContext(options);
    }
}