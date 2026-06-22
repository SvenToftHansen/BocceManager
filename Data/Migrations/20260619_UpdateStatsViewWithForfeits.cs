using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatsViewWithForfeits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing Stats view
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Stats"";");

            // Recreate Stats view with Forfeits column
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Stats"" AS
SELECT
    ROW_NUMBER() OVER (ORDER BY sd.""Id"", CASE WHEN t.""Id"" = sd.""Team1Id"" THEN 1 ELSE 2 END) AS ""Id"",
    sd.""Id"" AS ""ScheduleDivisionsId"",
    sd.""DivisionId"",
    sd.""TemplateWeekNumber"" AS ""WeekNumber"",
    CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team1Id"" ELSE sd.""Team2Id"" END AS ""TeamId"",
    -- PlusMinus: sum of scores minus opponent (only count valid games: one team has 12 or one/both have -1)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team1Score1"" - sd.""Team2Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team1Score2"" - sd.""Team2Score2"" ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team2Score1"" - sd.""Team1Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team2Score2"" - sd.""Team1Score2"" ELSE 0 END)
    END AS ""PlusMinus"",
    -- Wins: only count valid games where team scored 12
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team1Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team1Score2"" = 12 THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team2Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team2Score2"" = 12 THEN 1 ELSE 0 END)
    END AS ""Wins"",
    -- Ties: not applicable in bocce (0-0 is unplayed, not a tie)
    0 AS ""Ties"",
    -- Losses: only count valid games where opponent scored 12 or team forfeited (had -1 while opponent didn't)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND (sd.""Team2Score1"" = 12 OR (sd.""Team1Score1"" = -1 AND sd.""Team2Score1"" != -1)) THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND (sd.""Team2Score2"" = 12 OR (sd.""Team1Score2"" = -1 AND sd.""Team2Score2"" != -1)) THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND (sd.""Team1Score1"" = 12 OR (sd.""Team2Score1"" = -1 AND sd.""Team1Score1"" != -1)) THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND (sd.""Team1Score2"" = 12 OR (sd.""Team2Score2"" = -1 AND sd.""Team1Score2"" != -1)) THEN 1 ELSE 0 END)
    END AS ""Losses"",
    -- Forfeits: count of valid games where team had -1
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team1Score1"" = -1 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team1Score2"" = -1 THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team2Score1"" = -1 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team2Score2"" = -1 THEN 1 ELSE 0 END)
    END AS ""Forfeits"",
    -- Points: 2 for each valid game won (forfeit points applied at application layer)
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team1Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team1Score2"" = 12 THEN 2 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team2Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team2Score2"" = 12 THEN 2 ELSE 0 END)
    END AS ""Points""
FROM ""ScheduleDivisions"" sd
CROSS JOIN LATERAL (VALUES (sd.""Team1Id""), (sd.""Team2Id"")) AS t(""Id"")
WHERE
    -- Only include played games (at least one valid score)
    (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1 OR
     sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Stats view
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Stats"";");

            // Recreate Stats view without Forfeits column (previous version)
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Stats"" AS
SELECT
    ROW_NUMBER() OVER (ORDER BY sd.""Id"", CASE WHEN t.""Id"" = sd.""Team1Id"" THEN 1 ELSE 2 END) AS ""Id"",
    sd.""Id"" AS ""ScheduleDivisionsId"",
    sd.""DivisionId"",
    sd.""TemplateWeekNumber"" AS ""WeekNumber"",
    CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team1Id"" ELSE sd.""Team2Id"" END AS ""TeamId"",
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team1Score1"" - sd.""Team2Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team1Score2"" - sd.""Team2Score2"" ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) THEN sd.""Team2Score1"" - sd.""Team1Score1"" ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) THEN sd.""Team2Score2"" - sd.""Team1Score2"" ELSE 0 END)
    END AS ""PlusMinus"",
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team1Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team1Score2"" = 12 THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team2Score1"" = 12 THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team2Score2"" = 12 THEN 1 ELSE 0 END)
    END AS ""Wins"",
    0 AS ""Ties"",
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND (sd.""Team2Score1"" = 12 OR (sd.""Team1Score1"" = -1 AND sd.""Team2Score1"" != -1)) THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND (sd.""Team2Score2"" = 12 OR (sd.""Team1Score2"" = -1 AND sd.""Team2Score2"" != -1)) THEN 1 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND (sd.""Team1Score1"" = 12 OR (sd.""Team2Score1"" = -1 AND sd.""Team1Score1"" != -1)) THEN 1 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND (sd.""Team1Score2"" = 12 OR (sd.""Team2Score2"" = -1 AND sd.""Team1Score2"" != -1)) THEN 1 ELSE 0 END)
    END AS ""Losses"",
    CASE
        WHEN t.""Id"" = sd.""Team1Id"" THEN
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team1Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team1Score2"" = 12 THEN 2 ELSE 0 END)
        ELSE
            (CASE WHEN (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1) AND sd.""Team2Score1"" = 12 THEN 2 ELSE 0 END) +
            (CASE WHEN (sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1) AND sd.""Team2Score2"" = 12 THEN 2 ELSE 0 END)
    END AS ""Points""
FROM ""ScheduleDivisions"" sd
CROSS JOIN LATERAL (VALUES (sd.""Team1Id""), (sd.""Team2Id"")) AS t(""Id"")
WHERE
    -- Only include played games (at least one valid score)
    (sd.""Team1Score1"" = 12 OR sd.""Team2Score1"" = 12 OR sd.""Team1Score1"" = -1 OR sd.""Team2Score1"" = -1 OR
     sd.""Team1Score2"" = 12 OR sd.""Team2Score2"" = 12 OR sd.""Team1Score2"" = -1 OR sd.""Team2Score2"" = -1);
");
        }
    }
}
