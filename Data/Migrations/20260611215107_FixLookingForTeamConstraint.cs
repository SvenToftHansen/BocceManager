using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixLookingForTeamConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LookingForTeams_LeagueId_PlayerId",
                table: "LookingForTeams");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_LeagueId_PlayerId_SeasonId",
                table: "LookingForTeams",
                columns: new[] { "LeagueId", "PlayerId", "SeasonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LookingForTeams_LeagueId_PlayerId_SeasonId",
                table: "LookingForTeams");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_LeagueId_PlayerId",
                table: "LookingForTeams",
                columns: new[] { "LeagueId", "PlayerId" },
                unique: true);
        }
    }
}
