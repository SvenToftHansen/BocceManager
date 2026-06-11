using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamSortOrderRemoveIsByeTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all bye teams before removing the column
            migrationBuilder.Sql("DELETE FROM \"Teams\" WHERE \"IsByeTeam\" = true;");

            migrationBuilder.DropColumn(
                name: "IsByeTeam",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "SortOrder",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Teams");

            migrationBuilder.AddColumn<bool>(
                name: "IsByeTeam",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
