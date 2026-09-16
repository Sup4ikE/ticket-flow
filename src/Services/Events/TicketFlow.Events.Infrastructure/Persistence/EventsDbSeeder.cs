using Microsoft.EntityFrameworkCore;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Infrastructure.Persistence;

public static class EventsDbSeeder
{
    public static async Task SeedAsync(EventsDbContext context)
    {
        if (await context.Events.AnyAsync())
            return;

        var events = new List<Event>
        {
            Event.Create(
                "Kyiv Web3 Meetup",
                "Community meetup for builders in the Ukrainian crypto ecosystem.",
                DateTime.UtcNow.AddDays(14),
                "Kyiv, UNIT.City",
                capacity: 80,
                price: 250m),

            Event.Create(
                ".NET Lviv Conf",
                "Talks on distributed systems, EF Core internals, and cloud-native .NET.",
                DateTime.UtcNow.AddDays(21),
                "Lviv, Ukrainian Catholic University",
                capacity: 150,
                price: 400m),

            Event.Create(
                "AI Builders Podcast - Live Recording",
                "Live episode with three founders building AI products in Ukraine.",
                DateTime.UtcNow.AddDays(7),
                "Kyiv, Sense Hub",
                capacity: 40,
                price: 0m),

            Event.Create(
                "Crypto Office Roundtable",
                "Closed-door discussion on exchange listings and token launches.",
                DateTime.UtcNow.AddDays(30),
                "Online",
                capacity: 25,
                price: 100m),

            Event.Create(
                "Draft: Backend Deep Dive (unpublished)",
                "Placeholder event kept in Draft to verify the publish flow.",
                DateTime.UtcNow.AddDays(45),
                "TBA",
                capacity: 60,
                price: 300m)
        };

        events[0].Publish();
        events[1].Publish();
        events[2].Publish();
        events[3].Publish();
        // events[4] лишається Draft — навмисно

        context.Events.AddRange(events);
        await context.SaveChangesAsync();
    }
}