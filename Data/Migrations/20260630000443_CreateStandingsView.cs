using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateStandingsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Standings"" AS

-- Aggregates the Scoring view per team per division.
-- Sort order: StandingsPoints DESC → PlusMinus DESC → Wins DESC.
-- DivisionRank reflects that sort within each division.
SELECT
    CAST(ROW_NUMBER() OVER (
        PARTITION BY sc.""DivisionId""
        ORDER BY
            SUM(sc.""WinPTS"" + sc.""TiePTS"" + sc.""LossPTS"" + sc.""ForfeitPoints"") DESC,
            SUM(sc.""PlusMinus"") DESC,
            COUNT(*) FILTER (WHERE sc.""IsWin"") DESC
    ) AS integer)                                                   AS ""DivisionRank"",
    sc.""TeamId"",
    sc.""TeamName"",
    sc.""DivisionId"",
    sc.""SeasonId"",
    sc.""LeagueId"",
    CAST(COUNT(*) AS integer)                                       AS ""GamesPlayed"",
    CAST(COUNT(DISTINCT sc.""ScheduleDivisionsId"") AS integer)      AS ""MatchesPlayed"",
    CAST(COUNT(*) FILTER (WHERE sc.""IsWin"")                AS integer) AS ""Wins"",
    CAST(COUNT(*) FILTER (WHERE sc.""IsTie"")                AS integer) AS ""Ties"",
    CAST(COUNT(*) FILTER (WHERE sc.""IsLoss""
                             AND NOT sc.""IsForfeit"")       AS integer) AS ""Losses"",
    CAST(COUNT(*) FILTER (WHERE sc.""IsForfeit"")            AS integer) AS ""Forfeits"",
    CAST(SUM(sc.""WinPTS"" + sc.""TiePTS"" + sc.""LossPTS"" + sc.""ForfeitPoints"")
                                                             AS integer) AS ""StandingsPoints"",
    CAST(SUM(sc.""PlusMinus"")   AS integer)                            AS ""PlusMinus"",
    CAST(SUM(sc.""PointsFor"")   AS integer)                            AS ""PointsFor"",
    CAST(SUM(sc.""PointsAgainst"") AS integer)                          AS ""PointsAgainst""
FROM ""Scoring"" sc
GROUP BY sc.""TeamId"", sc.""TeamName"", sc.""DivisionId"", sc.""SeasonId"", sc.""LeagueId"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");
        }
    }
}
