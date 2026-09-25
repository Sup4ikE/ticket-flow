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

builder.Services.AddHostedService<BookingOutcomeNotificationConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
