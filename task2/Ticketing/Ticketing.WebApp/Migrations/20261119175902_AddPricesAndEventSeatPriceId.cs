using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPricesAndEventSeatPriceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "PriceId",
                table: "EventSeats",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [EventSeats] SET [PriceId] = NEWID();");

            migrationBuilder.Sql(
                """
                INSERT INTO [Prices] ([Id], [Name], [Price])
                SELECT [PriceId], N'Standard', [Price]
                FROM [EventSeats];
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PriceId",
                table: "EventSeats",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeats_PriceId",
                table: "EventSeats",
                column: "PriceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventSeats_Prices_PriceId",
                table: "EventSeats",
                column: "PriceId",
                principalTable: "Prices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "Price",
                table: "EventSeats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "EventSeats",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE [EventSeats]
                SET [Price] = [Prices].[Price]
                FROM [EventSeats]
                INNER JOIN [Prices] ON [Prices].[Id] = [EventSeats].[PriceId];
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_EventSeats_Prices_PriceId",
                table: "EventSeats");

            migrationBuilder.DropIndex(
                name: "IX_EventSeats_PriceId",
                table: "EventSeats");

            migrationBuilder.DropColumn(
                name: "PriceId",
                table: "EventSeats");

            migrationBuilder.DropTable(
                name: "Prices");
        }
    }
}
