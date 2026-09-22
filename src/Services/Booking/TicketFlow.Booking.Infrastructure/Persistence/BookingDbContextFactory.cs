using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TicketFlow.Booking.Infrastructure.Persistence;

public class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BookingDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException(
                "Connection string 'BookingDb' not found. Set it via user-secrets or the ConnectionStrings__BookingDb environment variable.");

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BookingDbContext(options);
    }
}