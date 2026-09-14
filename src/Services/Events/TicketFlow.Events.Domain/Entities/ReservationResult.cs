using TicketFlow.Contracts.Events;

namespace TicketFlow.Events.Domain.Entities;

public record ReservationResult
{
    public Reservation? Reservation { get; private init; }
    public ReservationFailureReason? FailureReason { get; private init; }
    public bool IsSuccess => Reservation is not null;

    public static ReservationResult Success(Reservation reservation) =>
        new() { Reservation = reservation };

    public static ReservationResult Failure(ReservationFailureReason reason) =>
        new() { FailureReason = reason };
}