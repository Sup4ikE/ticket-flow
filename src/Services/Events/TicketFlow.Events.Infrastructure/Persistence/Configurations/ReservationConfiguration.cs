using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketFlow.Events.Domain.Entities;

namespace TicketFlow.Events.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EventId).IsRequired();
        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.Quantity).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasIndex(r => r.BookingId);
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });
    }
}