using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceLookingForTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeams_Divisions_PreferredDivisionId",
                table: "LookingForTeams");

            migrationBuilder.RenameColumn(
                name: "PreferredDivisionId",
                table: "LookingForTeams",
                newName: "PreferredTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeams_PreferredDivisionId",
                table: "LookingForTeams",
                newName: "IX_LookingForTeams_PreferredTeamId");

            migrationBuilder.AddColumn<int>(
                name: "LookingForTeamGroupId",
                table: "LookingForTeams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LookingForTeamDivisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LookingForTeamId = table.Column<int>(type: "integer", nullable: false),
                    DivisionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookingForTeamDivisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookingForTeamDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LookingForTeamDivisions_LookingForTeams_LookingForTeamId",
                        column: x => x.LookingForTeamId,
                        principalTable: "LookingForTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LookingForTeamGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookingForTeamGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookingForTeamGroups_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LookingForTeamGroups_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_LookingForTeamGroupId",
                table: "LookingForTeams",
                column: "LookingForTeamGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamDivisions_DivisionId",
                table: "LookingForTeamDivisions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamDivisions_LookingForTeamId_DivisionId",
                table: "LookingForTeamDivisions",
                columns: new[] { "LookingForTeamId", "DivisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamGroups_LeagueId",
                table: "LookingForTeamGroups",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamGroups_SeasonId",
                table: "LookingForTeamGroups",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeams_LookingForTeamGroups_LookingForTeamGroupId",
                table: "LookingForTeams",
                column: "LookingForTeamGroupId",
                principalTable: "LookingForTeamGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeams_Teams_PreferredTeamId",
                table: "LookingForTeams",
                column: "PreferredTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeams_LookingForTeamGroups_LookingForTeamGroupId",
                table: "LookingForTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeams_Teams_PreferredTeamId",
                table: "LookingForTeams");

            migrationBuilder.DropTable(
                name: "LookingForTeamDivisions");

            migrationBuilder.DropTable(
                name: "LookingForTeamGroups");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeams_LookingForTeamGroupId",
                table: "LookingForTeams");

            migrationBuilder.DropColumn(
                name: "LookingForTeamGroupId",
                table: "LookingForTeams");

            migrationBuilder.RenameColumn(
                name: "PreferredTeamId",
                table: "LookingForTeams",
                newName: "PreferredDivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeams_PreferredTeamId",
                table: "LookingForTeams",
                newName: "IX_LookingForTeams_PreferredDivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeams_Divisions_PreferredDivisionId",
                table: "LookingForTeams",
                column: "PreferredDivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
