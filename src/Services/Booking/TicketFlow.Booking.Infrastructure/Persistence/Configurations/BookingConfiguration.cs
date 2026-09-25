using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Booking.Domain.Entities;

namespace TicketFlow.Booking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Domain.Entities.Booking>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserEmail).HasMaxLength(320).IsRequired();
        builder.Property(b => b.Status).HasConversion<int>();
        // By name, not int: it's the same string that goes out in BookingCancelled.Reason and reads fine in SQL.
        builder.Property(b => b.CancellationReason).HasConversion<string>().HasMaxLength(50);
        builder.Property(b => b.EventTitle).HasMaxLength(200).IsRequired();
        builder.Property(b => b.PricePerTicket).HasPrecision(18, 2);
        builder.Property(b => b.TotalPrice).HasPrecision(18, 2);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasIndex(b => b.EventId);
        builder.HasIndex(b => b.UserEmail);
        builder.HasIndex(b => b.Status);
    }
}