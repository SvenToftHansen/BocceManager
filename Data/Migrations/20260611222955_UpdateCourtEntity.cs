using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourtEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Courts",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "CourtName",
                table: "Courts",
                newName: "CourtLetter");

            migrationBuilder.AddColumn<int>(
                name: "CourtNumber",
                table: "Courts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourtNumber",
                table: "Courts");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Courts",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CourtLetter",
                table: "Courts",
                newName: "CourtName");
        }
    }
}
