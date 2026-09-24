using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.Services;
using TicketFlow.Events.Infrastructure.Messaging;
using TicketFlow.Events.Infrastructure.Persistence;
using TicketFlow.Events.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

var factory = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMq:HostName"] ?? "localhost",
    Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
    UserName = builder.Configuration["RabbitMq:UserName"] ?? "ticketflow",
    Password = builder.Configuration["RabbitMq:Password"] ?? "ticketflow"
};

var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventsDb")));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<BookingCreatedConsumer>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
    await EventsDbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();