using TicketFlow.Contracts.Events;
using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Enums;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Tests;

public class EventTests
{
    private const int Capacity = 10;
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    private readonly Event _event;

    public EventTests()
    {
        _event = CreateEvent();
        _event.Publish();
    }

    private static Event CreateEvent(int capacity = Capacity) =>
        Event.Create("Kyiv Web3 Meetup", "Community meetup", DateTime.UtcNow.AddDays(7), "UNIT.City", capacity, 250m);

    private Reservation ReserveHeld(int quantity = 3) =>
        _event.TryReserve(Guid.NewGuid(), quantity, HoldDuration).Reservation!;

    [Fact]
    public void Create_ValidInput_StartsAsDraftWithFullCapacity()
    {
        var @event = CreateEvent();

        Assert.Equal(EventStatus.Draft, @event.Status);
        Assert.Equal(Capacity, @event.AvailableSeats);
        Assert.Empty(@event.Reservations);
    }

    [Theory]
    [InlineData("", "UNIT.City", 10, 100)]
    [InlineData("Title", " ", 10, 100)]
    [InlineData("Title", "UNIT.City", 0, 100)]
    [InlineData("Title", "UNIT.City", 10, -1)]
    public void Create_InvalidInput_ThrowsDomainException(string title, string venue, int capacity, double price)
    {
        Assert.Throws<DomainException>(() =>
            Event.Create(title, "desc", DateTime.UtcNow.AddDays(1), venue, capacity, (decimal)price));
    }

    [Fact]
    public void Publish_CancelledEvent_ThrowsDomainException()
    {
        _event.Cancel();

        Assert.Throws<DomainException>(() => _event.Publish());
    }

    [Fact]
    public void TryReserve_EnoughSeats_ReturnsHeldReservationAndDecrementsSeats()
    {
        var bookingId = Guid.NewGuid();

        var result = _event.TryReserve(bookingId, 3, HoldDuration);

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);
        var reservation = result.Reservation!;
        Assert.Equal(ReservationStatus.Held, reservation.Status);
        Assert.Equal(bookingId, reservation.BookingId);
        Assert.Equal(_event.Id, reservation.EventId);
        Assert.Equal(3, reservation.Quantity);
        Assert.Equal(reservation.CreatedAt + HoldDuration, reservation.ExpiresAt);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
        Assert.Single(_event.Reservations);
    }

    [Fact]
    public void TryReserve_ExactlyAllRemainingSeats_Succeeds()
    {
        var result = _event.TryReserve(Guid.NewGuid(), Capacity, HoldDuration);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _event.AvailableSeats);
    }

    [Fact]
    public void TryReserve_NotEnoughSeats_ReturnsFailureWithReason()
    {
        ReserveHeld(quantity: 8);

        var result = _event.TryReserve(Guid.NewGuid(), 3, HoldDuration);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Reservation);
        Assert.Equal(ReservationFailureReason.NotEnoughSeats, result.FailureReason);
        Assert.Equal(2, _event.AvailableSeats);
        Assert.Single(_event.Reservations);
    }

    // Domain half of the double-booking guard (the other half is the xmin concurrency token, DB-only):
    // a redelivered BookingCreated for the same booking must hand back the same hold, not take seats again.
    [Fact]
    public void TryReserve_SameBookingIdTwice_ReturnsExistingReservationWithoutTakingSeatsAgain()
    {
        var bookingId = Guid.NewGuid();
        var first = _event.TryReserve(bookingId, 3, HoldDuration).Reservation!;

        var second = _event.TryReserve(bookingId, 3, HoldDuration);

        Assert.True(second.IsSuccess);
        Assert.Same(first, second.Reservation);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
        Assert.Single(_event.Reservations);
    }

    [Fact]
    public void TryReserve_SameBookingIdAfterConfirm_ReturnsExistingConfirmedReservation()
    {
        var bookingId = Guid.NewGuid();
        var first = _event.TryReserve(bookingId, 3, HoldDuration).Reservation!;
        _event.ConfirmReservation(first.Id);

        var second = _event.TryReserve(bookingId, 3, HoldDuration);

        Assert.Same(first, second.Reservation);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
    }

    [Fact]
    public void TryReserve_SameBookingIdAfterRelease_CreatesNewHold()
    {
        var bookingId = Guid.NewGuid();
        var first = _event.TryReserve(bookingId, 3, HoldDuration).Reservation!;
        _event.Release(first.Id);

        var second = _event.TryReserve(bookingId, 3, HoldDuration);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Id, second.Reservation!.Id);
        Assert.Equal(ReservationStatus.Held, second.Reservation.Status);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
        Assert.Equal(2, _event.Reservations.Count);
    }

    [Fact]
    public void TryReserve_DraftEvent_ReturnsEventNotFound()
    {
        var draft = CreateEvent();

        var result = draft.TryReserve(Guid.NewGuid(), 1, HoldDuration);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationFailureReason.EventNotFound, result.FailureReason);
        Assert.Equal(Capacity, draft.AvailableSeats);
    }

    [Fact]
    public void TryReserve_CancelledEvent_ReturnsEventCancelled()
    {
        _event.Cancel();

        var result = _event.TryReserve(Guid.NewGuid(), 1, HoldDuration);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationFailureReason.EventCancelled, result.FailureReason);
        Assert.Equal(Capacity, _event.AvailableSeats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryReserve_NonPositiveQuantity_ThrowsDomainException(int quantity)
    {
        Assert.Throws<DomainException>(() => _event.TryReserve(Guid.NewGuid(), quantity, HoldDuration));
    }

    [Fact]
    public void Release_HeldReservation_ReturnsSeatsAndMarksReleased()
    {
        var reservation = ReserveHeld(quantity: 3);

        var released = _event.Release(reservation.Id);

        Assert.True(released);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(Capacity, _event.AvailableSeats);
    }

    // Regression guard: a duplicate SeatsReleaseRequested / expiry must not hand the same seats back twice.
    [Fact]
    public void Release_AlreadyReleased_ReturnsFalseAndDoesNotReturnSeatsTwice()
    {
        var reservation = ReserveHeld(quantity: 3);
        _event.Release(reservation.Id);

        var releasedAgain = _event.Release(reservation.Id);

        Assert.False(releasedAgain);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(Capacity, _event.AvailableSeats);
    }

    // The Held-forever / release-after-pay race: an expiry arriving after payment must not free paid seats.
    [Fact]
    public void Release_ConfirmedReservation_ReturnsFalseAndKeepsSeatsTaken()
    {
        var reservation = ReserveHeld(quantity: 3);
        _event.ConfirmReservation(reservation.Id);

        var released = _event.Release(reservation.Id);

        Assert.False(released);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
    }

    [Fact]
    public void Release_UnknownReservation_ReturnsFalse()
    {
        ReserveHeld(quantity: 3);

        Assert.False(_event.Release(Guid.NewGuid()));
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
    }

    [Fact]
    public void ConfirmReservation_Held_MarksConfirmedWithoutChangingSeats()
    {
        var reservation = ReserveHeld(quantity: 3);

        var confirmed = _event.ConfirmReservation(reservation.Id);

        Assert.True(confirmed);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
    }

    [Fact]
    public void ConfirmReservation_AlreadyConfirmed_ReturnsFalseWithoutThrowing()
    {
        var reservation = ReserveHeld(quantity: 3);
        _event.ConfirmReservation(reservation.Id);

        var confirmedAgain = _event.ConfirmReservation(reservation.Id);

        Assert.False(confirmedAgain);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(Capacity - 3, _event.AvailableSeats);
    }

    [Fact]
    public void ConfirmReservation_Released_ReturnsFalseAndStaysReleased()
    {
        var reservation = ReserveHeld(quantity: 3);
        _event.Release(reservation.Id);

        var confirmed = _event.ConfirmReservation(reservation.Id);

        Assert.False(confirmed);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(Capacity, _event.AvailableSeats);
    }

    [Fact]
    public void ConfirmReservation_UnknownReservation_ReturnsFalse()
    {
        Assert.False(_event.ConfirmReservation(Guid.NewGuid()));
    }

    [Fact]
    public void IsExpired_HeldPastExpiry_TrueOnlyWhileHeld()
    {
        var reservation = ReserveHeld();
        var afterExpiry = reservation.ExpiresAt.AddSeconds(1);

        Assert.False(reservation.IsExpired(reservation.ExpiresAt.AddSeconds(-1)));
        Assert.True(reservation.IsExpired(afterExpiry));

        _event.ConfirmReservation(reservation.Id);
        Assert.False(reservation.IsExpired(afterExpiry));
    }
}
