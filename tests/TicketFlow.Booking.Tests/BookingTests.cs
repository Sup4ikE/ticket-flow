using TicketFlow.Booking.Domain.Enums;
using TicketFlow.Booking.Domain.Exceptions;
using BookingEntity = TicketFlow.Booking.Domain.Entities.Booking;

namespace TicketFlow.Booking.Tests;

public class BookingTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private const string UserEmail = "user@example.com";
    private const string EventTitle = "Kyiv Web3 Meetup";
    private static readonly DateTime EventStartsAt = DateTime.UtcNow.AddDays(7);

    private static BookingEntity CreateBooking(int quantity = 2, decimal pricePerTicket = 250m) =>
        BookingEntity.Create(EventId, UserEmail, quantity, EventTitle, EventStartsAt, pricePerTicket);

    // Drives a fresh booking through the real transitions, so every test starts from a reachable state.
    private static BookingEntity CreateBookingIn(BookingStatus status)
    {
        var booking = CreateBooking();

        switch (status)
        {
            case BookingStatus.Pending:
                break;
            case BookingStatus.AwaitingPayment:
                booking.MarkAwaitingPayment(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10));
                break;
            case BookingStatus.Confirmed:
                booking.MarkAwaitingPayment(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10));
                booking.Pay();
                break;
            case BookingStatus.Cancelled:
                booking.Cancel(BookingCancellationReason.UserCancelled);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "No domain transition reaches this status");
        }

        Assert.Equal(status, booking.Status);
        return booking;
    }

    [Fact]
    public void Create_ValidInput_PopulatesEveryField()
    {
        var before = DateTime.UtcNow;

        var booking = BookingEntity.Create(EventId, UserEmail, 3, EventTitle, EventStartsAt, 125.50m);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(EventId, booking.EventId);
        Assert.Equal(UserEmail, booking.UserEmail);
        Assert.Equal(3, booking.Quantity);
        Assert.Equal(EventTitle, booking.EventTitle);
        Assert.Equal(EventStartsAt, booking.EventStartsAt);
        Assert.Equal(125.50m, booking.PricePerTicket);
        Assert.Equal(376.50m, booking.TotalPrice);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.InRange(booking.CreatedAt, before, DateTime.UtcNow);
        Assert.Null(booking.ReservationId);
        Assert.Null(booking.HoldExpiresAt);
    }

    [Fact]
    public void Create_TwoBookings_GetDistinctIds()
    {
        Assert.NotEqual(CreateBooking().Id, CreateBooking().Id);
    }

    [Fact]
    public void Create_FreeTicket_Succeeds()
    {
        var booking = CreateBooking(pricePerTicket: 0m);

        Assert.Equal(0m, booking.PricePerTicket);
        Assert.Equal(0m, booking.TotalPrice);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Theory]
    [InlineData(0, 100, UserEmail, EventTitle)]
    [InlineData(-1, 100, UserEmail, EventTitle)]
    [InlineData(1, -0.01, UserEmail, EventTitle)]
    [InlineData(1, 100, "", EventTitle)]
    [InlineData(1, 100, "   ", EventTitle)]
    [InlineData(1, 100, UserEmail, "")]
    [InlineData(1, 100, UserEmail, "   ")]
    [InlineData(101, 100, UserEmail, EventTitle)] // total 10,100 > 10,000 cap
    public void Create_InvalidInput_ThrowsDomainException(int quantity, double pricePerTicket, string userEmail, string eventTitle)
    {
        Assert.Throws<DomainException>(() =>
            BookingEntity.Create(EventId, userEmail, quantity, eventTitle, EventStartsAt, (decimal)pricePerTicket));
    }

    [Fact]
    public void Create_NegativePrice_MessageSaysNegativeNotZero()
    {
        var ex = Assert.Throws<DomainException>(() => CreateBooking(pricePerTicket: -1m));

        Assert.Equal("Ціна квитка не може бути відʼємною.", ex.Message);
    }

    [Fact]
    public void Create_TotalExactlyAtCap_Succeeds()
    {
        var booking = CreateBooking(quantity: 100, pricePerTicket: 100m);

        Assert.Equal(10_000m, booking.TotalPrice);
    }

    [Fact]
    public void Create_EventInThePast_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            BookingEntity.Create(EventId, UserEmail, 1, EventTitle, DateTime.UtcNow.AddMinutes(-1), 100m));
    }

    [Fact]
    public void MarkAwaitingPayment_FromPending_SetsStatusReservationAndHold()
    {
        var booking = CreateBooking();
        var reservationId = Guid.NewGuid();
        var holdExpiresAt = DateTime.UtcNow.AddMinutes(10);

        booking.MarkAwaitingPayment(reservationId, holdExpiresAt);

        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.Equal(reservationId, booking.ReservationId);
        Assert.Equal(holdExpiresAt, booking.HoldExpiresAt);
    }

    [Theory]
    [InlineData(BookingStatus.AwaitingPayment)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public void MarkAwaitingPayment_FromNonPending_ThrowsAndLeavesStateUntouched(BookingStatus status)
    {
        var booking = CreateBookingIn(status);
        var reservationIdBefore = booking.ReservationId;

        var ex = Assert.Throws<DomainException>(() =>
            booking.MarkAwaitingPayment(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10)));

        Assert.Contains(status.ToString(), ex.Message);
        Assert.Equal(status, booking.Status);
        Assert.Equal(reservationIdBefore, booking.ReservationId);
    }

    [Fact]
    public void Pay_FromAwaitingPayment_Confirms()
    {
        var booking = CreateBookingIn(BookingStatus.AwaitingPayment);

        booking.Pay();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    // Regression: the old Confirm() guarded on Pending (plus an unreachable second check). Pay() must accept
    // AwaitingPayment only - in particular a Pending booking without a reservation must not be payable.
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public void Pay_FromNonAwaitingPayment_ThrowsWithCurrentStatusInMessage(BookingStatus status)
    {
        var booking = CreateBookingIn(status);

        var ex = Assert.Throws<DomainException>(booking.Pay);

        Assert.Contains($"current status is {status}", ex.Message);
        Assert.Equal(status, booking.Status);
    }

    [Fact]
    public void Pay_DoesNotRecalculateTotalPrice()
    {
        var booking = CreateBooking(quantity: 3, pricePerTicket: 99.99m);
        var totalAtCreate = booking.TotalPrice;
        booking.MarkAwaitingPayment(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10));

        booking.Pay();

        Assert.Equal(299.97m, totalAtCreate);
        Assert.Equal(totalAtCreate, booking.TotalPrice);
        Assert.Equal(99.99m, booking.PricePerTicket);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.AwaitingPayment)]
    public void Cancel_FromPendingOrAwaitingPayment_Cancels(BookingStatus status)
    {
        var booking = CreateBookingIn(status);

        booking.Cancel(BookingCancellationReason.UserCancelled);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, BookingCancellationReason.NotEnoughSeats)]
    [InlineData(BookingStatus.Pending, BookingCancellationReason.EventCancelled)]
    [InlineData(BookingStatus.AwaitingPayment, BookingCancellationReason.ReservationExpired)]
    [InlineData(BookingStatus.AwaitingPayment, BookingCancellationReason.UserCancelled)]
    public void Cancel_RecordsReason(BookingStatus status, BookingCancellationReason reason)
    {
        var booking = CreateBookingIn(status);

        booking.Cancel(reason);

        Assert.Equal(reason, booking.CancellationReason);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.AwaitingPayment)]
    [InlineData(BookingStatus.Confirmed)]
    public void CancellationReason_NotCancelled_IsNull(BookingStatus status)
    {
        Assert.Null(CreateBookingIn(status).CancellationReason);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public void Cancel_FromTerminalStatus_ThrowsWithCurrentStatusInMessage(BookingStatus status)
    {
        var booking = CreateBookingIn(status);
        var reasonBefore = booking.CancellationReason;

        var ex = Assert.Throws<DomainException>(() => booking.Cancel(BookingCancellationReason.ReservationExpired));

        Assert.Contains($"current status is {status}", ex.Message);
        // A late expiry must not overwrite why the booking actually ended (e.g. the user's own cancel).
        Assert.Equal(reasonBefore, booking.CancellationReason);
    }

    [Fact]
    public void Cancel_AfterPay_ThrowsAndStaysConfirmed()
    {
        var booking = CreateBookingIn(BookingStatus.AwaitingPayment);
        booking.Pay();

        Assert.Throws<DomainException>(() => booking.Cancel(BookingCancellationReason.ReservationExpired));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.CancellationReason);
    }
}
