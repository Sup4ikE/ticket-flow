namespace TicketFlow.Booking.Domain.Enums;

// Persisted by name (see BookingConfiguration) and sent as-is in BookingCancelled.Reason, so the names are the
// wire format - rename with care. The last four mirror Contracts' ReservationFailureReason one-to-one.
public enum BookingCancellationReason
{
    UserCancelled = 1,
    ReservationExpired = 2,
    NotEnoughSeats = 3,
    EventNotFound = 4,
    EventCancelled = 5,
    EventAlreadyStarted = 6
}
