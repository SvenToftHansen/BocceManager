using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTeamFoundWithSeasonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TeamFound",
                table: "LookingForTeams",
                newName: "SeasonId");

            // Discard historical team found values - SeasonId is now for binding to a specific season
            migrationBuilder.Sql(@"UPDATE ""LookingForTeams"" SET ""SeasonId"" = NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_SeasonId",
                table: "LookingForTeams",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeams_Seasons_SeasonId",
                table: "LookingForTeams",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeams_Seasons_SeasonId",
                table: "LookingForTeams");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeams_SeasonId",
                table: "LookingForTeams");

            migrationBuilder.RenameColumn(
                name: "SeasonId",
                table: "LookingForTeams",
                newName: "TeamFound");
        }
    }
}
