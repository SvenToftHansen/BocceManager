using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonForfeitPlusMinusParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ForfeitOpponentPlusMinus",
                table: "Seasons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ForfeitPlusMinus",
                table: "Seasons",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForfeitOpponentPlusMinus",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "ForfeitPlusMinus",
                table: "Seasons");
        }
    }
}
