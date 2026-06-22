using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayoffTiebreakerFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayoffTiebreakerEnd",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "TimeslotDriven",
                table: "Seasons");

            migrationBuilder.AddColumn<string>(
                name: "PlayoffTiebreakerFormat",
                table: "Seasons",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayoffTiebreakerFormat",
                table: "Seasons");

            migrationBuilder.AddColumn<bool>(
                name: "PlayoffTiebreakerEnd",
                table: "Seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TimeslotDriven",
                table: "Seasons",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
