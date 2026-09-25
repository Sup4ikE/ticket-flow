using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Booking.Application.Abstractions;
using TicketFlow.Booking.Application.Commands;
using TicketFlow.Booking.Infrastructure.Http;
using TicketFlow.Booking.Infrastructure.Messaging;
using TicketFlow.Booking.Infrastructure.Persistence;
using TicketFlow.Booking.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

var factory = new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMq:HostName"] ?? "localhost",
    Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
    UserName = builder.Configuration["RabbitMq:UserName"] ?? "ticketflow",
    Password = builder.Configuration["RabbitMq:Password"] ?? "ticketflow"
};

var connection = await factory.CreateConnectionAsync();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton(connection);

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("BookingDb")));

builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<ReservationOutcomeConsumer>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddHttpClient<IEventCatalog, EventsApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:EventsApi:BaseUrl"]
        ?? throw new InvalidOperationException("Configuration value 'Services:EventsApi:BaseUrl' is missing."));
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateBookingCommand).Assembly));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();