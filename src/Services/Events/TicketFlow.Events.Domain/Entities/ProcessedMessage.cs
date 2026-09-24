namespace TicketFlow.Events.Domain.Entities;

public class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedMessage() { }

    public static ProcessedMessage Create(Guid messageId) => new()
    {
        MessageId = messageId,
        ProcessedAt = DateTime.UtcNow
    };
}