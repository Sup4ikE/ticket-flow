using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReasonToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "bookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "bookings");
        }
    }
}
