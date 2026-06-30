using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSeasonSeedOrdering : Migration
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

-- ── Step 2: pre-rank to identify tied teams within each division ──────────────
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

-- ── Step 3: H2H stats against teams in the same division tie group ────────────
h2h AS (
    SELECT
        sc.""TeamId"",
        sc.""DivisionId"",
        CAST(COALESCE(SUM(sc.""PlusMinus""), 0)                AS integer) AS ""H2HPlusMinus"",
        CAST(COALESCE(COUNT(*) FILTER (WHERE sc.""IsWin""), 0) AS integer) AS ""H2HWins""
    FROM ""Scoring"" sc
    JOIN base_ranked br_me  ON br_me.""TeamId""     = sc.""TeamId""
                            AND br_me.""DivisionId"" = sc.""DivisionId""
    JOIN base_ranked br_opp ON br_opp.""TeamId""    = sc.""OpposingTeamId""
                            AND br_opp.""DivisionId"" = sc.""DivisionId""
    WHERE br_me.base_div_rank = br_opp.base_div_rank
    GROUP BY sc.""TeamId"", sc.""DivisionId""
),

-- ── Step 4: final division rankings with H2H tiebreaker ───────────────────────
ranked AS (
    SELECT
        br.*,
        COALESCE(h2h.""H2HPlusMinus"", 0) AS ""H2HPlusMinus"",
        COALESCE(h2h.""H2HWins"",      0) AS ""H2HWins"",
        CAST(DENSE_RANK() OVER (
            PARTITION BY br.""DivisionId""
            ORDER BY
                br.""StandingsPoints""             DESC,
                br.""PlusMinus""                   DESC,
                br.""Wins""                        DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0) DESC,
                COALESCE(h2h.""H2HWins"",      0) DESC
        ) AS integer) AS ""DivisionRank"",
        CAST(ROW_NUMBER() OVER (
            PARTITION BY br.""DivisionId""
            ORDER BY
                br.""StandingsPoints""             DESC,
                br.""PlusMinus""                   DESC,
                br.""Wins""                        DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0) DESC,
                COALESCE(h2h.""H2HWins"",      0) DESC
        ) AS integer) AS ""DivisionSeed""
    FROM base_ranked br
    LEFT JOIN h2h ON h2h.""TeamId"" = br.""TeamId"" AND h2h.""DivisionId"" = br.""DivisionId""
)

-- ── Step 5: season seed ───────────────────────────────────────────────────────
--
-- FirstPlaceGuaranteed = true:
--   All 1st-place finishers (DivisionRank = 1) get the top seeds, ordered among
--   themselves by StandingsPoints → PlusMinus → Wins.
--   All other teams (2nd place and below) follow, ordered purely by their own
--   StandingsPoints → PlusMinus → Wins — division placement is NOT used.
--   Final cross-division tiebreak: DivisionRank ASC (teams that finished higher
--   in their division win a stat-identical tie).
--
-- FirstPlaceGuaranteed = false:
--   All teams ranked flat by StandingsPoints → PlusMinus → Wins, ignoring
--   division placement entirely.
--   Same final cross-division tiebreak: DivisionRank ASC.
--
SELECT
    ranked.""DivisionRank"",
    ranked.""DivisionSeed"",
    CASE
        WHEN ranked.""FirstPlaceGuaranteed"" THEN
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    -- Group 0: 1st-place teams guaranteed; Group 1: everyone else
                    CASE WHEN ranked.""DivisionRank"" = 1 THEN 0 ELSE 1 END ASC,
                    ranked.""StandingsPoints""  DESC,
                    ranked.""PlusMinus""        DESC,
                    ranked.""Wins""             DESC,
                    ranked.""H2HPlusMinus""     DESC,
                    ranked.""H2HWins""          DESC,
                    -- Final tiebreak: team that finished higher in their division wins
                    ranked.""DivisionRank""     ASC
            ) AS integer)
        ELSE
            CAST(ROW_NUMBER() OVER (
                PARTITION BY ranked.""SeasonId""
                ORDER BY
                    ranked.""StandingsPoints""  DESC,
                    ranked.""PlusMinus""        DESC,
                    ranked.""Wins""             DESC,
                    ranked.""H2HPlusMinus""     DESC,
                    ranked.""H2HWins""          DESC,
                    ranked.""DivisionRank""     ASC
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
            // Restore the previous (DivisionSeed-based) ordering
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Standings"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Standings"" AS
WITH agg AS (
    SELECT sc.""TeamId"", sc.""TeamName"", sc.""DivisionId"", sc.""SeasonId"", sc.""LeagueId"",
        CAST(COUNT(*) AS integer) AS ""GamesPlayed"",
        CAST(COUNT(DISTINCT sc.""ScheduleDivisionsId"") AS integer) AS ""MatchesPlayed"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsWin"") AS integer) AS ""Wins"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsTie"") AS integer) AS ""Ties"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsLoss"" AND NOT sc.""IsForfeit"") AS integer) AS ""Losses"",
        CAST(COUNT(*) FILTER (WHERE sc.""IsForfeit"") AS integer) AS ""Forfeits"",
        CAST(SUM(sc.""WinPTS"" + sc.""TiePTS"" + sc.""LossPTS"" + sc.""ForfeitPoints"") AS integer) AS ""StandingsPoints"",
        CAST(SUM(sc.""PlusMinus"") AS integer) AS ""PlusMinus"",
        CAST(SUM(sc.""PointsFor"") AS integer) AS ""PointsFor"",
        CAST(SUM(sc.""PointsAgainst"") AS integer) AS ""PointsAgainst""
    FROM ""Scoring"" sc
    GROUP BY sc.""TeamId"", sc.""TeamName"", sc.""DivisionId"", sc.""SeasonId"", sc.""LeagueId""
),
base_ranked AS (
    SELECT agg.*, s.""FirstPlaceGuaranteed"",
        CAST(DENSE_RANK() OVER (PARTITION BY agg.""DivisionId""
            ORDER BY agg.""StandingsPoints"" DESC, agg.""PlusMinus"" DESC, agg.""Wins"" DESC
        ) AS integer) AS base_div_rank
    FROM agg JOIN ""Seasons"" s ON s.""Id"" = agg.""SeasonId""
),
h2h AS (
    SELECT sc.""TeamId"", sc.""DivisionId"",
        CAST(COALESCE(SUM(sc.""PlusMinus""), 0) AS integer) AS ""H2HPlusMinus"",
        CAST(COALESCE(COUNT(*) FILTER (WHERE sc.""IsWin""), 0) AS integer) AS ""H2HWins""
    FROM ""Scoring"" sc
    JOIN base_ranked br_me  ON br_me.""TeamId"" = sc.""TeamId"" AND br_me.""DivisionId"" = sc.""DivisionId""
    JOIN base_ranked br_opp ON br_opp.""TeamId"" = sc.""OpposingTeamId"" AND br_opp.""DivisionId"" = sc.""DivisionId""
    WHERE br_me.base_div_rank = br_opp.base_div_rank
    GROUP BY sc.""TeamId"", sc.""DivisionId""
),
ranked AS (
    SELECT br.*, COALESCE(h2h.""H2HPlusMinus"", 0) AS ""H2HPlusMinus"", COALESCE(h2h.""H2HWins"", 0) AS ""H2HWins"",
        CAST(DENSE_RANK() OVER (PARTITION BY br.""DivisionId""
            ORDER BY br.""StandingsPoints"" DESC, br.""PlusMinus"" DESC, br.""Wins"" DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0) DESC, COALESCE(h2h.""H2HWins"", 0) DESC
        ) AS integer) AS ""DivisionRank"",
        CAST(ROW_NUMBER() OVER (PARTITION BY br.""DivisionId""
            ORDER BY br.""StandingsPoints"" DESC, br.""PlusMinus"" DESC, br.""Wins"" DESC,
                COALESCE(h2h.""H2HPlusMinus"", 0) DESC, COALESCE(h2h.""H2HWins"", 0) DESC
        ) AS integer) AS ""DivisionSeed""
    FROM base_ranked br LEFT JOIN h2h ON h2h.""TeamId"" = br.""TeamId"" AND h2h.""DivisionId"" = br.""DivisionId""
)
SELECT ranked.""DivisionRank"", ranked.""DivisionSeed"",
    CASE WHEN ranked.""FirstPlaceGuaranteed"" THEN
        CAST(ROW_NUMBER() OVER (PARTITION BY ranked.""SeasonId""
            ORDER BY ranked.""DivisionSeed"" ASC, ranked.""StandingsPoints"" DESC,
                ranked.""PlusMinus"" DESC, ranked.""Wins"" DESC, ranked.""H2HPlusMinus"" DESC, ranked.""H2HWins"" DESC
        ) AS integer)
    ELSE
        CAST(ROW_NUMBER() OVER (PARTITION BY ranked.""SeasonId""
            ORDER BY ranked.""StandingsPoints"" DESC, ranked.""PlusMinus"" DESC,
                ranked.""Wins"" DESC, ranked.""H2HPlusMinus"" DESC, ranked.""H2HWins"" DESC
        ) AS integer)
    END AS ""SeasonSeed"",
    ranked.""TeamId"", ranked.""TeamName"", ranked.""DivisionId"", ranked.""SeasonId"", ranked.""LeagueId"",
    ranked.""GamesPlayed"", ranked.""MatchesPlayed"", ranked.""Wins"", ranked.""Ties"", ranked.""Losses"",
    ranked.""Forfeits"", ranked.""StandingsPoints"", ranked.""PlusMinus"", ranked.""PointsFor"",
    ranked.""PointsAgainst"", ranked.""H2HPlusMinus"", ranked.""H2HWins""
FROM ranked;
");
        }
    }
}
