namespace TicketFlow.Booking.Application.Exceptions;

public class EventsServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
