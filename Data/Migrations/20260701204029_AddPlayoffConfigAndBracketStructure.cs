using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayoffConfigAndBracketStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationBetweenRoundsMins",
                table: "PlayoffRounds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "PlayoffRounds",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "PlayoffRounds",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BracketSlot",
                table: "PlayoffMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBye",
                table: "PlayoffMatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NextMatchId",
                table: "PlayoffMatches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NextMatchIsTop",
                table: "PlayoffMatches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PlayoffConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    MatchDurationMins = table.Column<int>(type: "integer", nullable: false),
                    DisplayMode = table.Column<string>(type: "text", nullable: false),
                    IsGenerated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffConfigs_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffSeedings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffSeedings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffSeedings_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoffSeedings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffDayParams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayoffConfigId = table.Column<int>(type: "integer", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    GameDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DurationBetweenRoundsMins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffDayParams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffDayParams_PlayoffConfigs_PlayoffConfigId",
                        column: x => x.PlayoffConfigId,
                        principalTable: "PlayoffConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_NextMatchId",
                table: "PlayoffMatches",
                column: "NextMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffConfigs_SeasonId",
                table: "PlayoffConfigs",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffDayParams_PlayoffConfigId",
                table: "PlayoffDayParams",
                column: "PlayoffConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeedings_SeasonId_Seed",
                table: "PlayoffSeedings",
                columns: new[] { "SeasonId", "Seed" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffSeedings_TeamId",
                table: "PlayoffSeedings",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoffMatches_PlayoffMatches_NextMatchId",
                table: "PlayoffMatches",
                column: "NextMatchId",
                principalTable: "PlayoffMatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoffMatches_PlayoffMatches_NextMatchId",
                table: "PlayoffMatches");

            migrationBuilder.DropTable(
                name: "PlayoffDayParams");

            migrationBuilder.DropTable(
                name: "PlayoffSeedings");

            migrationBuilder.DropTable(
                name: "PlayoffConfigs");

            migrationBuilder.DropIndex(
                name: "IX_PlayoffMatches_NextMatchId",
                table: "PlayoffMatches");

            migrationBuilder.DropColumn(
                name: "DurationBetweenRoundsMins",
                table: "PlayoffRounds");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "PlayoffRounds");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "PlayoffRounds");

            migrationBuilder.DropColumn(
                name: "BracketSlot",
                table: "PlayoffMatches");

            migrationBuilder.DropColumn(
                name: "IsBye",
                table: "PlayoffMatches");

            migrationBuilder.DropColumn(
                name: "NextMatchId",
                table: "PlayoffMatches");

            migrationBuilder.DropColumn(
                name: "NextMatchIsTop",
                table: "PlayoffMatches");
        }
    }
}
