using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using TicketFlow.Notification.Api.Messaging;
using TicketFlow.Notification.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

var factory = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMq:HostName"] ?? "localhost",
    Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
    UserName = builder.Configuration["RabbitMq:UserName"] ?? "ticketflow",
    Password = builder.Configuration["RabbitMq:Password"] ?? "ticketflow"
};

var connection = await factory.CreateConnectionAsync();

builder.Services.AddOpenApi();

builder.Services.AddSingleton(connection);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotificationDb")));

builder.Services.AddHealthChecks().AddDbContextCheck<NotificationDbContext>();

builder.Services.AddHostedService<BookingOutcomeNotificationConsumer>();

var app = builder.Build();

// Containers opt in via Database__MigrateOnStartup (docker-compose.yml); local dev keeps applying
// migrations by hand with `dotnet ef database update`, so this never runs there.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Used by the docker-compose healthcheck. Only reachable once app.Run() starts, i.e. after the startup
// migration above, so "healthy" also means "schema is up to date and the database answers".
app.MapHealthChecks("/health");

app.Run();
