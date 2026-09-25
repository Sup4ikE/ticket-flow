using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Events.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string content)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("Type is required.");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Content is required.");

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed() => ProcessedAt = DateTime.UtcNow;

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
