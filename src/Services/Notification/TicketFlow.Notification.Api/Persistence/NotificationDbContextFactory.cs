using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketFlow.Notification.Api.Persistence;

// Lets `dotnet ef` build the context without running Program.cs, which opens a RabbitMQ connection at startup.
public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<NotificationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException(
                "Connection string 'NotificationDb' not found. Set it via user-secrets or the ConnectionStrings__NotificationDb environment variable.");

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NotificationDbContext(options);
    }
}
