using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArTrading.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketDataBars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDataBars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticker = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataBars", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataBars_Ticker",
                table: "MarketDataBars",
                column: "Ticker");

            migrationBuilder.CreateIndex(
                name: "IX_MarketDataBars_Ticker_Date",
                table: "MarketDataBars",
                columns: new[] { "Ticker", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketDataBars");
        }
    }
}
