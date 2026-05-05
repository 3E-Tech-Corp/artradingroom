using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArTrading.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBacktestTicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "BacktestRuns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "BacktestRuns");
        }
    }
}
