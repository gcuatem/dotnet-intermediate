using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeatRow",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatRow",
                table: "Seats");
        }
    }
}
