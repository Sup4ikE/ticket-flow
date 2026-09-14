using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Venue).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Price).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<int>();

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasMany(e => e.Reservations)
            .WithOne()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.Reservations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.StartsAt);
        builder.HasIndex(e => e.Status);
    }
}