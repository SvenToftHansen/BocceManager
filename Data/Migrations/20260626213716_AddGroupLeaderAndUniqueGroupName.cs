using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupLeaderAndUniqueGroupName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamGroups_LeagueId",
                table: "LookingForTeamGroups");

            migrationBuilder.AddColumn<int>(
                name: "GroupLeaderId",
                table: "LookingForTeamGroups",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamGroups_GroupLeaderId",
                table: "LookingForTeamGroups",
                column: "GroupLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamGroups_LeagueId_SeasonId_Name",
                table: "LookingForTeamGroups",
                columns: new[] { "LeagueId", "SeasonId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamGroups_LookingForTeams_GroupLeaderId",
                table: "LookingForTeamGroups",
                column: "GroupLeaderId",
                principalTable: "LookingForTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamGroups_LookingForTeams_GroupLeaderId",
                table: "LookingForTeamGroups");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamGroups_GroupLeaderId",
                table: "LookingForTeamGroups");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamGroups_LeagueId_SeasonId_Name",
                table: "LookingForTeamGroups");

            migrationBuilder.DropColumn(
                name: "GroupLeaderId",
                table: "LookingForTeamGroups");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamGroups_LeagueId",
                table: "LookingForTeamGroups",
                column: "LeagueId");
        }
    }
}
