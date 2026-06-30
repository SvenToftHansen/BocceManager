using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateScoringView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Scoring"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW ""Scoring"" AS

-- Outer query joins Teams twice for display names and computes PlusMinus.
SELECT
    core.""Id"",
    core.""ScheduleDivisionsId"",
    core.""SeasonId"",
    core.""LeagueId"",
    core.""DivisionId"",
    core.""WeekId"",
    core.""TeamId"",
    COALESCE(tm.""DisplayName"", tm.""SystemName"") AS ""TeamName"",
    core.""OpposingTeamId"",
    COALESCE(op.""DisplayName"", op.""SystemName"") AS ""OpposingTeamName"",
    core.""GameNumber"",
    core.""PointsFor"",
    core.""PointsAgainst"",
    core.""PointsFor"" - core.""PointsAgainst""     AS ""PlusMinus"",
    core.""IsWin"",
    core.""IsTie"",
    core.""IsLoss"",
    core.""IsForfeit"",
    core.""ByForfeit""
FROM (

    -- ── Branch 1: games_mode (Individual Games) ──────────────────────────────
    -- One row per team per game per match. Only valid/played games included.
    -- Valid = one side reached Season.PointsToWinGame, or either side forfeited (-1).
    SELECT
        ROW_NUMBER() OVER (
            ORDER BY sd.""Id"", g.game_num,
            CASE WHEN t.""Id"" = sd.""Team1Id"" THEN 1 ELSE 2 END
        ) AS ""Id"",
        sd.""Id""                 AS ""ScheduleDivisionsId"",
        s.""Id""                  AS ""SeasonId"",
        s.""LeagueId"",
        sd.""DivisionId"",
        sd.""TemplateWeekNumber"" AS ""WeekId"",
        t.""Id""                  AS ""TeamId"",
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team2Id"" ELSE sd.""Team1Id"" END AS ""OpposingTeamId"",
        g.game_num               AS ""GameNumber"",

        -- PointsFor: my raw score for normal play; season forfeit parameters for forfeits.
        CASE
            WHEN sc.my  = -1 THEN 0
            WHEN sc.opp = -1 THEN s.""ForfeitOpponentPlusMinus""
            ELSE sc.my
        END AS ""PointsFor"",

        -- PointsAgainst: sized so (For - Against = PlusMinus margin).
        CASE
            WHEN sc.my  = -1 THEN -s.""ForfeitPlusMinus""
            WHEN sc.opp = -1 THEN 0
            ELSE sc.opp
        END AS ""PointsAgainst"",

        CASE
            WHEN sc.my  = -1 THEN FALSE
            WHEN sc.opp = -1 THEN TRUE
            WHEN sc.my  = s.""PointsToWinGame"" THEN TRUE
            ELSE FALSE
        END AS ""IsWin"",

        FALSE AS ""IsTie"",  -- ties impossible in individual games

        CASE
            WHEN sc.my  = -1 THEN TRUE
            WHEN sc.opp = -1 THEN FALSE
            WHEN sc.opp = s.""PointsToWinGame"" THEN TRUE
            ELSE FALSE
        END AS ""IsLoss"",

        -- IsForfeit: this team forfeited this game (including double forfeit).
        COALESCE(sc.my = -1, FALSE)                    AS ""IsForfeit"",
        -- ByForfeit: opponent forfeited AND this team did not (single forfeit only).
        COALESCE(sc.opp = -1 AND sc.my != -1, FALSE)  AS ""ByForfeit""

    FROM ""ScheduleDivisions"" sd
    JOIN  ""Divisions"" d ON d.""Id"" = sd.""DivisionId""
    JOIN  ""Seasons""   s ON s.""Id"" = d.""SeasonId""
    CROSS JOIN LATERAL (VALUES
        (1, sd.""Team1Score1"", sd.""Team2Score1""),
        (2, sd.""Team1Score2"", sd.""Team2Score2"")
    ) AS g(game_num, t1_raw, t2_raw)
    CROSS JOIN LATERAL (VALUES (sd.""Team1Id""), (sd.""Team2Id"")) AS t(""Id"")
    CROSS JOIN LATERAL (VALUES (
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN g.t1_raw ELSE g.t2_raw END,
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN g.t2_raw ELSE g.t1_raw END
    )) AS sc(my, opp)
    WHERE s.""ScoringMode"" = 'games_mode'
      AND (
          sc.my  = s.""PointsToWinGame"" OR sc.opp = s.""PointsToWinGame""
       OR sc.my  = -1                   OR sc.opp = -1
      )

    UNION ALL

    -- ── Branch 2: match_score_mode (Match Games) ─────────────────────────────
    -- One row per team per match (both games summed). Forfeit determined at match level.
    -- A -1 in either game slot marks the whole match as forfeited for that team.
    SELECT
        ROW_NUMBER() OVER (
            ORDER BY sd.""Id"",
            CASE WHEN t.""Id"" = sd.""Team1Id"" THEN 1 ELSE 2 END
        ) AS ""Id"",
        sd.""Id""                 AS ""ScheduleDivisionsId"",
        s.""Id""                  AS ""SeasonId"",
        s.""LeagueId"",
        sd.""DivisionId"",
        sd.""TemplateWeekNumber"" AS ""WeekId"",
        t.""Id""                  AS ""TeamId"",
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team2Id"" ELSE sd.""Team1Id"" END AS ""OpposingTeamId"",
        NULL::integer            AS ""GameNumber"",

        CASE
            WHEN     mf.i_forfeit AND NOT mf.opp_forfeit THEN 0
            WHEN NOT mf.i_forfeit AND     mf.opp_forfeit THEN s.""ForfeitOpponentPlusMinus""
            WHEN     mf.i_forfeit AND     mf.opp_forfeit THEN 0
            ELSE COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)
        END AS ""PointsFor"",

        CASE
            WHEN     mf.i_forfeit AND NOT mf.opp_forfeit THEN -s.""ForfeitPlusMinus""
            WHEN NOT mf.i_forfeit AND     mf.opp_forfeit THEN 0
            WHEN     mf.i_forfeit AND     mf.opp_forfeit THEN -s.""ForfeitPlusMinus""
            ELSE COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0)
        END AS ""PointsAgainst"",

        CASE
            WHEN     mf.i_forfeit                    THEN FALSE
            WHEN NOT mf.i_forfeit AND mf.opp_forfeit THEN TRUE
            ELSE (COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) >
                 (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))
        END AS ""IsWin"",

        CASE
            WHEN mf.i_forfeit OR mf.opp_forfeit THEN FALSE
            ELSE (COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) =
                 (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))
        END AS ""IsTie"",

        CASE
            WHEN     mf.i_forfeit                    THEN TRUE
            WHEN NOT mf.i_forfeit AND mf.opp_forfeit THEN FALSE
            ELSE (COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) <
                 (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))
        END AS ""IsLoss"",

        -- IsForfeit: this team forfeited the match (any game had -1).
        COALESCE(mf.i_forfeit, FALSE)                          AS ""IsForfeit"",
        -- ByForfeit: opponent forfeited and this team did not (single forfeit benefit).
        COALESCE(mf.opp_forfeit AND NOT mf.i_forfeit, FALSE)  AS ""ByForfeit""

    FROM ""ScheduleDivisions"" sd
    JOIN  ""Divisions"" d ON d.""Id"" = sd.""DivisionId""
    JOIN  ""Seasons""   s ON s.""Id"" = d.""SeasonId""
    CROSS JOIN LATERAL (VALUES (sd.""Team1Id""), (sd.""Team2Id"")) AS t(""Id"")
    CROSS JOIN LATERAL (VALUES (
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team1Score1"" ELSE sd.""Team2Score1"" END,
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team2Score1"" ELSE sd.""Team1Score1"" END,
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team1Score2"" ELSE sd.""Team2Score2"" END,
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN sd.""Team2Score2"" ELSE sd.""Team1Score2"" END
    )) AS sc(my1, opp1, my2, opp2)
    CROSS JOIN LATERAL (VALUES (
        (sd.""Team1Score1"" = -1 OR sd.""Team1Score2"" = -1),
        (sd.""Team2Score1"" = -1 OR sd.""Team2Score2"" = -1)
    )) AS tf(t1_forfeit, t2_forfeit)
    CROSS JOIN LATERAL (VALUES (
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN tf.t1_forfeit ELSE tf.t2_forfeit END,
        CASE WHEN t.""Id"" = sd.""Team1Id"" THEN tf.t2_forfeit ELSE tf.t1_forfeit END
    )) AS mf(i_forfeit, opp_forfeit)
    WHERE s.""ScoringMode"" = 'match_score_mode'
      AND (
          sd.""Team1Score1"" IS NOT NULL OR sd.""Team2Score1"" IS NOT NULL
       OR sd.""Team1Score2"" IS NOT NULL OR sd.""Team2Score2"" IS NOT NULL
      )

) AS core
JOIN ""Teams"" tm ON tm.""Id"" = core.""TeamId""
JOIN ""Teams"" op ON op.""Id"" = core.""OpposingTeamId"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Scoring"";");
        }
    }
}
