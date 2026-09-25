namespace TicketFlow.Booking.Domain.Enums;

public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    // 4 was Completed: no transition ever reached it, so it was removed. Status is persisted as int - don't reuse 4.
    AwaitingPayment = 5
}
