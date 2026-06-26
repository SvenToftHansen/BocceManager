-- ============================================================
-- BocceManager — Drop Legacy Website Tables
-- Generated: 2026-06-24
--
-- These are all snake_case tables from the old GVBOCCE.com
-- website project (Prisma ORM). The data was fully migrated
-- into the EF Core PascalCase tables used by BocceManager.
--
-- Safe to run: all 46 tables are either duplicates with
-- matching row counts in the EF tables, or website-only
-- tables with no counterpart in the desktop app.
--
-- Wrapped in a transaction — ROLLBACK if anything looks wrong.
-- ============================================================

BEGIN;

-- --------------------------------------------------------
-- Prisma migration tracker
-- --------------------------------------------------------
DROP TABLE IF EXISTS _prisma_migrations CASCADE;

-- --------------------------------------------------------
-- Website auth tables (no EF counterpart)
-- --------------------------------------------------------
DROP TABLE IF EXISTS player_password_reset_tokens CASCADE;
DROP TABLE IF EXISTS player_roles CASCADE;
DROP TABLE IF EXISTS role_items CASCADE;
DROP TABLE IF EXISTS roles CASCADE;

-- --------------------------------------------------------
-- Website-only junction / mapping tables
-- --------------------------------------------------------
DROP TABLE IF EXISTS pending_player_leagues CASCADE;
DROP TABLE IF EXISTS club_parameters CASCADE;
DROP TABLE IF EXISTS club_courts CASCADE;
DROP TABLE IF EXISTS spare_list_players CASCADE;

-- --------------------------------------------------------
-- clubs — old website "club" record (superseded by Leagues).
-- Contains the full bocce rules HTML text (1 row).
-- Dropping this: if you want to preserve the rules text,
-- copy it to a ClubDocuments record first.
-- --------------------------------------------------------
DROP TABLE IF EXISTS clubs CASCADE;

-- --------------------------------------------------------
-- Duplicate tables — data fully present in EF PascalCase tables
-- --------------------------------------------------------
DROP TABLE IF EXISTS announcements CASCADE;
DROP TABLE IF EXISTS app_parameters CASCADE;
DROP TABLE IF EXISTS courts CASCADE;
DROP TABLE IF EXISTS day_slots CASCADE;
DROP TABLE IF EXISTS divisions CASCADE;
DROP TABLE IF EXISTS email_list_members CASCADE;
DROP TABLE IF EXISTS email_lists CASCADE;
DROP TABLE IF EXISTS email_log CASCADE;
DROP TABLE IF EXISTS games CASCADE;
DROP TABLE IF EXISTS gl_accounts CASCADE;
DROP TABLE IF EXISTS initiation_fees CASCADE;
DROP TABLE IF EXISTS journal_entries CASCADE;
DROP TABLE IF EXISTS league_officials CASCADE;
DROP TABLE IF EXISTS league_parameters CASCADE;
DROP TABLE IF EXISTS leagues CASCADE;
DROP TABLE IF EXISTS looking_for_team CASCADE;
DROP TABLE IF EXISTS match_team_results CASCADE;
DROP TABLE IF EXISTS matches CASCADE;
DROP TABLE IF EXISTS pending_players CASCADE;
DROP TABLE IF EXISTS players CASCADE;
DROP TABLE IF EXISTS playoff_games CASCADE;
DROP TABLE IF EXISTS playoff_matches CASCADE;
DROP TABLE IF EXISTS playoff_rounds CASCADE;
DROP TABLE IF EXISTS schedule_weeks CASCADE;
DROP TABLE IF EXISTS season_courts CASCADE;
DROP TABLE IF EXISTS season_day_slots CASCADE;
DROP TABLE IF EXISTS season_fees CASCADE;
DROP TABLE IF EXISTS season_parameters CASCADE;
DROP TABLE IF EXISTS season_time_slots CASCADE;
DROP TABLE IF EXISTS seasons CASCADE;
DROP TABLE IF EXISTS spare_lists CASCADE;
DROP TABLE IF EXISTS spare_requests CASCADE;
DROP TABLE IF EXISTS team_players CASCADE;
DROP TABLE IF EXISTS team_standings CASCADE;
DROP TABLE IF EXISTS teams CASCADE;
DROP TABLE IF EXISTS time_slots CASCADE;

COMMIT;

-- Verify: after running, this should return 0 rows
-- SELECT table_name FROM information_schema.tables
-- WHERE table_schema = 'public'
--   AND table_type = 'BASE TABLE'
--   AND table_name ~ '^[a-z_]'
-- ORDER BY table_name;
