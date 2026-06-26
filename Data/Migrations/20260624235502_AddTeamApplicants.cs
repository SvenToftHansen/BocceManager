using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamApplicants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LookingForTeams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredDivisionId",
                table: "LookingForTeams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegisteredDate",
                table: "LookingForTeams",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    GroupName = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    PreferredDivisionId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PlacedTeamId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Divisions_PreferredDivisionId",
                        column: x => x.PreferredDivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Teams_PlacedTeamId",
                        column: x => x.PlacedTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamApplicantMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedPlayerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicantMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicantMembers_Players_CreatedPlayerId",
                        column: x => x.CreatedPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamApplicantMembers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamApplicantMembers_TeamApplicants_TeamApplicantId",
                        column: x => x.TeamApplicantId,
                        principalTable: "TeamApplicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_PreferredDivisionId",
                table: "LookingForTeams",
                column: "PreferredDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantMembers_CreatedPlayerId",
                table: "TeamApplicantMembers",
                column: "CreatedPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantMembers_PlayerId",
                table: "TeamApplicantMembers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantMembers_TeamApplicantId",
                table: "TeamApplicantMembers",
                column: "TeamApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_LeagueId",
                table: "TeamApplicants",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_PlacedTeamId",
                table: "TeamApplicants",
                column: "PlacedTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_PreferredDivisionId",
                table: "TeamApplicants",
                column: "PreferredDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_SeasonId",
                table: "TeamApplicants",
                column: "SeasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeams_Divisions_PreferredDivisionId",
                table: "LookingForTeams",
                column: "PreferredDivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeams_Divisions_PreferredDivisionId",
                table: "LookingForTeams");

            migrationBuilder.DropTable(
                name: "TeamApplicantMembers");

            migrationBuilder.DropTable(
                name: "TeamApplicants");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeams_PreferredDivisionId",
                table: "LookingForTeams");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LookingForTeams");

            migrationBuilder.DropColumn(
                name: "PreferredDivisionId",
                table: "LookingForTeams");

            migrationBuilder.DropColumn(
                name: "RegisteredDate",
                table: "LookingForTeams");
        }
    }
}
