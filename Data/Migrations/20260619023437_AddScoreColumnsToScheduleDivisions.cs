using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreColumnsToScheduleDivisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Team1Score1",
                table: "ScheduleDivisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team1Score2",
                table: "ScheduleDivisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2Score1",
                table: "ScheduleDivisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2Score2",
                table: "ScheduleDivisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create Stats view
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Stats"" AS
SELECT
    ROW_NUMBER() OVER (ORDER BY sd.""Id"", CASE WHEN t.""Id"" = sd.""Team1Id"" THEN 1 ELSE 2 END) AS ""Id"",
    sd.""Id"" AS ""ScheduleDivisionsId"",
    CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team1Id"" ELSE sd.""Team2Id"" END AS ""TeamId"",
    -- PlusMinus: sum of scores minus opponent (only count valid games with 12 or -1)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team1Score1"" - sd.""Team2Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team1Score2"" - sd.""Team2Score2"" ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team2Score1"" - sd.""Team1Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team2Score2"" - sd.""Team1Score2"" ELSE 0 END)
    END AS ""PlusMinus"",
    -- Wins: only count games where team scored 12
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN sd.""Team1Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team1Score2"" = 12 THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN sd.""Team2Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team2Score2"" = 12 THEN 1 ELSE 0 END)
    END AS ""Wins"",
    -- Ties: not applicable in bocce (one team must reach 12 or forfeit)
    0 AS ""Ties"",
    -- Losses: opponent scored 12 or we forfeited (had -1 while opponent didn't)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN sd.""Team2Score1"" = 12 OR (sd.""Team1Score1"" = -1 AND sd.""Team2Score1"" != -1) THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team2Score2"" = 12 OR (sd.""Team1Score2"" = -1 AND sd.""Team2Score2"" != -1) THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN sd.""Team1Score1"" = 12 OR (sd.""Team2Score1"" = -1 AND sd.""Team1Score1"" != -1) THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team1Score2"" = 12 OR (sd.""Team2Score2"" = -1 AND sd.""Team1Score2"" != -1) THEN 1 ELSE 0 END)
    END AS ""Losses"",
    -- Forfeits: count of games where team had -1
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN sd.""Team1Score1"" = -1 THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team1Score2"" = -1 THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN sd.""Team2Score1"" = -1 THEN 1 ELSE 0 END) +
            (CASE WHEN sd.""Team2Score2"" = -1 THEN 1 ELSE 0 END)
    END AS ""Forfeits"",
    -- Points: 2 for each win (forfeit points applied at application layer)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN sd.""Team1Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN sd.""Team1Score2"" = 12 THEN 2 ELSE 0 END)
        ELSE
            (CASE WHEN sd.""Team2Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN sd.""Team2Score2"" = 12 THEN 2 ELSE 0 END)
    END AS ""Points""
FROM ""ScheduleDivisions"" sd
CROSS JOIN LATERAL (VALUES (sd.""Team1Id""), (sd.""Team2Id"")) AS t(""Id"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Stats view
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Stats"";");

            migrationBuilder.DropColumn(
                name: "Team1Score1",
                table: "ScheduleDivisions");

            migrationBuilder.DropColumn(
                name: "Team1Score2",
                table: "ScheduleDivisions");

            migrationBuilder.DropColumn(
                name: "Team2Score1",
                table: "ScheduleDivisions");

            migrationBuilder.DropColumn(
                name: "Team2Score2",
                table: "ScheduleDivisions");
        }
    }
}
