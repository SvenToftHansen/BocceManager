using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayoffConfigTiebreakerBalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TiebreakerBalls",
                table: "PlayoffConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TiebreakerBalls",
                table: "PlayoffConfigs");
        }
    }
}
