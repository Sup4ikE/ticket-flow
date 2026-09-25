using TicketFlow.Contracts.Events;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Enums;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Tests;

public class EventTimeTests
{
    private static readonly DateTimeOffset CreatedAt = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime StartsAt = CreatedAt.AddDays(1).UtcDateTime;
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    private readonly FixedTimeProvider _clock = new(CreatedAt);
    private readonly Event _event;

    public EventTimeTests()
    {
        _event = Event.Create("Kyiv Web3 Meetup", "desc", StartsAt, "UNIT.City", 10, 250m, _clock);
    }

    [Fact]
    public void Create_UsesInjectedClockForCreatedAt()
    {
        Assert.Equal(CreatedAt.UtcDateTime, _event.CreatedAt);
    }

    [Fact]
    public void Create_StartsAtNotAfterInjectedNow_ThrowsDomainException()
    {
        _clock.UtcNow = StartsAt;

        var ex = Assert.Throws<DomainException>(() =>
            Event.Create("Late", "desc", StartsAt, "UNIT.City", 10, 250m, _clock));

        Assert.Equal("Дата початку події не може бути в минулому.", ex.Message);
    }

    [Fact]
    public void Publish_AfterEventStarted_ThrowsDomainException()
    {
        _clock.UtcNow = StartsAt.AddMinutes(1);

        var ex = Assert.Throws<DomainException>(() => _event.Publish(_clock));

        Assert.Equal("Не можна опублікувати подію, яка вже почалася.", ex.Message);
        Assert.Equal(EventStatus.Draft, _event.Status);
    }

    [Fact]
    public void TryReserve_AfterEventStarted_ReturnsEventAlreadyStarted()
    {
        _event.Publish(_clock);
        _clock.UtcNow = StartsAt.AddMinutes(1);

        var result = _event.TryReserve(Guid.NewGuid(), 1, HoldDuration, _clock);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationFailureReason.EventAlreadyStarted, result.FailureReason);
        Assert.Equal(10, _event.AvailableSeats);
        Assert.Empty(_event.Reservations);
    }

    [Fact]
    public void TryReserve_BeforeStart_HoldTimesComeFromInjectedClock()
    {
        _event.Publish(_clock);

        var reservation = _event.TryReserve(Guid.NewGuid(), 1, HoldDuration, _clock).Reservation!;

        Assert.Equal(CreatedAt.UtcDateTime, reservation.CreatedAt);
        Assert.Equal(CreatedAt.UtcDateTime + HoldDuration, reservation.ExpiresAt);
    }
}
