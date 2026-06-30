using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingsSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Standings"" AS

-- Step 1: aggregate Scoring rows per team per division.
WITH agg AS (
    SELECT
        sc.""TeamId"",
        sc.""TeamName"",
        sc.""DivisionId"",
        sc.""SeasonId"",
        sc.""LeagueId"",
        CAST(COUNT(*)                                                           AS integer) AS ""GamesPlayed"",
        CAST(COUNT(DISTINCT sc.""ScheduleDivisionsId"")                          AS integer) AS ""MatchesPlayed"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsWin"")                               AS integer) AS ""Wins"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsTie"")                               AS integer) AS ""Ties"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsLoss"" AND NOT sc.""IsForfeit"")      AS integer) AS ""Losses"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsForfeit"")                           AS integer) AS ""Forfeits"",
        CAST(SUM(sc.""WinPTS"" + sc.""TiePTS"" + sc.""LossPTS"" + sc.""ForfeitPoints"") AS integer) AS ""StandingsPoints"",
        CAST(SUM(sc.""PlusMinus"")                                              AS integer) AS ""PlusMinus"",
        CAST(SUM(sc.""PointsFor"")                                              AS integer) AS ""PointsFor"",
        CAST(SUM(sc.""PointsAgainst"")                                          AS integer) AS ""PointsAgainst""
    FROM ""Scoring"" sc
    GROUP BY sc.""TeamId"", sc.""TeamName"", sc.""DivisionId"", sc.""SeasonId"", sc.""LeagueId""
),

-- Step 2: add division-level rankings and join Season for FirstPlaceGuaranteed flag.
--   DivisionRank: DENSE_RANK — tied teams share a rank number (for display).
--   DivisionSeed: ROW_NUMBER — always unique, same sort order (for bracket assignment).
ranked AS (
    SELECT
        agg.*,
        s.""FirstPlaceGuaranteed"",
        CAST(DENSE_RANK() OVER (
            PARTITION BY agg.""DivisionId""
            ORDER BY agg.""StandingsPoints"" DESC, agg.""PlusMinus"" DESC, agg.""Wins"" DESC
        ) AS integer) AS ""DivisionRank"",
        CAST(ROW_NUMBER() OVER (
            PARTITION BY agg.""DivisionId""
            ORDER BY agg.""StandingsPoints"" DESC, agg.""PlusMinus"" DESC, agg.""Wins"" DESC
        ) AS integer) AS ""DivisionSeed""
    FROM agg
    JOIN ""Seasons"" s ON s.""Id"" = agg.""SeasonId""
)

-- Step 3: add SeasonSeed — rank across all divisions in the season.
--   FirstPlaceGuaranteed=true:  group by DivisionSeed first (all 1st-place teams seed
--                                before all 2nd-place teams, etc.), then sort within
--                                each group by StandingsPoints → PlusMinus → Wins.
--   FirstPlaceGuaranteed=false: rank all teams together by StandingsPoints → PlusMinus → Wins,
--                                ignoring division placement.
SELECT
    ranked.""DivisionRank"",
    ranked.""DivisionSeed"",
    CASE
        WHEN ranked.""FirstPlaceGuaranteed"" THEN
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    ranked.""DivisionSeed""      ASC,
                    ranked.""StandingsPoints""   DESC,
                    ranked.""PlusMinus""         DESC,
                    ranked.""Wins""              DESC
            ) AS integer)
        ELSE
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    ranked.""StandingsPoints""   DESC,
                    ranked.""PlusMinus""         DESC,
                    ranked.""Wins""              DESC
            ) AS integer)
    END AS ""SeasonSeed"",
    ranked.""TeamId"",
    ranked.""TeamName"",
    ranked.""DivisionId"",
    ranked.""SeasonId"",
    ranked.""LeagueId"",
    ranked.""GamesPlayed"",
    ranked.""MatchesPlayed"",
    ranked.""Wins"",
    ranked.""Ties"",
    ranked.""Losses"",
    ranked.""Forfeits"",
    ranked.""StandingsPoints"",
    ranked.""PlusMinus"",
    ranked.""PointsFor"",
    ranked.""PointsAgainst""
FROM ranked;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");
        }
    }
}
