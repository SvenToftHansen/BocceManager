using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingsH2HTiebreaker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Standings"" AS

-- ── Step 1: aggregate Scoring per team per division ───────────────────────────
WITH agg AS (
    SELECT
        sc.""TeamId"",
        sc.""TeamName"",
        sc.""DivisionId"",
        sc.""SeasonId"",
        sc.""LeagueId"",
        CAST(COUNT(*)                                                             AS integer) AS ""GamesPlayed"",
        CAST(COUNT(DISTINCT sc.""ScheduleDivisionsId"")                            AS integer) AS ""MatchesPlayed"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsWin"")                                 AS integer) AS ""Wins"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsTie"")                                 AS integer) AS ""Ties"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsLoss"" AND NOT sc.""IsForfeit"")        AS integer) AS ""Losses"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsForfeit"")                             AS integer) AS ""Forfeits"",
        CAST(SUM(sc.""WinPTS"" + sc.""TiePTS"" + sc.""LossPTS"" + sc.""ForfeitPoints"") AS integer) AS ""StandingsPoints"",
        CAST(SUM(sc.""PlusMinus"")                                                AS integer) AS ""PlusMinus"",
        CAST(SUM(sc.""PointsFor"")                                                AS integer) AS ""PointsFor"",
        CAST(SUM(sc.""PointsAgainst"")                                            AS integer) AS ""PointsAgainst""
    FROM ""Scoring"" sc
    GROUP BY sc.""TeamId"", sc.""TeamName"", sc.""DivisionId"", sc.""SeasonId"", sc.""LeagueId""
),

-- ── Step 2: pre-rank to identify which teams are in the same tie group ────────
-- base_div_rank uses only SP/PlusMinus/Wins (no H2H yet).
-- Teams sharing the same base_div_rank value are tied and need H2H resolution.
base_ranked AS (
    SELECT
        agg.*,
        s.""FirstPlaceGuaranteed"",
        CAST(DENSE_RANK() OVER (
            PARTITION BY agg.""DivisionId""
            ORDER BY agg.""StandingsPoints"" DESC, agg.""PlusMinus"" DESC, agg.""Wins"" DESC
        ) AS integer) AS base_div_rank
    FROM agg
    JOIN ""Seasons"" s ON s.""Id"" = agg.""SeasonId""
),

-- ── Step 3: H2H stats, restricted to games against opponents in the same tie group ──
-- For a 3-way tie: only games where all three teams played each other count.
-- For cross-division season ties: no shared games exist, so H2H stays 0 (tie unresolved).
h2h AS (
    SELECT
        sc.""TeamId"",
        sc.""DivisionId"",
        CAST(COALESCE(SUM(sc.""PlusMinus""), 0)                AS integer) AS ""H2HPlusMinus"",
        CAST(COALESCE(COUNT(*) FILTER (WHERE sc.""IsWin""), 0) AS integer) AS ""H2HWins""
    FROM ""Scoring"" sc
    JOIN base_ranked br_me  ON br_me.""TeamId""  = sc.""TeamId""
                            AND br_me.""DivisionId"" = sc.""DivisionId""
    JOIN base_ranked br_opp ON br_opp.""TeamId""  = sc.""OpposingTeamId""
                            AND br_opp.""DivisionId"" = sc.""DivisionId""
    WHERE br_me.base_div_rank = br_opp.base_div_rank   -- same tie group only
    GROUP BY sc.""TeamId"", sc.""DivisionId""
),

-- ── Step 4: final division rankings with H2H tiebreaker applied ───────────────
-- Sort order: StandingsPoints → PlusMinus → Wins → H2HPlusMinus → H2HWins
-- DivisionRank: DENSE_RANK (tied teams share a number — for display)
-- DivisionSeed: ROW_NUMBER (always unique — for bracket assignment)
ranked AS (
    SELECT
        br.*,
        COALESCE(h2h.""H2HPlusMinus"", 0) AS ""H2HPlusMinus"",
        COALESCE(h2h.""H2HWins"",      0) AS ""H2HWins"",
        CAST(DENSE_RANK() OVER (
            PARTITION BY br.""DivisionId""
            ORDER BY
                br.""StandingsPoints""              DESC,
                br.""PlusMinus""                    DESC,
                br.""Wins""                         DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0)  DESC,
                COALESCE(h2h.""H2HWins"",      0)  DESC
        ) AS integer) AS ""DivisionRank"",
        CAST(ROW_NUMBER() OVER (
            PARTITION BY br.""DivisionId""
            ORDER BY
                br.""StandingsPoints""              DESC,
                br.""PlusMinus""                    DESC,
                br.""Wins""                         DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0)  DESC,
                COALESCE(h2h.""H2HWins"",      0)  DESC
        ) AS integer) AS ""DivisionSeed""
    FROM base_ranked br
    LEFT JOIN h2h ON h2h.""TeamId"" = br.""TeamId"" AND h2h.""DivisionId"" = br.""DivisionId""
)

-- ── Step 5: season seed ───────────────────────────────────────────────────────
-- FirstPlaceGuaranteed=true:  all DivisionSeed-1 teams seed before DivisionSeed-2, etc.
--                              within each rank group sort by SP → PM → Wins → H2H.
-- FirstPlaceGuaranteed=false: all teams ranked flat by SP → PM → Wins → H2H.
-- Cross-division ties: H2H will be 0 (teams haven't played each other) — tie stands.
SELECT
    ranked.""DivisionRank"",
    ranked.""DivisionSeed"",
    CASE
        WHEN ranked.""FirstPlaceGuaranteed"" THEN
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    ranked.""DivisionSeed""     ASC,
                    ranked.""StandingsPoints""  DESC,
                    ranked.""PlusMinus""        DESC,
                    ranked.""Wins""             DESC,
                    ranked.""H2HPlusMinus""     DESC,
                    ranked.""H2HWins""          DESC
            ) AS integer)
        ELSE
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    ranked.""StandingsPoints""  DESC,
                    ranked.""PlusMinus""        DESC,
                    ranked.""Wins""             DESC,
                    ranked.""H2HPlusMinus""     DESC,
                    ranked.""H2HWins""          DESC
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
    ranked.""PointsAgainst"",
    ranked.""H2HPlusMinus"",
    ranked.""H2HWins""
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
