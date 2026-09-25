using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using StackExchange.Redis;
using TicketFlow.Events.Application.Abstractions;
using TicketFlow.Events.Application.Configuration;
using TicketFlow.Events.Application.Services;
using TicketFlow.Events.Infrastructure.Caching;
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

// AbortOnConnectFail=false: the service must start and serve from Postgres even when Redis is down.
// BacklogPolicy.FailFast: while disconnected, commands fail immediately instead of queueing until AsyncTimeout,
// so the Postgres fallback costs nothing extra; short timeouts cover a Redis that is up but unresponsive.
var redisOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
redisOptions.AbortOnConnectFail = false;
redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
redisOptions.ConnectTimeout = 2000;
redisOptions.SyncTimeout = 1000;
redisOptions.AsyncTimeout = 1000;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<IPublishedEventsCache, RedisPublishedEventsCache>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventsDb")));

builder.Services.Configure<ReservationSettings>(builder.Configuration.GetSection("ReservationSettings"));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<BookingCreatedConsumer>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.AddHostedService<SeatsReleaseRequestedConsumer>();
builder.Services.AddHostedService<BookingConfirmedConsumer>();

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