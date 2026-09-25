namespace TicketFlow.Events.Application.Configuration;

public class ReservationSettings
{
    public int? HoldDurationMinutes { get; set; }
    public int? HoldDurationSeconds { get; set; }

    public TimeSpan HoldDuration => HoldDurationSeconds is { } seconds
        ? TimeSpan.FromSeconds(seconds)
        : TimeSpan.FromMinutes(HoldDurationMinutes ?? 10);
}
