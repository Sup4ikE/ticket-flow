namespace TicketFlow.Events.Tests;

// Minimal stand-in for Microsoft.Extensions.TimeProvider.Testing's FakeTimeProvider - only "now" matters to the domain.
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
