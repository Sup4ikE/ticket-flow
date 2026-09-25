using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketFlow.Contracts.Events;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Enums;
using TicketFlow.Events.Infrastructure.Persistence;

namespace TicketFlow.Events.Infrastructure.Messaging;

public class ReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    private const string RoutingKey = "events.reservationexpired";
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while expiring reservations.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        var now = DateTime.UtcNow;

        // Lightweight, untracked scan for candidates; the actual state change goes through the owning
        // Event (below), whose EF configuration is the only place allowed to mutate Reservation.Status.
        var expiredReservations = await db.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Held && r.ExpiresAt < now)
            .OrderBy(r => r.ExpiresAt)
            .Take(20)
            .ToListAsync(ct);

        if (expiredReservations.Count == 0)
            return;

        foreach (var reservation in expiredReservations)
        {
            var @event = await db.Events
                .Include(e => e.Reservations)
                .FirstOrDefaultAsync(e => e.Id == reservation.EventId, ct);

            if (@event is null)
            {
                logger.LogWarning(
                    "Event {EventId} not found for expired reservation {ReservationId}",
                    reservation.EventId, reservation.Id);
                continue;
            }

            if (!@event.Release(reservation.Id))
            {
                // Already released/confirmed through another path since the scan above ran - nothing to do.
                continue;
            }

            db.OutboxMessages.Add(OutboxMessage.Create(
                RoutingKey,
                JsonSerializer.Serialize(new ReservationExpired
                {
                    BookingId = reservation.BookingId,
                    ReservationId = reservation.Id
                })));

            logger.LogInformation(
                "Reservation {ReservationId} for booking {BookingId} expired, seats released",
                reservation.Id, reservation.BookingId);
        }

        await db.SaveChangesAsync(ct);
    }
}
