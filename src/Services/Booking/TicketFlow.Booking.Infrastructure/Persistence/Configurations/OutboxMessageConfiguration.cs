using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Booking.Domain.Entities;

namespace TicketFlow.Booking.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);

        builder.HasIndex(m => m.ProcessedAt);
    }
}