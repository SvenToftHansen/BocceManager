--
-- PostgreSQL database dump
--

\restrict 4yMCRZinp27Q9jB1wNF5swessz0YvJwuPlHo38HtzcyVXLUG0hodF2qlfaQ4UCD

-- Dumped from database version 18.4 (709c4c3)
-- Dumped by pg_dump version 18.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public."ScheduleTemplates" DROP CONSTRAINT IF EXISTS "ScheduleTemplates_SeasonId_fkey";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplateWeeks" DROP CONSTRAINT IF EXISTS "ScheduleTemplateWeeks_TemplateId_fkey";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplateMatches" DROP CONSTRAINT IF EXISTS "ScheduleTemplateMatches_TemplateWeekId_fkey";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplateMatches" DROP CONSTRAINT IF EXISTS "ScheduleTemplateMatches_CourtId_fkey";
ALTER TABLE IF EXISTS ONLY public."Teams" DROP CONSTRAINT IF EXISTS "FK_Teams_Players_CaptainPlayerId";
ALTER TABLE IF EXISTS ONLY public."Teams" DROP CONSTRAINT IF EXISTS "FK_Teams_Divisions_DivisionId";
ALTER TABLE IF EXISTS ONLY public."TeamStandings" DROP CONSTRAINT IF EXISTS "FK_TeamStandings_Teams_TeamId";
ALTER TABLE IF EXISTS ONLY public."TeamStandings" DROP CONSTRAINT IF EXISTS "FK_TeamStandings_Divisions_DivisionId";
ALTER TABLE IF EXISTS ONLY public."TeamPlayers" DROP CONSTRAINT IF EXISTS "FK_TeamPlayers_Teams_TeamId";
ALTER TABLE IF EXISTS ONLY public."TeamPlayers" DROP CONSTRAINT IF EXISTS "FK_TeamPlayers_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicants" DROP CONSTRAINT IF EXISTS "FK_TeamApplicants_Teams_PlacedTeamId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicants" DROP CONSTRAINT IF EXISTS "FK_TeamApplicants_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicants" DROP CONSTRAINT IF EXISTS "FK_TeamApplicants_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicants" DROP CONSTRAINT IF EXISTS "FK_TeamApplicants_Divisions_PreferredDivisionId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicantMembers" DROP CONSTRAINT IF EXISTS "FK_TeamApplicantMembers_TeamApplicants_TeamApplicantId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicantMembers" DROP CONSTRAINT IF EXISTS "FK_TeamApplicantMembers_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."TeamApplicantMembers" DROP CONSTRAINT IF EXISTS "FK_TeamApplicantMembers_Players_CreatedPlayerId";
ALTER TABLE IF EXISTS ONLY public."SpareLists" DROP CONSTRAINT IF EXISTS "FK_SpareLists_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."SpareLists" DROP CONSTRAINT IF EXISTS "FK_SpareLists_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."Seasons" DROP CONSTRAINT IF EXISTS "FK_Seasons_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."SeasonTimeSlots" DROP CONSTRAINT IF EXISTS "FK_SeasonTimeSlots_TimeSlots_TimeSlotId";
ALTER TABLE IF EXISTS ONLY public."SeasonTimeSlots" DROP CONSTRAINT IF EXISTS "FK_SeasonTimeSlots_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."SeasonParameters" DROP CONSTRAINT IF EXISTS "FK_SeasonParameters_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."SeasonFees" DROP CONSTRAINT IF EXISTS "FK_SeasonFees_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."SeasonFees" DROP CONSTRAINT IF EXISTS "FK_SeasonFees_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."SeasonDaySlots" DROP CONSTRAINT IF EXISTS "FK_SeasonDaySlots_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."SeasonDaySlots" DROP CONSTRAINT IF EXISTS "FK_SeasonDaySlots_DaySlots_DaySlotId";
ALTER TABLE IF EXISTS ONLY public."SeasonCourts" DROP CONSTRAINT IF EXISTS "FK_SeasonCourts_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."SeasonCourts" DROP CONSTRAINT IF EXISTS "FK_SeasonCourts_Courts_CourtId";
ALTER TABLE IF EXISTS ONLY public."ScheduleWeeks" DROP CONSTRAINT IF EXISTS "FK_ScheduleWeeks_Divisions_DivisionId";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "FK_ScheduleDivisions_Teams_Team2Id";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "FK_ScheduleDivisions_Teams_Team1Id";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "FK_ScheduleDivisions_ScheduleTemplates_TemplateId";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "FK_ScheduleDivisions_Divisions_DivisionId";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "FK_ScheduleDivisions_Courts_CourtId";
ALTER TABLE IF EXISTS ONLY public."ReportParameters" DROP CONSTRAINT IF EXISTS "FK_ReportParameters_Reports_ReportId";
ALTER TABLE IF EXISTS ONLY public."PlayoffSeedings" DROP CONSTRAINT IF EXISTS "FK_PlayoffSeedings_Teams_TeamId";
ALTER TABLE IF EXISTS ONLY public."PlayoffSeedings" DROP CONSTRAINT IF EXISTS "FK_PlayoffSeedings_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."PlayoffRounds" DROP CONSTRAINT IF EXISTS "FK_PlayoffRounds_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_Teams_WinnerId";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_Teams_Team2Id";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_Teams_Team1Id";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_PlayoffRounds_PlayoffRoundId";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_PlayoffMatches_NextMatchId";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "FK_PlayoffMatches_Courts_CourtId";
ALTER TABLE IF EXISTS ONLY public."PlayoffGames" DROP CONSTRAINT IF EXISTS "FK_PlayoffGames_PlayoffMatches_PlayoffMatchId";
ALTER TABLE IF EXISTS ONLY public."PlayoffDayParams" DROP CONSTRAINT IF EXISTS "FK_PlayoffDayParams_PlayoffConfigs_PlayoffConfigId";
ALTER TABLE IF EXISTS ONLY public."PlayoffConfigs" DROP CONSTRAINT IF EXISTS "FK_PlayoffConfigs_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."Players" DROP CONSTRAINT IF EXISTS "FK_Players_Players_PartnerPlayerId";
ALTER TABLE IF EXISTS ONLY public."Matches" DROP CONSTRAINT IF EXISTS "FK_Matches_Teams_Team2Id";
ALTER TABLE IF EXISTS ONLY public."Matches" DROP CONSTRAINT IF EXISTS "FK_Matches_Teams_Team1Id";
ALTER TABLE IF EXISTS ONLY public."Matches" DROP CONSTRAINT IF EXISTS "FK_Matches_ScheduleWeeks_ScheduleWeekId";
ALTER TABLE IF EXISTS ONLY public."Matches" DROP CONSTRAINT IF EXISTS "FK_Matches_Courts_CourtId";
ALTER TABLE IF EXISTS ONLY public."MatchTeamResults" DROP CONSTRAINT IF EXISTS "FK_MatchTeamResults_Teams_TeamId";
ALTER TABLE IF EXISTS ONLY public."MatchTeamResults" DROP CONSTRAINT IF EXISTS "FK_MatchTeamResults_Matches_MatchId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_Teams_TeamId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_Teams_PreferredTeamId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_LookingForTeamGroups_LookingForTeamGroupId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "FK_LookingForTeams_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredTimes" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamPreferredTimes_TimeSlots_TimeSlotId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredTimes" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamPreferredTimes_LookingForTeams_LookingForTeam~";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredDays" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamPreferredDays_LookingForTeams_LookingForTeamId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredDays" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamPreferredDays_DaySlots_DaySlotId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamGroups" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamGroups_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamGroups" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamGroups_LookingForTeams_GroupLeaderId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamGroups" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamGroups_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamDivisions" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamDivisions_LookingForTeams_LookingForTeamId";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamDivisions" DROP CONSTRAINT IF EXISTS "FK_LookingForTeamDivisions_Divisions_DivisionId";
ALTER TABLE IF EXISTS ONLY public."LeagueParameters" DROP CONSTRAINT IF EXISTS "FK_LeagueParameters_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."InitiationFees" DROP CONSTRAINT IF EXISTS "FK_InitiationFees_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."Games" DROP CONSTRAINT IF EXISTS "FK_Games_Matches_MatchId";
ALTER TABLE IF EXISTS ONLY public."EmailLogs" DROP CONSTRAINT IF EXISTS "FK_EmailLogs_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."EmailLists" DROP CONSTRAINT IF EXISTS "FK_EmailLists_Leagues_LeagueId";
ALTER TABLE IF EXISTS ONLY public."EmailListMembers" DROP CONSTRAINT IF EXISTS "FK_EmailListMembers_Players_PlayerId";
ALTER TABLE IF EXISTS ONLY public."EmailListMembers" DROP CONSTRAINT IF EXISTS "FK_EmailListMembers_EmailLists_EmailListId";
ALTER TABLE IF EXISTS ONLY public."Divisions" DROP CONSTRAINT IF EXISTS "FK_Divisions_TimeSlots_TimeSlotId";
ALTER TABLE IF EXISTS ONLY public."Divisions" DROP CONSTRAINT IF EXISTS "FK_Divisions_Seasons_SeasonId";
ALTER TABLE IF EXISTS ONLY public."Divisions" DROP CONSTRAINT IF EXISTS "FK_Divisions_DaySlots_DaySlotId";
ALTER TABLE IF EXISTS ONLY public."Announcements" DROP CONSTRAINT IF EXISTS "FK_Announcements_Leagues_LeagueId";
DROP INDEX IF EXISTS public."IX_Teams_DivisionId_TeamLetter";
DROP INDEX IF EXISTS public."IX_Teams_CaptainPlayerId";
DROP INDEX IF EXISTS public."IX_TeamStandings_TeamId_DivisionId";
DROP INDEX IF EXISTS public."IX_TeamStandings_DivisionId";
DROP INDEX IF EXISTS public."IX_TeamPlayers_TeamId_PlayerId";
DROP INDEX IF EXISTS public."IX_TeamPlayers_PlayerId";
DROP INDEX IF EXISTS public."IX_TeamApplicants_SeasonId";
DROP INDEX IF EXISTS public."IX_TeamApplicants_PreferredDivisionId";
DROP INDEX IF EXISTS public."IX_TeamApplicants_PlacedTeamId";
DROP INDEX IF EXISTS public."IX_TeamApplicants_LeagueId";
DROP INDEX IF EXISTS public."IX_TeamApplicantMembers_TeamApplicantId";
DROP INDEX IF EXISTS public."IX_TeamApplicantMembers_PlayerId";
DROP INDEX IF EXISTS public."IX_TeamApplicantMembers_CreatedPlayerId";
DROP INDEX IF EXISTS public."IX_SpareLists_PlayerId";
DROP INDEX IF EXISTS public."IX_SpareLists_LeagueId_PlayerId";
DROP INDEX IF EXISTS public."IX_Seasons_LeagueId";
DROP INDEX IF EXISTS public."IX_SeasonTimeSlots_TimeSlotId";
DROP INDEX IF EXISTS public."IX_SeasonTimeSlots_SeasonId_TimeSlotId";
DROP INDEX IF EXISTS public."IX_SeasonParameters_SeasonId_Key";
DROP INDEX IF EXISTS public."IX_SeasonFees_SeasonId";
DROP INDEX IF EXISTS public."IX_SeasonFees_PlayerId_SeasonId";
DROP INDEX IF EXISTS public."IX_SeasonDaySlots_SeasonId_DaySlotId";
DROP INDEX IF EXISTS public."IX_SeasonDaySlots_DaySlotId";
DROP INDEX IF EXISTS public."IX_SeasonCourts_SeasonId_CourtId";
DROP INDEX IF EXISTS public."IX_SeasonCourts_CourtId";
DROP INDEX IF EXISTS public."IX_ScheduleWeeks_DivisionId";
DROP INDEX IF EXISTS public."IX_ScheduleTemplateWeeks_TemplateId";
DROP INDEX IF EXISTS public."IX_ScheduleTemplateMatches_TemplateWeekId";
DROP INDEX IF EXISTS public."IX_ScheduleTemplateMatches_CourtId";
DROP INDEX IF EXISTS public."IX_ScheduleDivisions_TemplateId";
DROP INDEX IF EXISTS public."IX_ScheduleDivisions_Team2Id";
DROP INDEX IF EXISTS public."IX_ScheduleDivisions_Team1Id";
DROP INDEX IF EXISTS public."IX_ScheduleDivisions_DivisionId";
DROP INDEX IF EXISTS public."IX_ScheduleDivisions_CourtId";
DROP INDEX IF EXISTS public."IX_Reports_Name";
DROP INDEX IF EXISTS public."IX_PlayoffSeedings_TeamId";
DROP INDEX IF EXISTS public."IX_PlayoffSeedings_SeasonId_Seed";
DROP INDEX IF EXISTS public."IX_PlayoffRounds_SeasonId";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_WinnerId";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_Team2Id";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_Team1Id";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_SeasonId";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_PlayoffRoundId";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_NextMatchId";
DROP INDEX IF EXISTS public."IX_PlayoffMatches_CourtId";
DROP INDEX IF EXISTS public."IX_PlayoffGames_PlayoffMatchId";
DROP INDEX IF EXISTS public."IX_PlayoffDayParams_PlayoffConfigId";
DROP INDEX IF EXISTS public."IX_PlayoffConfigs_SeasonId";
DROP INDEX IF EXISTS public."IX_Players_PartnerPlayerId";
DROP INDEX IF EXISTS public."IX_Matches_Team2Id";
DROP INDEX IF EXISTS public."IX_Matches_Team1Id";
DROP INDEX IF EXISTS public."IX_Matches_ScheduleWeekId";
DROP INDEX IF EXISTS public."IX_Matches_CourtId";
DROP INDEX IF EXISTS public."IX_MatchTeamResults_TeamId";
DROP INDEX IF EXISTS public."IX_MatchTeamResults_MatchId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_TeamId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_SeasonId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_PreferredTeamId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_PlayerId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_LookingForTeamGroupId";
DROP INDEX IF EXISTS public."IX_LookingForTeams_LeagueId_PlayerId_SeasonId";
DROP INDEX IF EXISTS public."IX_LookingForTeamPreferredTimes_TimeSlotId";
DROP INDEX IF EXISTS public."IX_LookingForTeamPreferredTimes_LookingForTeamId_TimeSlotId";
DROP INDEX IF EXISTS public."IX_LookingForTeamPreferredDays_LookingForTeamId_DaySlotId";
DROP INDEX IF EXISTS public."IX_LookingForTeamPreferredDays_DaySlotId";
DROP INDEX IF EXISTS public."IX_LookingForTeamGroups_SeasonId";
DROP INDEX IF EXISTS public."IX_LookingForTeamGroups_LeagueId_SeasonId_Name";
DROP INDEX IF EXISTS public."IX_LookingForTeamGroups_GroupLeaderId";
DROP INDEX IF EXISTS public."IX_LookingForTeamDivisions_LookingForTeamId_DivisionId";
DROP INDEX IF EXISTS public."IX_LookingForTeamDivisions_DivisionId";
DROP INDEX IF EXISTS public."IX_LeagueParameters_LeagueId_Key";
DROP INDEX IF EXISTS public."IX_InitiationFees_PlayerId";
DROP INDEX IF EXISTS public."IX_Games_MatchId";
DROP INDEX IF EXISTS public."IX_EmailLogs_LeagueId";
DROP INDEX IF EXISTS public."IX_EmailLists_LeagueId";
DROP INDEX IF EXISTS public."IX_EmailListMembers_PlayerId";
DROP INDEX IF EXISTS public."IX_EmailListMembers_EmailListId_PlayerId";
DROP INDEX IF EXISTS public."IX_Divisions_TimeSlotId";
DROP INDEX IF EXISTS public."IX_Divisions_SeasonId";
DROP INDEX IF EXISTS public."IX_Divisions_DaySlotId";
DROP INDEX IF EXISTS public."IX_AppParameters_Key";
DROP INDEX IF EXISTS public."IX_Announcements_LeagueId";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplates" DROP CONSTRAINT IF EXISTS "ScheduleTemplates_pkey";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplateWeeks" DROP CONSTRAINT IF EXISTS "ScheduleTemplateWeeks_pkey";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplateMatches" DROP CONSTRAINT IF EXISTS "ScheduleTemplateMatches_pkey";
ALTER TABLE IF EXISTS ONLY public."Reports" DROP CONSTRAINT IF EXISTS "Reports_pkey";
ALTER TABLE IF EXISTS ONLY public."Reports" DROP CONSTRAINT IF EXISTS "Reports_Name_key";
ALTER TABLE IF EXISTS ONLY public."ReportParameters" DROP CONSTRAINT IF EXISTS "ReportParameters_pkey";
ALTER TABLE IF EXISTS ONLY public."ReportParameters" DROP CONSTRAINT IF EXISTS "ReportParameters_ReportId_ParameterName_key";
ALTER TABLE IF EXISTS ONLY public."__EFMigrationsHistory" DROP CONSTRAINT IF EXISTS "PK___EFMigrationsHistory";
ALTER TABLE IF EXISTS ONLY public."TimeSlots" DROP CONSTRAINT IF EXISTS "PK_TimeSlots";
ALTER TABLE IF EXISTS ONLY public."Teams" DROP CONSTRAINT IF EXISTS "PK_Teams";
ALTER TABLE IF EXISTS ONLY public."TeamStandings" DROP CONSTRAINT IF EXISTS "PK_TeamStandings";
ALTER TABLE IF EXISTS ONLY public."TeamPlayers" DROP CONSTRAINT IF EXISTS "PK_TeamPlayers";
ALTER TABLE IF EXISTS ONLY public."TeamApplicants" DROP CONSTRAINT IF EXISTS "PK_TeamApplicants";
ALTER TABLE IF EXISTS ONLY public."TeamApplicantMembers" DROP CONSTRAINT IF EXISTS "PK_TeamApplicantMembers";
ALTER TABLE IF EXISTS ONLY public."SpareLists" DROP CONSTRAINT IF EXISTS "PK_SpareLists";
ALTER TABLE IF EXISTS ONLY public."Seasons" DROP CONSTRAINT IF EXISTS "PK_Seasons";
ALTER TABLE IF EXISTS ONLY public."SeasonTimeSlots" DROP CONSTRAINT IF EXISTS "PK_SeasonTimeSlots";
ALTER TABLE IF EXISTS ONLY public."SeasonParameters" DROP CONSTRAINT IF EXISTS "PK_SeasonParameters";
ALTER TABLE IF EXISTS ONLY public."SeasonFees" DROP CONSTRAINT IF EXISTS "PK_SeasonFees";
ALTER TABLE IF EXISTS ONLY public."SeasonDaySlots" DROP CONSTRAINT IF EXISTS "PK_SeasonDaySlots";
ALTER TABLE IF EXISTS ONLY public."SeasonCourts" DROP CONSTRAINT IF EXISTS "PK_SeasonCourts";
ALTER TABLE IF EXISTS ONLY public."ScheduleWeeks" DROP CONSTRAINT IF EXISTS "PK_ScheduleWeeks";
ALTER TABLE IF EXISTS ONLY public."ScheduleDivisions" DROP CONSTRAINT IF EXISTS "PK_ScheduleDivisions";
ALTER TABLE IF EXISTS ONLY public."PlayoffSeedings" DROP CONSTRAINT IF EXISTS "PK_PlayoffSeedings";
ALTER TABLE IF EXISTS ONLY public."PlayoffRounds" DROP CONSTRAINT IF EXISTS "PK_PlayoffRounds";
ALTER TABLE IF EXISTS ONLY public."PlayoffMatches" DROP CONSTRAINT IF EXISTS "PK_PlayoffMatches";
ALTER TABLE IF EXISTS ONLY public."PlayoffGames" DROP CONSTRAINT IF EXISTS "PK_PlayoffGames";
ALTER TABLE IF EXISTS ONLY public."PlayoffDayParams" DROP CONSTRAINT IF EXISTS "PK_PlayoffDayParams";
ALTER TABLE IF EXISTS ONLY public."PlayoffConfigs" DROP CONSTRAINT IF EXISTS "PK_PlayoffConfigs";
ALTER TABLE IF EXISTS ONLY public."Players" DROP CONSTRAINT IF EXISTS "PK_Players";
ALTER TABLE IF EXISTS ONLY public."PlayerRoles" DROP CONSTRAINT IF EXISTS "PK_PlayerRoles";
ALTER TABLE IF EXISTS ONLY public."NewIdeas" DROP CONSTRAINT IF EXISTS "PK_NewIdeas";
ALTER TABLE IF EXISTS ONLY public."Matches" DROP CONSTRAINT IF EXISTS "PK_Matches";
ALTER TABLE IF EXISTS ONLY public."MatchTeamResults" DROP CONSTRAINT IF EXISTS "PK_MatchTeamResults";
ALTER TABLE IF EXISTS ONLY public."LookingForTeams" DROP CONSTRAINT IF EXISTS "PK_LookingForTeams";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredTimes" DROP CONSTRAINT IF EXISTS "PK_LookingForTeamPreferredTimes";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamPreferredDays" DROP CONSTRAINT IF EXISTS "PK_LookingForTeamPreferredDays";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamGroups" DROP CONSTRAINT IF EXISTS "PK_LookingForTeamGroups";
ALTER TABLE IF EXISTS ONLY public."LookingForTeamDivisions" DROP CONSTRAINT IF EXISTS "PK_LookingForTeamDivisions";
ALTER TABLE IF EXISTS ONLY public."Leagues" DROP CONSTRAINT IF EXISTS "PK_Leagues";
ALTER TABLE IF EXISTS ONLY public."LeagueParameters" DROP CONSTRAINT IF EXISTS "PK_LeagueParameters";
ALTER TABLE IF EXISTS ONLY public."InitiationFees" DROP CONSTRAINT IF EXISTS "PK_InitiationFees";
ALTER TABLE IF EXISTS ONLY public."Games" DROP CONSTRAINT IF EXISTS "PK_Games";
ALTER TABLE IF EXISTS ONLY public."EmailLogs" DROP CONSTRAINT IF EXISTS "PK_EmailLogs";
ALTER TABLE IF EXISTS ONLY public."EmailLists" DROP CONSTRAINT IF EXISTS "PK_EmailLists";
ALTER TABLE IF EXISTS ONLY public."EmailListMembers" DROP CONSTRAINT IF EXISTS "PK_EmailListMembers";
ALTER TABLE IF EXISTS ONLY public."Divisions" DROP CONSTRAINT IF EXISTS "PK_Divisions";
ALTER TABLE IF EXISTS ONLY public."DaySlots" DROP CONSTRAINT IF EXISTS "PK_DaySlots";
ALTER TABLE IF EXISTS ONLY public."Courts" DROP CONSTRAINT IF EXISTS "PK_Courts";
ALTER TABLE IF EXISTS ONLY public."ClubDocuments" DROP CONSTRAINT IF EXISTS "PK_ClubDocuments";
ALTER TABLE IF EXISTS ONLY public."AppParameters" DROP CONSTRAINT IF EXISTS "PK_AppParameters";
ALTER TABLE IF EXISTS ONLY public."Announcements" DROP CONSTRAINT IF EXISTS "PK_Announcements";
ALTER TABLE IF EXISTS ONLY public."ScheduleTemplates" DROP CONSTRAINT IF EXISTS "IX_ScheduleTemplates_SeasonId_TeamCount";
ALTER TABLE IF EXISTS public."ScheduleTemplates" ALTER COLUMN "Id" DROP DEFAULT;
ALTER TABLE IF EXISTS public."ScheduleTemplateWeeks" ALTER COLUMN "Id" DROP DEFAULT;
ALTER TABLE IF EXISTS public."ScheduleTemplateMatches" ALTER COLUMN "Id" DROP DEFAULT;
DROP TABLE IF EXISTS public."__EFMigrationsHistory";
DROP TABLE IF EXISTS public."TimeSlots";
DROP TABLE IF EXISTS public."TeamStandings";
DROP TABLE IF EXISTS public."TeamPlayers";
DROP TABLE IF EXISTS public."TeamApplicants";
DROP TABLE IF EXISTS public."TeamApplicantMembers";
DROP VIEW IF EXISTS public."Stats";
DROP VIEW IF EXISTS public."Standings";
DROP TABLE IF EXISTS public."SpareLists";
DROP TABLE IF EXISTS public."SeasonTimeSlots";
DROP TABLE IF EXISTS public."SeasonParameters";
DROP TABLE IF EXISTS public."SeasonFees";
DROP TABLE IF EXISTS public."SeasonDaySlots";
DROP TABLE IF EXISTS public."SeasonCourts";
DROP VIEW IF EXISTS public."Scoring";
DROP TABLE IF EXISTS public."Teams";
DROP TABLE IF EXISTS public."Seasons";
DROP TABLE IF EXISTS public."ScheduleWeeks";
DROP SEQUENCE IF EXISTS public."ScheduleTemplates_Id_seq";
DROP TABLE IF EXISTS public."ScheduleTemplates";
DROP SEQUENCE IF EXISTS public."ScheduleTemplateWeeks_Id_seq";
DROP TABLE IF EXISTS public."ScheduleTemplateWeeks";
DROP SEQUENCE IF EXISTS public."ScheduleTemplateMatches_Id_seq";
DROP TABLE IF EXISTS public."ScheduleTemplateMatches";
DROP TABLE IF EXISTS public."ScheduleDivisions";
DROP TABLE IF EXISTS public."Reports";
DROP TABLE IF EXISTS public."ReportParameters";
DROP TABLE IF EXISTS public."PlayoffSeedings";
DROP TABLE IF EXISTS public."PlayoffRounds";
DROP TABLE IF EXISTS public."PlayoffMatches";
DROP TABLE IF EXISTS public."PlayoffGames";
DROP TABLE IF EXISTS public."PlayoffDayParams";
DROP TABLE IF EXISTS public."PlayoffConfigs";
DROP TABLE IF EXISTS public."Players";
DROP TABLE IF EXISTS public."PlayerRoles";
DROP TABLE IF EXISTS public."NewIdeas";
DROP TABLE IF EXISTS public."Matches";
DROP TABLE IF EXISTS public."MatchTeamResults";
DROP TABLE IF EXISTS public."LookingForTeams";
DROP TABLE IF EXISTS public."LookingForTeamPreferredTimes";
DROP TABLE IF EXISTS public."LookingForTeamPreferredDays";
DROP TABLE IF EXISTS public."LookingForTeamGroups";
DROP TABLE IF EXISTS public."LookingForTeamDivisions";
DROP TABLE IF EXISTS public."Leagues";
DROP TABLE IF EXISTS public."LeagueParameters";
DROP TABLE IF EXISTS public."InitiationFees";
DROP TABLE IF EXISTS public."Games";
DROP TABLE IF EXISTS public."EmailLogs";
DROP TABLE IF EXISTS public."EmailLists";
DROP TABLE IF EXISTS public."EmailListMembers";
DROP TABLE IF EXISTS public."Divisions";
DROP TABLE IF EXISTS public."DaySlots";
DROP TABLE IF EXISTS public."Courts";
DROP TABLE IF EXISTS public."ClubDocuments";
DROP TABLE IF EXISTS public."AppParameters";
DROP TABLE IF EXISTS public."Announcements";
DROP TYPE IF EXISTS public."TeamPlayerRole";
DROP TYPE IF EXISTS public."SpareRequestStatus";
DROP TYPE IF EXISTS public."SeasonStatus";
DROP TYPE IF EXISTS public."ScoringMode";
DROP TYPE IF EXISTS public."PlayoffType";
DROP TYPE IF EXISTS public."PlayoffMatchStatus";
DROP TYPE IF EXISTS public."PendingStatus";
DROP TYPE IF EXISTS public."MatchStatus";
DROP TYPE IF EXISTS public."GlAccountType";
DROP TYPE IF EXISTS public."GameInterval";
--
-- Name: GameInterval; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."GameInterval" AS ENUM (
    'weekly',
    'schedule_determined'
);


--
-- Name: GlAccountType; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."GlAccountType" AS ENUM (
    'asset',
    'income',
    'expense',
    'liability',
    'equity'
);


--
-- Name: MatchStatus; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."MatchStatus" AS ENUM (
    'scheduled',
    'completed',
    'no_show_team1',
    'no_show_team2',
    'postponed',
    'bye'
);


--
-- Name: PendingStatus; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."PendingStatus" AS ENUM (
    'pending',
    'approved',
    'rejected'
);


--
-- Name: PlayoffMatchStatus; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."PlayoffMatchStatus" AS ENUM (
    'scheduled',
    'completed',
    'bye'
);


--
-- Name: PlayoffType; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."PlayoffType" AS ENUM (
    'ladder',
    'round_robin'
);


--
-- Name: ScoringMode; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."ScoringMode" AS ENUM (
    'games_mode',
    'match_score_mode',
    'match_play'
);


--
-- Name: SeasonStatus; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."SeasonStatus" AS ENUM (
    'building',
    'regular_season',
    'playoffs',
    'complete'
);


--
-- Name: SpareRequestStatus; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."SpareRequestStatus" AS ENUM (
    'pending',
    'accepted',
    'declined',
    'cancelled'
);


--
-- Name: TeamPlayerRole; Type: TYPE; Schema: public; Owner: -
--

CREATE TYPE public."TeamPlayerRole" AS ENUM (
    'captain',
    'player'
);


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Announcements; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Announcements" (
    "Id" integer NOT NULL,
    "LeagueId" integer,
    "Title" text NOT NULL,
    "Body" text NOT NULL,
    "PublishedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CreatedBy" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "DesignJson" text
);


--
-- Name: Announcements_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Announcements" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Announcements_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: AppParameters; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."AppParameters" (
    "Id" integer NOT NULL,
    "Key" text NOT NULL,
    "Value" text NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL
);


--
-- Name: AppParameters_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."AppParameters" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."AppParameters_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ClubDocuments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ClubDocuments" (
    "Id" integer NOT NULL,
    "Title" text NOT NULL,
    "FileName" text NOT NULL,
    "DocType" text NOT NULL,
    "GoogleDocsUrl" text,
    "LeagueId" integer,
    "UploadedAt" timestamp with time zone NOT NULL,
    "Notes" text
);


--
-- Name: ClubDocuments_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ClubDocuments" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ClubDocuments_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Courts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Courts" (
    "Id" integer NOT NULL,
    "CourtLetter" text CONSTRAINT "Courts_CourtName_not_null" NOT NULL,
    "Notes" text,
    "IsActive" boolean NOT NULL,
    "CourtNumber" integer DEFAULT 0 NOT NULL,
    "SortOrder" integer DEFAULT 0 NOT NULL
);


--
-- Name: Courts_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Courts" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Courts_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: DaySlots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DaySlots" (
    "Id" integer NOT NULL,
    "DayName" text NOT NULL,
    "DayAbbr" text NOT NULL,
    "DayNbr" integer NOT NULL,
    "IsActive" boolean NOT NULL
);


--
-- Name: DaySlots_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."DaySlots" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."DaySlots_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Divisions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Divisions" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "Name" text NOT NULL,
    "ShortName" text NOT NULL,
    "SortName" text NOT NULL,
    "PlayersPerTeamMinimum" integer,
    "PlayersPerTeamMaximum" integer,
    "TeamsInDivision" integer NOT NULL,
    "DaySlotId" integer,
    "TimeSlotId" integer,
    "IsActive" boolean NOT NULL
);


--
-- Name: Divisions_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Divisions" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Divisions_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: EmailListMembers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EmailListMembers" (
    "Id" integer NOT NULL,
    "EmailListId" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "IsActive" boolean NOT NULL
);


--
-- Name: EmailListMembers_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."EmailListMembers" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."EmailListMembers_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: EmailLists; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EmailLists" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL
);


--
-- Name: EmailLists_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."EmailLists" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."EmailLists_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: EmailLogs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."EmailLogs" (
    "Id" integer NOT NULL,
    "SentBy" text,
    "LeagueId" integer,
    "Subject" text NOT NULL,
    "Body" text,
    "RecipientCount" integer,
    "SentAt" timestamp with time zone NOT NULL
);


--
-- Name: EmailLogs_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."EmailLogs" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."EmailLogs_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Games; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Games" (
    "Id" integer NOT NULL,
    "MatchId" integer NOT NULL,
    "GameNumber" integer NOT NULL,
    "Team1Score" integer NOT NULL,
    "Team2Score" integer NOT NULL,
    "IsForfeit" boolean NOT NULL,
    "EnteredBy" text,
    "EnteredAt" timestamp with time zone
);


--
-- Name: Games_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Games" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Games_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: InitiationFees; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."InitiationFees" (
    "Id" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "AmountOwing" numeric NOT NULL,
    "AmountPaid" numeric NOT NULL,
    "PaidDate" date,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: InitiationFees_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."InitiationFees" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."InitiationFees_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LeagueParameters; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LeagueParameters" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "Key" text NOT NULL,
    "Value" text NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL
);


--
-- Name: LeagueParameters_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LeagueParameters" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LeagueParameters_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Leagues; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Leagues" (
    "Id" integer NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "RulesText" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "PlayersPerTeamMinimum" integer,
    "PlayersPerTeamMaximum" integer,
    "MaxTeamsInDivision" integer NOT NULL
);


--
-- Name: Leagues_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Leagues" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Leagues_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LookingForTeamDivisions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LookingForTeamDivisions" (
    "Id" integer NOT NULL,
    "LookingForTeamId" integer NOT NULL,
    "DivisionId" integer NOT NULL
);


--
-- Name: LookingForTeamDivisions_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LookingForTeamDivisions" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LookingForTeamDivisions_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LookingForTeamGroups; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LookingForTeamGroups" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Name" text,
    "GroupLeaderId" integer
);


--
-- Name: LookingForTeamGroups_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LookingForTeamGroups" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LookingForTeamGroups_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LookingForTeamPreferredDays; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LookingForTeamPreferredDays" (
    "Id" integer CONSTRAINT "LookingForTeamPreferredDay_Id_not_null" NOT NULL,
    "LookingForTeamId" integer CONSTRAINT "LookingForTeamPreferredDay_LookingForTeamId_not_null" NOT NULL,
    "DaySlotId" integer CONSTRAINT "LookingForTeamPreferredDay_DaySlotId_not_null" NOT NULL
);


--
-- Name: LookingForTeamPreferredDay_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LookingForTeamPreferredDays" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LookingForTeamPreferredDay_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LookingForTeamPreferredTimes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LookingForTeamPreferredTimes" (
    "Id" integer CONSTRAINT "LookingForTeamPreferredTime_Id_not_null" NOT NULL,
    "LookingForTeamId" integer CONSTRAINT "LookingForTeamPreferredTime_LookingForTeamId_not_null" NOT NULL,
    "TimeSlotId" integer CONSTRAINT "LookingForTeamPreferredTime_TimeSlotId_not_null" NOT NULL
);


--
-- Name: LookingForTeamPreferredTime_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LookingForTeamPreferredTimes" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LookingForTeamPreferredTime_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LookingForTeams; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."LookingForTeams" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "TeamId" integer,
    "SeasonId" integer,
    "Notes" text,
    "PreferredTeamId" integer,
    "RegisteredDate" date,
    "LookingForTeamGroupId" integer
);


--
-- Name: LookingForTeams_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."LookingForTeams" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."LookingForTeams_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: MatchTeamResults; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."MatchTeamResults" (
    "Id" integer NOT NULL,
    "MatchId" integer NOT NULL,
    "TeamId" integer NOT NULL,
    "Wins" integer NOT NULL,
    "Losses" integer NOT NULL,
    "Ties" integer NOT NULL,
    "NoShows" integer NOT NULL,
    "StandingsPoints" integer NOT NULL,
    "PointsFor" integer NOT NULL,
    "PointsAgainst" integer NOT NULL,
    "PlusMinus" integer NOT NULL
);


--
-- Name: MatchTeamResults_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."MatchTeamResults" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."MatchTeamResults_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Matches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Matches" (
    "Id" integer NOT NULL,
    "ScheduleWeekId" integer NOT NULL,
    "Team1Id" integer NOT NULL,
    "Team2Id" integer NOT NULL,
    "CourtId" integer,
    "ScheduledDate" date,
    "ScheduledTime" time without time zone,
    "GamesPlayed" integer NOT NULL,
    "Status" text NOT NULL,
    "EnteredBy" text,
    "EnteredAt" timestamp with time zone
);


--
-- Name: Matches_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Matches" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Matches_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: NewIdeas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."NewIdeas" (
    "Id" integer NOT NULL,
    "Idea" text NOT NULL,
    "DateCreated" timestamp with time zone NOT NULL,
    "DateCollected" timestamp with time zone
);


--
-- Name: NewIdeas_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."NewIdeas" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."NewIdeas_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayerRoles; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayerRoles" (
    "Id" integer NOT NULL,
    "RoleName" text NOT NULL
);


--
-- Name: PlayerRoles_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayerRoles" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayerRoles_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Players; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Players" (
    "Id" integer NOT NULL,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "Email" text,
    "Phone" text,
    "LotNumber" text,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "PartnerPlayerId" integer,
    "Role" integer DEFAULT 0 NOT NULL
);


--
-- Name: Players_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Players" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Players_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffConfigs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffConfigs" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "MatchDurationMins" integer NOT NULL,
    "DisplayMode" text NOT NULL,
    "IsGenerated" boolean NOT NULL,
    "TiebreakerBalls" integer DEFAULT 0 NOT NULL
);


--
-- Name: PlayoffConfigs_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffConfigs" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffConfigs_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffDayParams; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffDayParams" (
    "Id" integer NOT NULL,
    "PlayoffConfigId" integer NOT NULL,
    "DayNumber" integer NOT NULL,
    "GameDate" date NOT NULL,
    "StartTime" time without time zone NOT NULL,
    "EndTime" time without time zone NOT NULL,
    "MatchLengthMins" integer CONSTRAINT "PlayoffDayParams_DurationBetweenRoundsMins_not_null" NOT NULL
);


--
-- Name: PlayoffDayParams_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffDayParams" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffDayParams_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffGames; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffGames" (
    "Id" integer NOT NULL,
    "PlayoffMatchId" integer NOT NULL,
    "GameNumber" integer NOT NULL,
    "Team1Score" integer NOT NULL,
    "Team2Score" integer NOT NULL,
    "IsForfeit" boolean NOT NULL,
    "EnteredBy" text,
    "EnteredAt" timestamp with time zone
);


--
-- Name: PlayoffGames_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffGames" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffGames_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffMatches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffMatches" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "PlayoffRoundId" integer,
    "Seed1" integer NOT NULL,
    "Seed2" integer,
    "Team1Id" integer,
    "Team2Id" integer,
    "CourtId" integer,
    "ScheduledDate" date,
    "ScheduledTime" time without time zone,
    "Status" text NOT NULL,
    "WinnerId" integer,
    "EnteredBy" text,
    "EnteredAt" timestamp with time zone,
    "BracketSlot" integer DEFAULT 0 NOT NULL,
    "IsBye" boolean DEFAULT false NOT NULL,
    "NextMatchId" integer,
    "NextMatchIsTop" boolean DEFAULT false NOT NULL
);


--
-- Name: PlayoffMatches_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffMatches" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffMatches_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffRounds; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffRounds" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "RoundNumber" integer NOT NULL,
    "RoundName" text,
    "MatchDate" date,
    "DurationBetweenRoundsMins" integer DEFAULT 0 NOT NULL,
    "EndTime" time without time zone,
    "StartTime" time without time zone
);


--
-- Name: PlayoffRounds_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffRounds" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffRounds_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: PlayoffSeedings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."PlayoffSeedings" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "Seed" integer NOT NULL,
    "TeamId" integer NOT NULL
);


--
-- Name: PlayoffSeedings_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."PlayoffSeedings" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."PlayoffSeedings_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ReportParameters; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ReportParameters" (
    "Id" integer NOT NULL,
    "ReportId" integer NOT NULL,
    "ParameterName" text NOT NULL,
    "ParameterLabel" text NOT NULL,
    "IsRequired" boolean NOT NULL,
    "DefaultSource" text NOT NULL,
    "DisplayOrder" integer NOT NULL
);


--
-- Name: ReportParameters_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ReportParameters" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."ReportParameters_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Reports; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Reports" (
    "Id" integer NOT NULL,
    "Name" text NOT NULL,
    "ReportPath" text NOT NULL,
    "Description" text NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "IsActive" boolean DEFAULT true NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone NOT NULL
);


--
-- Name: Reports_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Reports" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Reports_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ScheduleDivisions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScheduleDivisions" (
    "Id" integer NOT NULL,
    "DivisionId" integer NOT NULL,
    "TemplateId" integer NOT NULL,
    "TemplateWeekNumber" integer NOT NULL,
    "MatchDate" date NOT NULL,
    "Team1Id" integer NOT NULL,
    "Team2Id" integer NOT NULL,
    "CourtId" integer,
    "CreatedDate" timestamp with time zone NOT NULL,
    "Team1Score1" integer DEFAULT 0,
    "Team1Score2" integer DEFAULT 0,
    "Team2Score1" integer DEFAULT 0,
    "Team2Score2" integer DEFAULT 0
);


--
-- Name: ScheduleDivisions_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ScheduleDivisions" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ScheduleDivisions_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ScheduleTemplateMatches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScheduleTemplateMatches" (
    "Id" integer NOT NULL,
    "TemplateWeekId" integer NOT NULL,
    "Slot1" text NOT NULL,
    "Slot2" text NOT NULL,
    "CourtId" integer NOT NULL
);


--
-- Name: ScheduleTemplateMatches_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."ScheduleTemplateMatches_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: ScheduleTemplateMatches_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."ScheduleTemplateMatches_Id_seq" OWNED BY public."ScheduleTemplateMatches"."Id";


--
-- Name: ScheduleTemplateWeeks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScheduleTemplateWeeks" (
    "Id" integer NOT NULL,
    "TemplateId" integer NOT NULL,
    "WeekNumber" integer NOT NULL
);


--
-- Name: ScheduleTemplateWeeks_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."ScheduleTemplateWeeks_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: ScheduleTemplateWeeks_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."ScheduleTemplateWeeks_Id_seq" OWNED BY public."ScheduleTemplateWeeks"."Id";


--
-- Name: ScheduleTemplates; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScheduleTemplates" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "TeamCount" integer NOT NULL,
    "WeekCount" integer NOT NULL,
    "GeneratedAt" timestamp with time zone NOT NULL,
    "IsLocked" boolean DEFAULT false NOT NULL
);


--
-- Name: ScheduleTemplates_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."ScheduleTemplates_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: ScheduleTemplates_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."ScheduleTemplates_Id_seq" OWNED BY public."ScheduleTemplates"."Id";


--
-- Name: ScheduleWeeks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."ScheduleWeeks" (
    "Id" integer NOT NULL,
    "DivisionId" integer NOT NULL,
    "WeekNumber" integer NOT NULL,
    "MatchDate" date
);


--
-- Name: ScheduleWeeks_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."ScheduleWeeks" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."ScheduleWeeks_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Seasons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Seasons" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "Name" text NOT NULL,
    "StartDate" date,
    "EndDate" date,
    "GamesPerSeason" integer NOT NULL,
    "PlayersPerTeamMinimum" integer,
    "PlayersPerTeamMaximum" integer,
    "PointsForWin" integer NOT NULL,
    "PointsForTie" integer NOT NULL,
    "PointsForLoss" integer NOT NULL,
    "PointsForNoShow" integer NOT NULL,
    "PointsToWinGame" integer NOT NULL,
    "GamesPerMatch" integer NOT NULL,
    "ScoringMode" text NOT NULL,
    "TeamsInPlayoffs" integer NOT NULL,
    "FirstPlaceGuaranteed" boolean NOT NULL,
    "PlayoffType" text NOT NULL,
    "PlayoffGamesPerMatch" integer NOT NULL,
    "PlayoffScoringMode" text NOT NULL,
    "IsCurrent" boolean NOT NULL,
    "WeeksInSeason" integer NOT NULL,
    "MaxTeamsInDivision" integer NOT NULL,
    "Status" text NOT NULL,
    "PlayoffStartDate" date,
    "PlayoffEndDate" date,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsLocked" boolean DEFAULT false NOT NULL,
    "PlayoffTiebreakerFormat" text DEFAULT ''::text NOT NULL,
    "ForfeitOpponentPlusMinus" integer DEFAULT 0 NOT NULL,
    "ForfeitPlusMinus" integer DEFAULT 0 NOT NULL,
    "CourtDisplayStyle" text DEFAULT 'number'::text NOT NULL
);


--
-- Name: Teams; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Teams" (
    "Id" integer NOT NULL,
    "DivisionId" integer NOT NULL,
    "TeamLetter" text NOT NULL,
    "SystemName" text NOT NULL,
    "DisplayName" text,
    "CaptainPlayerId" integer,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "SortOrder" text DEFAULT ''::text NOT NULL
);


--
-- Name: Scoring; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public."Scoring" AS
 SELECT core."Id",
    core."ScheduleDivisionsId",
    core."SeasonId",
    core."LeagueId",
    core."DivisionId",
    core."WeekId",
    core."TeamId",
    COALESCE(tm."DisplayName", tm."SystemName") AS "TeamName",
    core."OpposingTeamId",
    COALESCE(op."DisplayName", op."SystemName") AS "OpposingTeamName",
    core."GameNumber",
    core."PointsFor",
    core."PointsAgainst",
    (core."PointsFor" - core."PointsAgainst") AS "PlusMinus",
    core."IsWin",
    core."IsTie",
    core."IsLoss",
    core."IsForfeit",
    core."ByForfeit",
    core."WinPTS",
    core."TiePTS",
    core."LossPTS",
    core."ForfeitPoints"
   FROM ((( SELECT row_number() OVER (ORDER BY sd."Id", g.game_num,
                CASE
                    WHEN (t."Id" = sd."Team1Id") THEN 1
                    ELSE 2
                END) AS "Id",
            sd."Id" AS "ScheduleDivisionsId",
            s."Id" AS "SeasonId",
            s."LeagueId",
            sd."DivisionId",
            sd."TemplateWeekNumber" AS "WeekId",
            t."Id" AS "TeamId",
                CASE
                    WHEN (t."Id" = sd."Team1Id") THEN sd."Team2Id"
                    ELSE sd."Team1Id"
                END AS "OpposingTeamId",
            g.game_num AS "GameNumber",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN 0
                    WHEN (sc.opp = '-1'::integer) THEN s."ForfeitOpponentPlusMinus"
                    ELSE sc.my
                END AS "PointsFor",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN (- s."ForfeitPlusMinus")
                    WHEN (sc.opp = '-1'::integer) THEN 0
                    ELSE sc.opp
                END AS "PointsAgainst",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN false
                    WHEN (sc.opp = '-1'::integer) THEN true
                    WHEN (sc.my = s."PointsToWinGame") THEN true
                    ELSE false
                END AS "IsWin",
            false AS "IsTie",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN true
                    WHEN (sc.opp = '-1'::integer) THEN false
                    WHEN (sc.opp = s."PointsToWinGame") THEN true
                    ELSE false
                END AS "IsLoss",
            COALESCE((sc.my = '-1'::integer), false) AS "IsForfeit",
            COALESCE(((sc.opp = '-1'::integer) AND (sc.my <> '-1'::integer)), false) AS "ByForfeit",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN 0
                    WHEN (sc.opp = '-1'::integer) THEN s."PointsForWin"
                    WHEN (sc.my = s."PointsToWinGame") THEN s."PointsForWin"
                    ELSE 0
                END AS "WinPTS",
            0 AS "TiePTS",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN 0
                    WHEN (sc.opp = s."PointsToWinGame") THEN s."PointsForLoss"
                    ELSE 0
                END AS "LossPTS",
                CASE
                    WHEN (sc.my = '-1'::integer) THEN s."PointsForNoShow"
                    ELSE 0
                END AS "ForfeitPoints"
           FROM (((((public."ScheduleDivisions" sd
             JOIN public."Divisions" d ON ((d."Id" = sd."DivisionId")))
             JOIN public."Seasons" s ON ((s."Id" = d."SeasonId")))
             CROSS JOIN LATERAL ( VALUES (1,sd."Team1Score1",sd."Team2Score1"), (2,sd."Team1Score2",sd."Team2Score2")) g(game_num, t1_raw, t2_raw))
             CROSS JOIN LATERAL ( VALUES (sd."Team1Id"), (sd."Team2Id")) t("Id"))
             CROSS JOIN LATERAL ( VALUES (
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN g.t1_raw
                            ELSE g.t2_raw
                        END,
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN g.t2_raw
                            ELSE g.t1_raw
                        END)) sc(my, opp))
          WHERE ((s."ScoringMode" = 'games_mode'::text) AND ((sc.my = s."PointsToWinGame") OR (sc.opp = s."PointsToWinGame") OR (sc.my = '-1'::integer) OR (sc.opp = '-1'::integer)))
        UNION ALL
         SELECT row_number() OVER (ORDER BY sd."Id",
                CASE
                    WHEN (t."Id" = sd."Team1Id") THEN 1
                    ELSE 2
                END) AS "Id",
            sd."Id" AS "ScheduleDivisionsId",
            s."Id" AS "SeasonId",
            s."LeagueId",
            sd."DivisionId",
            sd."TemplateWeekNumber" AS "WeekId",
            t."Id" AS "TeamId",
                CASE
                    WHEN (t."Id" = sd."Team1Id") THEN sd."Team2Id"
                    ELSE sd."Team1Id"
                END AS "OpposingTeamId",
            NULL::integer AS "GameNumber",
                CASE
                    WHEN (mf.i_forfeit AND (NOT mf.opp_forfeit)) THEN 0
                    WHEN ((NOT mf.i_forfeit) AND mf.opp_forfeit) THEN s."ForfeitOpponentPlusMinus"
                    WHEN (mf.i_forfeit AND mf.opp_forfeit) THEN 0
                    ELSE (COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0))
                END AS "PointsFor",
                CASE
                    WHEN (mf.i_forfeit AND (NOT mf.opp_forfeit)) THEN (- s."ForfeitPlusMinus")
                    WHEN ((NOT mf.i_forfeit) AND mf.opp_forfeit) THEN 0
                    WHEN (mf.i_forfeit AND mf.opp_forfeit) THEN (- s."ForfeitPlusMinus")
                    ELSE (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))
                END AS "PointsAgainst",
                CASE
                    WHEN mf.i_forfeit THEN false
                    WHEN ((NOT mf.i_forfeit) AND mf.opp_forfeit) THEN true
                    ELSE ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) > (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0)))
                END AS "IsWin",
                CASE
                    WHEN (mf.i_forfeit OR mf.opp_forfeit) THEN false
                    ELSE ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) = (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0)))
                END AS "IsTie",
                CASE
                    WHEN mf.i_forfeit THEN true
                    WHEN ((NOT mf.i_forfeit) AND mf.opp_forfeit) THEN false
                    ELSE ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) < (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0)))
                END AS "IsLoss",
            COALESCE(mf.i_forfeit, false) AS "IsForfeit",
            COALESCE((mf.opp_forfeit AND (NOT mf.i_forfeit)), false) AS "ByForfeit",
                CASE
                    WHEN (mf.i_forfeit AND (NOT mf.opp_forfeit)) THEN 0
                    WHEN ((NOT mf.i_forfeit) AND mf.opp_forfeit) THEN s."PointsForWin"
                    WHEN (mf.i_forfeit AND mf.opp_forfeit) THEN 0
                    WHEN ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) > (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))) THEN s."PointsForWin"
                    ELSE 0
                END AS "WinPTS",
                CASE
                    WHEN (mf.i_forfeit OR mf.opp_forfeit) THEN 0
                    WHEN ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) = (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))) THEN s."PointsForTie"
                    ELSE 0
                END AS "TiePTS",
                CASE
                    WHEN mf.i_forfeit THEN 0
                    WHEN mf.opp_forfeit THEN 0
                    WHEN ((COALESCE(sc.my1, 0) + COALESCE(sc.my2, 0)) < (COALESCE(sc.opp1, 0) + COALESCE(sc.opp2, 0))) THEN s."PointsForLoss"
                    ELSE 0
                END AS "LossPTS",
                CASE
                    WHEN mf.i_forfeit THEN s."PointsForNoShow"
                    ELSE 0
                END AS "ForfeitPoints"
           FROM ((((((public."ScheduleDivisions" sd
             JOIN public."Divisions" d ON ((d."Id" = sd."DivisionId")))
             JOIN public."Seasons" s ON ((s."Id" = d."SeasonId")))
             CROSS JOIN LATERAL ( VALUES (sd."Team1Id"), (sd."Team2Id")) t("Id"))
             CROSS JOIN LATERAL ( VALUES (
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN sd."Team1Score1"
                            ELSE sd."Team2Score1"
                        END,
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN sd."Team2Score1"
                            ELSE sd."Team1Score1"
                        END,
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN sd."Team1Score2"
                            ELSE sd."Team2Score2"
                        END,
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN sd."Team2Score2"
                            ELSE sd."Team1Score2"
                        END)) sc(my1, opp1, my2, opp2))
             CROSS JOIN LATERAL ( VALUES (((sd."Team1Score1" = '-1'::integer) OR (sd."Team1Score2" = '-1'::integer)),((sd."Team2Score1" = '-1'::integer) OR (sd."Team2Score2" = '-1'::integer)))) tf(t1_forfeit, t2_forfeit))
             CROSS JOIN LATERAL ( VALUES (
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN tf.t1_forfeit
                            ELSE tf.t2_forfeit
                        END,
                        CASE
                            WHEN (t."Id" = sd."Team1Id") THEN tf.t2_forfeit
                            ELSE tf.t1_forfeit
                        END)) mf(i_forfeit, opp_forfeit))
          WHERE ((s."ScoringMode" = 'match_score_mode'::text) AND ((sd."Team1Score1" IS NOT NULL) OR (sd."Team2Score1" IS NOT NULL) OR (sd."Team1Score2" IS NOT NULL) OR (sd."Team2Score2" IS NOT NULL)))) core
     JOIN public."Teams" tm ON ((tm."Id" = core."TeamId")))
     JOIN public."Teams" op ON ((op."Id" = core."OpposingTeamId")));


--
-- Name: SeasonCourts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SeasonCourts" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "CourtId" integer NOT NULL,
    "SortOrder" integer DEFAULT 0 NOT NULL
);


--
-- Name: SeasonCourts_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SeasonCourts" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SeasonCourts_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SeasonDaySlots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SeasonDaySlots" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "DaySlotId" integer NOT NULL
);


--
-- Name: SeasonDaySlots_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SeasonDaySlots" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SeasonDaySlots_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SeasonFees; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SeasonFees" (
    "Id" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "AmountOwing" numeric NOT NULL,
    "AmountPaid" numeric NOT NULL,
    "PaidDate" date,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: SeasonFees_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SeasonFees" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SeasonFees_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SeasonParameters; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SeasonParameters" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "Key" text NOT NULL,
    "Value" text NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL
);


--
-- Name: SeasonParameters_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SeasonParameters" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SeasonParameters_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SeasonTimeSlots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SeasonTimeSlots" (
    "Id" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "TimeSlotId" integer NOT NULL
);


--
-- Name: SeasonTimeSlots_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SeasonTimeSlots" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SeasonTimeSlots_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Seasons_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Seasons" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Seasons_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: SpareLists; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."SpareLists" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "Notes" text
);


--
-- Name: SpareLists_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."SpareLists" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."SpareLists_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Standings; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public."Standings" AS
 WITH agg AS (
         SELECT sc."TeamId",
            sc."TeamName",
            sc."DivisionId",
            sc."SeasonId",
            sc."LeagueId",
            (count(*))::integer AS "GamesPlayed",
            (count(DISTINCT sc."ScheduleDivisionsId"))::integer AS "MatchesPlayed",
            (count(*) FILTER (WHERE sc."IsWin"))::integer AS "Wins",
            (count(*) FILTER (WHERE sc."IsTie"))::integer AS "Ties",
            (count(*) FILTER (WHERE (sc."IsLoss" AND (NOT sc."IsForfeit"))))::integer AS "Losses",
            (count(*) FILTER (WHERE sc."IsForfeit"))::integer AS "Forfeits",
            (sum((((sc."WinPTS" + sc."TiePTS") + sc."LossPTS") + sc."ForfeitPoints")))::integer AS "StandingsPoints",
            (sum(sc."PlusMinus"))::integer AS "PlusMinus",
            (sum(sc."PointsFor"))::integer AS "PointsFor",
            (sum(sc."PointsAgainst"))::integer AS "PointsAgainst"
           FROM public."Scoring" sc
          GROUP BY sc."TeamId", sc."TeamName", sc."DivisionId", sc."SeasonId", sc."LeagueId"
        ), base_ranked AS (
         SELECT agg."TeamId",
            agg."TeamName",
            agg."DivisionId",
            agg."SeasonId",
            agg."LeagueId",
            agg."GamesPlayed",
            agg."MatchesPlayed",
            agg."Wins",
            agg."Ties",
            agg."Losses",
            agg."Forfeits",
            agg."StandingsPoints",
            agg."PlusMinus",
            agg."PointsFor",
            agg."PointsAgainst",
            s."FirstPlaceGuaranteed",
            (dense_rank() OVER (PARTITION BY agg."DivisionId" ORDER BY agg."StandingsPoints" DESC, agg."PlusMinus" DESC, agg."Wins" DESC))::integer AS base_div_rank
           FROM (agg
             JOIN public."Seasons" s ON ((s."Id" = agg."SeasonId")))
        ), h2h AS (
         SELECT sc."TeamId",
            sc."DivisionId",
            (COALESCE(sum(sc."PlusMinus"), (0)::bigint))::integer AS "H2HPlusMinus",
            (COALESCE(count(*) FILTER (WHERE sc."IsWin"), (0)::bigint))::integer AS "H2HWins"
           FROM ((public."Scoring" sc
             JOIN base_ranked br_me ON (((br_me."TeamId" = sc."TeamId") AND (br_me."DivisionId" = sc."DivisionId"))))
             JOIN base_ranked br_opp ON (((br_opp."TeamId" = sc."OpposingTeamId") AND (br_opp."DivisionId" = sc."DivisionId"))))
          WHERE (br_me.base_div_rank = br_opp.base_div_rank)
          GROUP BY sc."TeamId", sc."DivisionId"
        ), ranked AS (
         SELECT br."TeamId",
            br."TeamName",
            br."DivisionId",
            br."SeasonId",
            br."LeagueId",
            br."GamesPlayed",
            br."MatchesPlayed",
            br."Wins",
            br."Ties",
            br."Losses",
            br."Forfeits",
            br."StandingsPoints",
            br."PlusMinus",
            br."PointsFor",
            br."PointsAgainst",
            br."FirstPlaceGuaranteed",
            br.base_div_rank,
            COALESCE(h2h."H2HPlusMinus", 0) AS "H2HPlusMinus",
            COALESCE(h2h."H2HWins", 0) AS "H2HWins",
            (dense_rank() OVER (PARTITION BY br."DivisionId" ORDER BY br."StandingsPoints" DESC, br."PlusMinus" DESC, br."Wins" DESC, COALESCE(h2h."H2HPlusMinus", 0) DESC, COALESCE(h2h."H2HWins", 0) DESC))::integer AS "DivisionRank",
            (row_number() OVER (PARTITION BY br."DivisionId" ORDER BY br."StandingsPoints" DESC, br."PlusMinus" DESC, br."Wins" DESC, COALESCE(h2h."H2HPlusMinus", 0) DESC, COALESCE(h2h."H2HWins", 0) DESC))::integer AS "DivisionSeed"
           FROM (base_ranked br
             LEFT JOIN h2h ON (((h2h."TeamId" = br."TeamId") AND (h2h."DivisionId" = br."DivisionId"))))
        )
 SELECT "DivisionRank",
    "DivisionSeed",
        CASE
            WHEN "FirstPlaceGuaranteed" THEN (row_number() OVER (PARTITION BY "SeasonId" ORDER BY "DivisionSeed", "StandingsPoints" DESC, "PlusMinus" DESC, "Wins" DESC, "H2HPlusMinus" DESC, "H2HWins" DESC))::integer
            ELSE (row_number() OVER (PARTITION BY "SeasonId" ORDER BY "StandingsPoints" DESC, "PlusMinus" DESC, "Wins" DESC, "H2HPlusMinus" DESC, "H2HWins" DESC))::integer
        END AS "SeasonSeed",
    "TeamId",
    "TeamName",
    "DivisionId",
    "SeasonId",
    "LeagueId",
    "GamesPlayed",
    "MatchesPlayed",
    "Wins",
    "Ties",
    "Losses",
    "Forfeits",
    "StandingsPoints",
    "PlusMinus",
    "PointsFor",
    "PointsAgainst",
    "H2HPlusMinus",
    "H2HWins"
   FROM ranked;


--
-- Name: Stats; Type: VIEW; Schema: public; Owner: -
--

CREATE VIEW public."Stats" AS
 SELECT row_number() OVER (ORDER BY sd."Id",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN 1
            ELSE 2
        END) AS "Id",
    sd."Id" AS "ScheduleDivisionsId",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN sd."Team1Id"
            ELSE sd."Team2Id"
        END AS "TeamId",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN ((sd."Team1Score1" + sd."Team1Score2") - (sd."Team2Score1" + sd."Team2Score2"))
            ELSE ((sd."Team2Score1" + sd."Team2Score2") - (sd."Team1Score1" + sd."Team1Score2"))
        END AS "PlusMinus",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN (
            CASE
                WHEN (sd."Team1Score1" > sd."Team2Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team1Score2" > sd."Team2Score2") THEN 1
                ELSE 0
            END)
            ELSE (
            CASE
                WHEN (sd."Team2Score1" > sd."Team1Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team2Score2" > sd."Team1Score2") THEN 1
                ELSE 0
            END)
        END AS "Wins",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN (
            CASE
                WHEN (sd."Team1Score1" = sd."Team2Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team1Score2" = sd."Team2Score2") THEN 1
                ELSE 0
            END)
            ELSE (
            CASE
                WHEN (sd."Team2Score1" = sd."Team1Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team2Score2" = sd."Team1Score2") THEN 1
                ELSE 0
            END)
        END AS "Ties",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN (
            CASE
                WHEN (sd."Team1Score1" < sd."Team2Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team1Score2" < sd."Team2Score2") THEN 1
                ELSE 0
            END)
            ELSE (
            CASE
                WHEN (sd."Team2Score1" < sd."Team1Score1") THEN 1
                ELSE 0
            END +
            CASE
                WHEN (sd."Team2Score2" < sd."Team1Score2") THEN 1
                ELSE 0
            END)
        END AS "Losses",
        CASE
            WHEN (t."Id" = sd."Team1Id") THEN ((
            CASE
                WHEN (sd."Team1Score1" > sd."Team2Score1") THEN 2
                ELSE 0
            END +
            CASE
                WHEN (sd."Team1Score1" = sd."Team2Score1") THEN 1
                ELSE 0
            END) + (
            CASE
                WHEN (sd."Team1Score2" > sd."Team2Score2") THEN 2
                ELSE 0
            END +
            CASE
                WHEN (sd."Team1Score2" = sd."Team2Score2") THEN 1
                ELSE 0
            END))
            ELSE ((
            CASE
                WHEN (sd."Team2Score1" > sd."Team1Score1") THEN 2
                ELSE 0
            END +
            CASE
                WHEN (sd."Team2Score1" = sd."Team1Score1") THEN 1
                ELSE 0
            END) + (
            CASE
                WHEN (sd."Team2Score2" > sd."Team1Score2") THEN 2
                ELSE 0
            END +
            CASE
                WHEN (sd."Team2Score2" = sd."Team1Score2") THEN 1
                ELSE 0
            END))
        END AS "Points"
   FROM (public."ScheduleDivisions" sd
     CROSS JOIN LATERAL ( VALUES (sd."Team1Id"), (sd."Team2Id")) t("Id"));


--
-- Name: TeamApplicantMembers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamApplicantMembers" (
    "Id" integer NOT NULL,
    "TeamApplicantId" integer NOT NULL,
    "PlayerId" integer,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "Email" text,
    "Phone" text,
    "Notes" text,
    "CreatedPlayerId" integer
);


--
-- Name: TeamApplicantMembers_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."TeamApplicantMembers" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TeamApplicantMembers_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: TeamApplicants; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamApplicants" (
    "Id" integer NOT NULL,
    "LeagueId" integer NOT NULL,
    "SeasonId" integer NOT NULL,
    "GroupName" text NOT NULL,
    "ContactEmail" text,
    "ContactPhone" text,
    "PreferredDivisionId" integer,
    "Notes" text,
    "Status" text NOT NULL,
    "PlacedTeamId" integer,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: TeamApplicants_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."TeamApplicants" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TeamApplicants_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: TeamPlayers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamPlayers" (
    "Id" integer NOT NULL,
    "TeamId" integer NOT NULL,
    "PlayerId" integer NOT NULL,
    "Role" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "JoinedDate" date NOT NULL
);


--
-- Name: TeamPlayers_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."TeamPlayers" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TeamPlayers_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: TeamStandings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TeamStandings" (
    "Id" integer NOT NULL,
    "TeamId" integer NOT NULL,
    "DivisionId" integer NOT NULL,
    "Wins" integer NOT NULL,
    "Losses" integer NOT NULL,
    "Ties" integer NOT NULL,
    "NoShows" integer NOT NULL,
    "StandingsPoints" integer NOT NULL,
    "PointsFor" integer NOT NULL,
    "PointsAgainst" integer NOT NULL,
    "PlusMinus" integer NOT NULL,
    "DivisionRank" integer,
    "PlayoffSeed" integer
);


--
-- Name: TeamStandings_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."TeamStandings" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TeamStandings_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Teams_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."Teams" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."Teams_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: TimeSlots; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."TimeSlots" (
    "Id" integer NOT NULL,
    "Timeslot12h" text NOT NULL,
    "Timeslot24h" text NOT NULL,
    "SortOrder" integer,
    "IsActive" boolean NOT NULL
);


--
-- Name: TimeSlots_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public."TimeSlots" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."TimeSlots_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: ScheduleTemplateMatches Id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateMatches" ALTER COLUMN "Id" SET DEFAULT nextval('public."ScheduleTemplateMatches_Id_seq"'::regclass);


--
-- Name: ScheduleTemplateWeeks Id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateWeeks" ALTER COLUMN "Id" SET DEFAULT nextval('public."ScheduleTemplateWeeks_Id_seq"'::regclass);


--
-- Name: ScheduleTemplates Id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplates" ALTER COLUMN "Id" SET DEFAULT nextval('public."ScheduleTemplates_Id_seq"'::regclass);


--
-- Data for Name: Announcements; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Announcements" ("Id", "LeagueId", "Title", "Body", "PublishedAt", "ExpiresAt", "IsActive", "CreatedBy", "CreatedAt", "DesignJson") FROM stdin;
\.


--
-- Data for Name: AppParameters; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AppParameters" ("Id", "Key", "Value", "Description", "IsActive") FROM stdin;
1	ClubName	Golden Vista Bocce Club	Full name of the bocce club	t
2	LeagueCaptainName	Sven Hansen	Name of the league captain	t
3	LeagueCaptainEmail	gvbocceleague@gmail.com	Email address of the league captain	t
18	SpeedEntry.ForfeitKey	-	Speed Entry: key for single forfeit (-1)	t
22	SpeedEntry.Key12	.	Speed Entry: key for score 12	t
13	DivisionTeamsSplitPct	0.7088	\N	t
14	ShowReportDesigner	true	\N	t
15	ReportEditingFolder	C:\\Users\\svenh\\Documents\\BocceDocs\\Report Editing	\N	t
17	SpeedEntry.Enabled	true	Speed Entry mode on by default (true/false)	t
24	SpeedEntry.AutoSave.Enabled	true	Auto-save match when all 4 scores are entered (true/false)	t
25	SpeedEntry.SwapCourts	false	\N	t
16	InitiationFeeAmount	10.00	Default initiation fee charged to new players	t
19	SpeedEntry.DoubleForfeitKey	X	Speed Entry: key for double forfeit (both -1)	t
20	SpeedEntry.Key10	/	Speed Entry: key for score 10	t
21	SpeedEntry.Key11	*	Speed Entry: key for score 11	t
7	BackupFolder	C:\\Users\\svenh\\Documents\\BocceDocs\\Backups	Folder where database backups are saved	t
9	ReportPdfLocation	C:\\Users\\svenh\\Documents\\BocceDocs\\Reports	Default folder for PDF report exports	t
8	DocumentsFolder	C:\\Users\\svenh\\Documents\\BocceDocs\\Documents	Folder where imported document files are stored	t
12	ClubRulesDocument	C:\\Users\\svenh\\Documents\\BocceDocs\\Documents\\Golden Vista Bocce Ball League Rules.pdf	Path to the club rules PDF or Word document	t
6	CourtDisplay	letter	\N	t
23	SpeedEntry.SpecialChars.Enabled	false	Shortcut keys enabled for 10/11/12 and forfeits (true/false)	t
4	DefaultLeagueId	1	Default league for user context	t
5	DefaultSeasonId	13	Default season for user context	t
\.


--
-- Data for Name: ClubDocuments; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ClubDocuments" ("Id", "Title", "FileName", "DocType", "GoogleDocsUrl", "LeagueId", "UploadedAt", "Notes") FROM stdin;
6	Golden Vista Bocce Ball League Rules	Golden Vista Bocce Ball League Rules.pdf	pdf	\N	\N	2026-06-18 20:23:40.513725+00	\N
7	Golden Vista bocce ball rules 3	Golden Vista bocce ball rules 3.docx	docx	\N	\N	2026-06-18 20:24:14.824255+00	\N
\.


--
-- Data for Name: Courts; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Courts" ("Id", "CourtLetter", "Notes", "IsActive", "CourtNumber", "SortOrder") FROM stdin;
1	A	Even wonkier than 2	t	1	1
5	D	\N	t	4	4
2	B	Court is a bit wonky	t	2	2
4	C	\N	t	3	3
\.


--
-- Data for Name: DaySlots; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."DaySlots" ("Id", "DayName", "DayAbbr", "DayNbr", "IsActive") FROM stdin;
1	Monday	MON	1	t
2	Tuesday	TUE	2	t
3	Wednesday	WED	3	t
4	Thursday	THU	4	t
5	Friday	FRI	5	t
6	Saturday	SAT	6	t
7	Sunday	SUN	7	t
\.


--
-- Data for Name: Divisions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Divisions" ("Id", "SeasonId", "Name", "ShortName", "SortName", "PlayersPerTeamMinimum", "PlayersPerTeamMaximum", "TeamsInDivision", "DaySlotId", "TimeSlotId", "IsActive") FROM stdin;
10	1	Thursday 9:00 AM	Th-0900	4-0900	\N	\N	4	4	3	t
4	1	Tuesday 9:00 AM	Tu-0900	2-0900	\N	\N	4	2	3	t
8	1	Wednesday 1:00 PM	We-1300	3-1300	\N	\N	4	3	11	t
13	1	Friday 9:00 AM	Fr-0900	5-0900	\N	\N	4	5	3	t
162	13	Friday 3:30 PM	Fr-1530	5-1530	\N	\N	4	5	16	t
157	13	Monday 1:00 PM	Mo-1300	1-1300	4	5	4	1	11	t
154	13	Thursday 9:00 AM	Th-0900	4-0900	\N	\N	4	4	3	t
156	13	Wednesday 9:00 AM	We-0900	3-0900	\N	\N	4	3	3	t
158	13	Wednesday 3:30 PM	We-1530	3-1530	\N	\N	4	3	16	t
163	13	Friday 1:00 PM	Fr-1300	5-1300	\N	\N	4	5	11	t
160	13	Tuesday 1:00 PM	Tu-1300	2-1300	\N	\N	4	2	11	t
152	13	Tuesday 9:00 AM	Tu-0900	2-0900	\N	\N	4	2	3	t
161	13	Thursday 1:00 PM	Th-1300	4-1300	\N	\N	4	4	11	t
159	13	Wednesday 1:00 PM	We-1300	3-1300	\N	\N	4	3	11	t
165	13	Monday 3:30 PM	Mo-1530	1-1530	4	5	0	1	16	f
166	13	Tuesday 3:30 PM	Tu-1530	2-1530	4	5	0	2	16	f
167	13	Thursday 3:30 PM	Th-1530	4-1530	4	5	0	4	16	f
7	1	Wednesday 9:00 AM	We-0900	3-0900	\N	\N	4	3	3	t
2	1	Monday 1:00 PM	Mo-1300	1-1300	4	5	4	1	11	t
9	1	Wednesday 3:30 PM	We-1530	3-1530	\N	\N	4	3	16	t
5	1	Tuesday 1:00 PM	Tu-1300	2-1300	\N	\N	4	2	11	t
11	1	Thursday 1:00 PM	Th-1300	4-1300	\N	\N	4	4	11	t
15	1	Friday 3:30 PM	Fr-1530	5-1530	\N	\N	4	5	16	t
14	1	Friday 1:00 PM	Fr-1300	5-1300	\N	\N	4	5	11	t
1	1	Monday 9:00 AM	Mo-0900	1-0900	4	5	4	1	3	t
155	13	Monday 9:00 AM	Mo-0900	1-0900	4	5	6	1	3	t
153	13	Friday 9:00 AM	Fr-0900	5-0900	\N	\N	6	5	3	t
\.


--
-- Data for Name: EmailListMembers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EmailListMembers" ("Id", "EmailListId", "PlayerId", "IsActive") FROM stdin;
\.


--
-- Data for Name: EmailLists; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EmailLists" ("Id", "LeagueId", "Name", "Description", "IsActive") FROM stdin;
\.


--
-- Data for Name: EmailLogs; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."EmailLogs" ("Id", "SentBy", "LeagueId", "Subject", "Body", "RecipientCount", "SentAt") FROM stdin;
\.


--
-- Data for Name: Games; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Games" ("Id", "MatchId", "GameNumber", "Team1Score", "Team2Score", "IsForfeit", "EnteredBy", "EnteredAt") FROM stdin;
\.


--
-- Data for Name: InitiationFees; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."InitiationFees" ("Id", "PlayerId", "AmountOwing", "AmountPaid", "PaidDate", "Notes", "CreatedAt") FROM stdin;
1	239	10.00	0	\N	\N	2026-06-24 18:55:58.436095+00
17	14	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.514844+00
10	7	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.508132+00
11	8	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.50931+00
12	9	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.510246+00
13	10	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.511231+00
14	11	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.512161+00
15	12	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.513092+00
19	16	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.51677+00
20	17	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.517658+00
21	18	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.518557+00
22	19	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.519503+00
23	20	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.520467+00
24	21	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.521402+00
2	77	10.00	0	\N	\N	2026-06-24 18:55:58.499613+00
52	49	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.545284+00
53	50	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.546097+00
54	51	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.54691+00
55	52	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.547742+00
56	53	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.548558+00
57	54	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.549488+00
58	55	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.550621+00
59	56	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.551682+00
60	57	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.552745+00
3	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.501283+00
4	2	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.502637+00
5	3	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.503615+00
6	4	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.504537+00
7	5	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.505451+00
8	78	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.50635+00
9	6	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.50728+00
16	13	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.513999+00
18	15	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.515804+00
25	22	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.522343+00
26	23	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.523299+00
27	24	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.524318+00
28	25	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.52558+00
29	26	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.526635+00
30	27	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.527477+00
31	28	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.528318+00
32	29	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.529086+00
33	30	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.529872+00
34	31	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.53064+00
35	32	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.531412+00
36	33	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.532181+00
37	34	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.53292+00
38	35	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.533656+00
39	36	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.534469+00
40	37	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.535216+00
41	38	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.535984+00
42	39	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.536735+00
43	40	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.537504+00
44	41	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.538257+00
45	42	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.539024+00
46	43	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.539778+00
47	44	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.540547+00
48	45	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.541557+00
49	46	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.542355+00
50	47	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.543514+00
51	48	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.544462+00
61	58	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.553836+00
62	59	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.554904+00
63	60	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.556259+00
64	61	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.558072+00
65	62	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.559187+00
66	63	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.560355+00
67	64	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.56153+00
68	65	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.562638+00
69	66	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.563733+00
70	67	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.564903+00
71	68	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.56605+00
72	69	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.567147+00
73	70	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.568287+00
74	71	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.569378+00
75	72	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.570495+00
76	73	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.585367+00
77	74	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.587969+00
78	75	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.589277+00
79	76	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.590472+00
80	79	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.591658+00
81	80	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.593325+00
82	81	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.594511+00
83	82	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.595687+00
84	83	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.597166+00
85	84	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.598621+00
86	85	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.599795+00
87	86	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.600939+00
88	87	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.602496+00
89	88	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.603676+00
90	89	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.604809+00
91	90	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.606056+00
92	91	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.607241+00
93	92	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.608417+00
94	93	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.609565+00
95	94	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.610869+00
96	95	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.612127+00
97	96	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.613418+00
98	97	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.614712+00
99	98	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.615985+00
100	99	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.617116+00
101	100	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.61822+00
102	101	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.619476+00
103	102	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.620868+00
104	103	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.622035+00
105	104	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.623209+00
106	105	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.624333+00
107	106	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.625707+00
108	107	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.626914+00
109	108	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.628066+00
110	109	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.629305+00
111	110	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.630799+00
112	111	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.631971+00
113	112	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.633161+00
114	113	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.634312+00
115	114	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.635625+00
116	115	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.636816+00
117	116	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.638068+00
118	117	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.639268+00
119	118	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.640559+00
120	119	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.641757+00
121	120	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.642928+00
122	121	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.644294+00
123	122	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.645773+00
124	123	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.647137+00
125	124	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.648413+00
126	125	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.649702+00
127	126	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.651162+00
128	127	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.652583+00
129	128	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.654109+00
130	129	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.656021+00
131	130	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.657288+00
132	131	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.658535+00
133	132	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.659837+00
134	133	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.661339+00
135	134	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.662767+00
136	135	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.664066+00
137	136	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.665437+00
138	137	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.666891+00
139	138	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.668348+00
140	139	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.669663+00
141	140	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.670949+00
142	141	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.672582+00
143	142	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.673963+00
144	143	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.675436+00
145	144	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.677019+00
146	145	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.678391+00
147	146	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.679766+00
148	147	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.681324+00
149	148	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.682656+00
150	149	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.684021+00
151	150	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.685329+00
152	151	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.686753+00
153	152	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.688143+00
154	153	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.68947+00
155	154	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.690828+00
156	155	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.692515+00
157	156	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.693897+00
158	157	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.69524+00
159	158	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.698229+00
160	159	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.699898+00
161	160	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.701225+00
162	161	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.7024+00
163	162	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.703845+00
164	163	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.705092+00
165	164	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.70622+00
166	165	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.707327+00
167	166	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.708445+00
168	167	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.709938+00
169	168	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.711241+00
170	169	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.71255+00
171	170	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.713707+00
172	171	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.715201+00
173	172	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.716351+00
174	173	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.717467+00
175	174	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.718599+00
176	175	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.720028+00
177	176	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.721276+00
178	177	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.722381+00
179	178	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.723469+00
180	179	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.724563+00
181	180	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.725922+00
182	181	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.727237+00
183	182	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.728607+00
184	183	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.729761+00
185	184	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.731123+00
186	185	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.732425+00
187	186	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.733574+00
188	187	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.734763+00
189	188	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.735925+00
190	189	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.737212+00
191	190	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.738464+00
192	191	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.739614+00
193	192	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.740725+00
194	193	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.742331+00
195	194	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.744161+00
196	195	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.745547+00
197	196	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.746752+00
198	197	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.747938+00
199	198	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.749425+00
200	199	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.750862+00
201	200	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.752312+00
202	201	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.753778+00
203	202	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.755433+00
204	203	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.756866+00
205	204	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.759925+00
206	205	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.761311+00
207	206	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.763148+00
208	207	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.764476+00
209	208	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.765641+00
210	209	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.767108+00
211	210	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.768385+00
212	211	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.77282+00
213	212	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.774125+00
214	213	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.775573+00
215	214	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.776857+00
216	215	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.778045+00
217	216	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.779238+00
218	217	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.780398+00
219	218	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.782228+00
220	219	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.783517+00
221	220	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.784726+00
222	221	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.786289+00
223	222	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.787623+00
224	223	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.78886+00
225	224	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.79023+00
226	225	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.791925+00
227	226	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.793241+00
228	227	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.794421+00
229	228	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.795601+00
230	229	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.796779+00
231	230	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.798358+00
232	231	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.799648+00
233	232	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.800855+00
234	233	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.802124+00
235	234	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.803353+00
236	235	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.804876+00
237	236	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.806172+00
238	237	10.00	10.00	2026-06-24	\N	2026-06-24 18:55:58.80755+00
239	240	10.00	0	\N	\N	2026-07-04 02:38:52.613922+00
240	241	10.00	0	\N	\N	2026-07-04 02:39:15.911664+00
\.


--
-- Data for Name: LeagueParameters; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LeagueParameters" ("Id", "LeagueId", "Key", "Value", "Description", "IsActive") FROM stdin;
1	1	court_display	number	How courts are labelled in schedules: 'number' or 'letter'	t
2	2	court_display	number	How courts are labelled in schedules: 'number' or 'letter'	t
\.


--
-- Data for Name: Leagues; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Leagues" ("Id", "Name", "Description", "RulesText", "IsActive", "CreatedAt", "PlayersPerTeamMinimum", "PlayersPerTeamMaximum", "MaxTeamsInDivision") FROM stdin;
1	Spring League	Original Bocce League - January to February	\N	t	2026-06-04 20:43:48.396396+00	4	5	8
2	Fall League	October to November Season	Martin Mingay will be club captain	t	2026-06-05 00:54:11.692916+00	4	5	8
\.


--
-- Data for Name: LookingForTeamDivisions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LookingForTeamDivisions" ("Id", "LookingForTeamId", "DivisionId") FROM stdin;
\.


--
-- Data for Name: LookingForTeamGroups; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LookingForTeamGroups" ("Id", "LeagueId", "SeasonId", "CreatedAt", "Name", "GroupLeaderId") FROM stdin;
12	1	13	2026-06-26 23:30:48.598957+00	Gibson.0	106
10	1	13	2026-06-26 23:23:25.03819+00	Aman.0	102
17	1	13	2026-06-27 02:13:25.619546+00	Stith.0	115
13	1	13	2026-06-26 23:39:48.765609+00	Mingay.0	114
\.


--
-- Data for Name: LookingForTeamPreferredDays; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LookingForTeamPreferredDays" ("Id", "LookingForTeamId", "DaySlotId") FROM stdin;
\.


--
-- Data for Name: LookingForTeamPreferredTimes; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LookingForTeamPreferredTimes" ("Id", "LookingForTeamId", "TimeSlotId") FROM stdin;
\.


--
-- Data for Name: LookingForTeams; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."LookingForTeams" ("Id", "LeagueId", "PlayerId", "TeamId", "SeasonId", "Notes", "PreferredTeamId", "RegisteredDate", "LookingForTeamGroupId") FROM stdin;
114	1	148	\N	13	\N	\N	\N	13
115	1	203	\N	13	\N	\N	\N	17
116	1	202	\N	13	\N	\N	\N	17
102	1	239	\N	13	\N	\N	\N	10
104	1	77	\N	13	\N	\N	\N	10
105	1	233	\N	13	\N	\N	\N	12
106	1	76	\N	13	\N	\N	\N	12
109	1	139	648	13	\N	\N	\N	13
107	1	43	694	13	\N	\N	\N	13
108	1	44	694	13	\N	\N	\N	13
\.


--
-- Data for Name: MatchTeamResults; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."MatchTeamResults" ("Id", "MatchId", "TeamId", "Wins", "Losses", "Ties", "NoShows", "StandingsPoints", "PointsFor", "PointsAgainst", "PlusMinus") FROM stdin;
\.


--
-- Data for Name: Matches; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Matches" ("Id", "ScheduleWeekId", "Team1Id", "Team2Id", "CourtId", "ScheduledDate", "ScheduledTime", "GamesPlayed", "Status", "EnteredBy", "EnteredAt") FROM stdin;
\.


--
-- Data for Name: NewIdeas; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."NewIdeas" ("Id", "Idea", "DateCreated", "DateCollected") FROM stdin;
1	setup a backup folder parameter as an app parameter that dictates where the backups will go.  We will need other folders to	2026-06-09 23:26:34.135907+00	2026-06-09 23:26:48.042961+00
3	Like to Move teams from one division to another.	2026-06-22 22:29:23.897116+00	\N
4	Fix Idea machine so Add Button shows properly.   Also Mark Collect and Delete idea should be on the Add Idea line, and smaller.  Space where they occupy should be turned over to the idea	2026-06-22 22:30:31.481968+00	\N
5	Double click idea to show it in its entirety in a window on the right side	2026-06-22 22:31:03.14191+00	\N
6	allow ideas to be copied so i can put them into VSCODE	2026-06-22 22:31:27.694023+00	\N
\.


--
-- Data for Name: PlayerRoles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayerRoles" ("Id", "RoleName") FROM stdin;
0	Player
1	Fundraiser
2	Treasurer
3	Secretary
4	Vice President
5	President
6	Stats
\.


--
-- Data for Name: Players; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Players" ("Id", "FirstName", "LastName", "Email", "Phone", "LotNumber", "IsActive", "CreatedAt", "PartnerPlayerId", "Role") FROM stdin;
1	Jerry	Anderson	\N	\N	765	t	2026-06-04 20:43:48.482867+00	\N	0
2	Sandy	Anderson	anderksandy@gmail.com	(507) 221-0768	619	t	2026-06-04 20:43:48.496474+00	\N	0
5	Sara	Austin	cs970austin@gmail.com	(719) 557-9261	891	t	2026-06-04 20:43:48.497335+00	\N	0
13	Brenda	Belgois	\N	\N	\N	t	2026-06-04 20:43:48.499442+00	\N	0
16	Donna	Bengtson	sbengtson6@gmail.com	(320) 291-4994	459	t	2026-06-04 20:43:48.50004+00	\N	0
19	Rita	Blosser	blosser58@gmail.com	(785) 640-0664	147	t	2026-06-04 20:43:48.500653+00	\N	0
22	Fred	Bregar	fred.bregar@aol.com	(719) 568-6714	762	t	2026-06-04 20:43:48.501493+00	\N	0
23	Diane	Breitbach	diane.breitbach@gmail.com	(563) 599-3743	488	t	2026-06-04 20:43:48.501761+00	\N	0
24	Steve	Breitbach	sbreits49@gmail.com	(563) 599-3743	488	t	2026-06-04 20:43:48.502064+00	\N	0
25	Dennis	Bridgeman	thephoneman51@yahoo.com	(480) 226-3657	599	t	2026-06-04 20:43:48.502336+00	\N	0
26	Rock	Bridges	rockbridges@gmail.com	(641) 590-4120	4	t	2026-06-04 20:43:48.502619+00	\N	0
27	Sue	Bridges	suziebridges@gmail.com	(641) 590-2443	4	t	2026-06-04 20:43:48.502846+00	\N	0
28	Dennis	Brown	janethelen75@gmail.com	(780) 335-0422	923	t	2026-06-04 20:43:48.503054+00	\N	0
29	Janet	Brown	janethelen75@gmail.com	(780) 335-0422	923	t	2026-06-04 20:43:48.503326+00	\N	0
30	Ben	Browning	greg.browning@shaw.ca	(403) 702-1846	895	t	2026-06-04 20:43:48.503628+00	\N	0
31	Greg	Browning	greg.g.browning@gmail.com	(403) 702-1846	895	t	2026-06-04 20:43:48.503888+00	\N	0
32	Mary	Bulthuis	marellen48@gmail.com	(302) 266-0369	655	t	2026-06-04 20:43:48.504107+00	\N	0
33	Ron	Bulthuis	rbulthuis49@gmail.com	(701) 740-1920	655	t	2026-06-04 20:43:48.504329+00	\N	0
34	Bob	Burgi	rburgs2@icloud.com	(480) 323-8574	29	t	2026-06-04 20:43:48.504536+00	\N	0
35	Jenny	Burgi	rburgs2@icloud.com	(480) 323-8574	29	t	2026-06-04 20:43:48.504736+00	\N	0
36	Fredra	Carlson	rickfredrac@gmail.com	(785) 342-7376	678	t	2026-06-04 20:43:48.504982+00	\N	0
37	Deb	Casper	dcasper58@gmail.com	(612) 910-8539	212	t	2026-06-04 20:43:48.505318+00	\N	0
38	Paul	Casper	pcasper1056@gmail.com	(763) 420-7936	212	t	2026-06-04 20:43:48.505616+00	\N	0
39	Leslie	Chamberlin	roycham59@gmail.com	(480) 466-3231	300	t	2026-06-04 20:43:48.505832+00	\N	0
40	Roy	Chamberlin	roycham59@gmail.com	(480) 466-3231	300	t	2026-06-04 20:43:48.506016+00	\N	0
41	Jim	Champoux	soxfan031966@gmail.com	(508) 330-2511	247	t	2026-06-04 20:43:48.50621+00	\N	0
42	Maureen	Champoux	maureen.Champoux@yahoo.com	(508) 330-2511	247	t	2026-06-04 20:43:48.506439+00	\N	0
43	Greg	Clark	ngregclark@gmail.com	(623) 556-3210	\N	t	2026-06-04 20:43:48.506647+00	\N	0
44	Nona	Clark	ngregclark@gmail.com	(623) 556-3210	\N	t	2026-06-04 20:43:48.506823+00	\N	0
45	Betty	Cross	blc10500@yahoo.com	(989) 390-3769	311	t	2026-06-04 20:43:48.507083+00	\N	0
46	Sheryl	Dahlke	shermrichp@gmail.com	(651) 200-0987	419	t	2026-06-04 20:43:48.507343+00	\N	0
47	Karen	Daniel	karendaniel@hotmail.com	(970) 542-0842	528	t	2026-06-04 20:43:48.507646+00	\N	0
48	RW	Daniel	golddaniel@hotmail.com	(970) 451-7406	528	t	2026-06-04 20:43:48.507953+00	\N	0
49	Brenda	Davis	bkdavis29@yahoo.com	(218) 390-6348	637	t	2026-06-04 20:43:48.508286+00	\N	0
51	Ron	Davis	trdavis725@yahoo.com	(218) 390-6348	637	t	2026-06-04 20:43:48.508845+00	\N	0
53	Dwayne	DeBoer	deboer11@netscape.net	(712) 541-7958	269	t	2026-06-04 20:43:48.50941+00	\N	0
54	Denise	Delaney	denisedelaney71@gmail.com	(605) 695-9977	284	t	2026-06-04 20:43:48.509686+00	\N	0
55	William	Delaney	williamdelaney75@gmail.com	(605) 695-9495	284	t	2026-06-04 20:43:48.509949+00	\N	0
56	Cheryl	Donohoe	gregdonohoe@shaw.ca	(778) 686-4660	1035	t	2026-06-04 20:43:48.510162+00	\N	0
57	Greg	Donohoe	gregdonohoe@shaw.ca	(778) 686-4660	1035	t	2026-06-04 20:43:48.510394+00	\N	0
58	Deb	Dougherty	ddougherty1915@gmail.com	(317) 502-5515	683	t	2026-06-04 20:43:48.510667+00	\N	0
59	Mike	Dougherty	ddougherty1915@gmail.com	(317) 502-5517	683	t	2026-06-04 20:43:48.510935+00	\N	0
60	Jerry	Eastin	jk_east10@yahoo.com	(719) 691-5474	699	t	2026-06-04 20:43:48.511179+00	\N	0
61	Karen	Eastin	jk_east10@yahoo.com	(719) 688-0474	699	t	2026-06-04 20:43:48.511396+00	\N	0
62	Lowell	Eichenberger	lowelleichenberger@gmail.com	(515) 420-3891	751	t	2026-06-04 20:43:48.511604+00	\N	0
63	Sue	Ellsworth	docnsue@yahoo.com	(515) 320-1896	935	t	2026-06-04 20:43:48.511814+00	\N	0
65	Pete	Fontana	ylipat@yahoo.com	(530) 218-6932	766	t	2026-06-04 20:43:48.512296+00	\N	0
66	Dennis	Forbeck	forbeckellyn11.gmail.com	(920) 205-8653	171	t	2026-06-04 20:43:48.512583+00	\N	0
67	Ellyn	Forbeck	forbeckellyn11.gmail.com	(920) 205-8653	171	t	2026-06-04 20:43:48.512868+00	\N	0
68	Gale	Fossen	jcfossen@hotmail.com	(701) 893-8516	95	t	2026-06-04 20:43:48.513137+00	\N	0
69	Janet	Fossen	jcfossen@hotmail.com	(701) 866-5108	95	t	2026-06-04 20:43:48.513373+00	\N	0
70	Eileen	Frendenburg	vefredenburg@gmail.com	(315) 882-3956	874	t	2026-06-04 20:43:48.513583+00	\N	0
71	Bridget	Gaff	bagaff58@gmail.com	(260) 615-8213	520	t	2026-06-04 20:43:48.513809+00	\N	0
72	Cindy	Gavin	cjgavin@netwtc.net	(563) 299-2522	819	t	2026-06-04 20:43:48.51411+00	\N	0
73	Jim	Gavin	gavinjames@netwtc.net	(563) 506-5112	819	t	2026-06-04 20:43:48.514334+00	\N	0
74	Ann	Gentry	tagentry51@att.net	(479) 629-3673	\N	t	2026-06-04 20:43:48.514551+00	\N	0
75	Ted	Gentry	tagentry51@att.net	(479) 629-3673	\N	t	2026-06-04 20:43:48.514832+00	\N	0
76	Chris	Gibson	hootchris@thurston.com	(360) 790-2223	526	t	2026-06-04 20:43:48.515097+00	\N	0
79	Kevin	Gillett	kevincnbrj5@gmail.com	(319) 493-1280	500	t	2026-06-04 20:43:48.515769+00	\N	0
80	Susan	Gillett	cnbrj5@gmail.com	(319) 493-1330	500	t	2026-06-04 20:43:48.515985+00	\N	0
81	Dan	Glaza	chitown830@gmail.com	(630) 217-1906	86	t	2026-06-04 20:43:48.516187+00	\N	0
82	Donna	Gohde	dgohde53563@gmail.com	(608) 436-0146	108	t	2026-06-04 20:43:48.516392+00	\N	0
3	Bonnie	Ask	askbon74@gmail.com	(320) 760-9097	270	t	2026-06-04 20:43:48.496798+00	4	0
7	Kairi	Banitt	dalebanitt@gmail.com	(651) 764-0902	\N	t	2026-06-04 20:43:48.498008+00	78	0
6	Darryl	Banitt	dlbanitt@gmail.com	(507) 298-0936	461	t	2026-06-04 20:43:48.49777+00	8	0
8	Lori	Banitt	dlbanitt@gmail.com	(507) 298-0972	461	t	2026-06-04 20:43:48.498319+00	6	0
9	Darryl	Bauer	7dabdab7@gmail.com	(369) 391-0393	986	t	2026-06-04 20:43:48.498542+00	10	0
10	Kellie	Bauer	kellybauer@frontier.com	(360) 672-0633	986	t	2026-06-04 20:43:48.498752+00	9	0
14	Craig	Belleau	craigorsue@gmail.com	(920) 227-7859	\N	t	2026-06-04 20:43:48.499632+00	15	0
15	Sue	Belleau	craigorsue@gmail.com	(920) 664-2416	651	t	2026-06-04 20:43:48.499811+00	14	0
17	Evy	Billings	evybillings@earthlink.net	(712) 380-2300	961	t	2026-06-04 20:43:48.500235+00	18	0
18	Ken	Billings	evybillings@earthlink.net	(712) 380-2300	961	t	2026-06-04 20:43:48.500456+00	17	0
20	Ruth	Braun	ruth.braun@sasktel.com	(306) 731-7965	396	t	2026-06-04 20:43:48.500986+00	21	0
21	Wes	Braun	wes.braun@sasktel.com	(306) 535-5883	396	t	2026-06-04 20:43:48.501282+00	20	0
64	Pat	Fontana	ylipat@yahoo.com	(530) 218-6932	766	t	2026-06-04 20:43:48.512019+00	\N	4
50	Dennis	Davis	suziq201084@gmail.com	(651) 308-1541	\N	t	2026-06-04 20:43:48.508587+00	52	0
52	Sue	Davis	suziq201084@gmail.com	(651) 308-1541	\N	t	2026-06-04 20:43:48.509091+00	50	0
83	Robert	Gohde	rgohde53563@gmail.com	(608) 868-3422	108	t	2026-06-04 20:43:48.516592+00	\N	0
84	Linda	Goulet	deerhunter491@aol.com	(989) 450-6188	632	t	2026-06-04 20:43:48.516785+00	\N	0
85	Norm	Goulet	deerhunter491@aol.com	(989) 450-1458	632	t	2026-06-04 20:43:48.516982+00	\N	0
86	Bill	Greenlee	greenleebk@gmail.com	(303) 478-0449	373	t	2026-06-04 20:43:48.517205+00	\N	0
87	Kay	Greenlee	greenleebk@gmail.com	(303) 478-0449	373	t	2026-06-04 20:43:48.517516+00	\N	0
88	Dan	Grill	bbqgrill@hotmail.com	(507) 236-5338	824	t	2026-06-04 20:43:48.517778+00	\N	0
89	Julie	Grill	momgrill@hotmail.com	(507) 236-2251	824	t	2026-06-04 20:43:48.518009+00	\N	0
90	Carol	Grothus	kgrothous@woh.rr.com	(419) 863-9123	635	t	2026-06-04 20:43:48.518255+00	\N	0
91	Bob	Guck	rjguck@gmail.com	(320) 290-5398	72	t	2026-06-04 20:43:48.518463+00	\N	0
92	Cheryl	Guck	cpguck@gmail.com	(320) 492-0836	72	t	2026-06-04 20:43:48.518714+00	\N	0
93	Greg	Gutzman	jeangreggutzman@gmail.com	(605) 291-2198	245	t	2026-06-04 20:43:48.518914+00	\N	0
94	Jean	Gutzman	jeangreggutzman@gmail.com	(605) 270-9399	245	t	2026-06-04 20:43:48.519127+00	\N	0
95	Chari	Hamilton	charihamilton68@gmail.com	(507) 530-7762	1010	t	2026-06-04 20:43:48.519324+00	\N	0
96	Doug	Hamilton	charihamilton68@gmail.com	(507) 530-7762	1010	t	2026-06-04 20:43:48.519508+00	\N	0
97	Barry	Hanke	\N	(780) 221-5224	262	t	2026-06-04 20:43:48.51973+00	\N	0
100	Bob	Haugerud	bobhaugerud5@gmail.com	(715) 377-8495	818	t	2026-06-04 20:43:48.520373+00	\N	0
101	Nancie	Hineline	thehinelines@comcast.com	(303) 668-4729	544	t	2026-06-04 20:43:48.520577+00	\N	0
102	Carol	Hoewisch	choewisch@yahoo.com	(920) 540-4141	878	t	2026-06-04 20:43:48.520756+00	\N	0
103	Bob	Holmes	bobholmes76@gmail.com	(507) 381-1488	698	t	2026-06-04 20:43:48.520932+00	\N	0
104	Wendy	Holmes	xrwendy@hotmail.com	(507) 381-1485	698	t	2026-06-04 20:43:48.521141+00	\N	0
105	Paul	Hultgren	gwing2@msn.com	(605) 270-3526	49	t	2026-06-04 20:43:48.521355+00	\N	0
106	Ruth	Hultgren	gwing2@msn.com	(605) 291-9517	49	t	2026-06-04 20:43:48.521591+00	\N	0
107	Don	James	aeknowlton29@aol.com	(989) 928-4331	399	t	2026-06-04 20:43:48.521797+00	\N	0
108	Duane	Jangula	duanejangula@gmail.com	(701) 425-5358	689	t	2026-06-04 20:43:48.521975+00	\N	0
109	Lon	Kaste	lonscaligirl@yahoo.com	(701) 337-0987	387	t	2026-06-04 20:43:48.522149+00	\N	0
110	Shelley	Kaste	lonscaligirl@yahoo.com	(701) 337-0988	387	t	2026-06-04 20:43:48.522349+00	\N	0
111	Arnold	Kayl	\N	(208) 304-2220	911	t	2026-06-04 20:43:48.522535+00	\N	0
112	Donna	Keefer	glawayne@aol.com	(989) 857-9459	931	t	2026-06-04 20:43:48.522744+00	\N	0
113	Gary	Keefer	glawayne@aol.com	(989) 287-2335	931	t	2026-06-04 20:43:48.522951+00	\N	0
114	Bonnie	Kennett	b_fkennett@yahoo.com	(651) 325-7028	818	t	2026-06-04 20:43:48.523155+00	\N	0
115	Carolyn	King	caking@thomasandsons.biz	(989) 915-9562	332	t	2026-06-04 20:43:48.523346+00	\N	0
116	Dawn	Klatt	hwklatt@outlook.com	(602) 526-1845	235	t	2026-06-04 20:43:48.523616+00	\N	0
117	Harley	Klatt	hwklatt@outlook.com	(602) 526-1845	235	t	2026-06-04 20:43:48.523805+00	\N	0
118	Edward	Klitzke	choewisch@yahoo.com	(920) 810-7676	878	t	2026-06-04 20:43:48.523985+00	\N	0
119	Anne	Knowlton	aeknowlton29@aol.com	(989) 928-4331	399	t	2026-06-04 20:43:48.524271+00	\N	0
120	Marsha	Kopecky	mrkopecky3@gmail.com	(402) 394-8294	680	t	2026-06-04 20:43:48.524577+00	\N	0
121	Richard	Kopecky	mrkopecky15@gmail.com	(402) 394-8364	680	t	2026-06-04 20:43:48.524836+00	\N	0
122	Gary	Kost	dgkost@nvc.net	(605) 228-8821	443	t	2026-06-04 20:43:48.525085+00	\N	0
123	Jon	Kragt	jonkragt@gmail.com	(402) 679-1175	154	t	2026-06-04 20:43:48.525296+00	\N	0
124	Pam	Kragt	jonkragt@gmail.com	(402) 679-1175	154	t	2026-06-04 20:43:48.525504+00	\N	0
125	Marlene	Kucera	kuceramr@gmail.com	(319) 404-4776	358	t	2026-06-04 20:43:48.525742+00	\N	0
126	Rich	Kucera	kuchrjk1952@gmail.com	(319) 404-4776	358	t	2026-06-04 20:43:48.526048+00	\N	0
127	Denise	Kulesa	eddkulesa@yahoo.com	(763) 248-4934	107	t	2026-06-04 20:43:48.526364+00	\N	0
128	Ed	Kulesa	eddkulesa@yahoo.com	(763) 248-4934	107	t	2026-06-04 20:43:48.526661+00	\N	0
129	Sam	Landon	samlandon76@gmail.com	(952) 649-7054	339	t	2026-06-04 20:43:48.526947+00	\N	0
130	Tammy	Landon	tammaralandon@gmail.com	(952) 649-7054	339	t	2026-06-04 20:43:48.527175+00	\N	0
131	Marlene	Lane	marlenelane6@gmail.com	(403) 988-7881	210	t	2026-06-04 20:43:48.527423+00	\N	0
132	Chris	Leeper	chrisleeper429@yahoo.com	(360) 280-2474	91	t	2026-06-04 20:43:48.527616+00	\N	0
133	Tom	Linahon	tom.linahon@gmail.com	(641) 529-7403	959	t	2026-06-04 20:43:48.527841+00	\N	0
134	Joe	Litzinger	joeblueangel4@gmail.com	(218) 779-9349	309	t	2026-06-04 20:43:48.528059+00	\N	0
135	Mark	Loch	markjoanloch@hotmail.com	(320) 290-6586	666	t	2026-06-04 20:43:48.52849+00	\N	0
136	Linda	Locken	llocken@nvc.net	(605) 380-9802	234	t	2026-06-04 20:43:48.528797+00	\N	0
137	Mary	Mahoney	irishmmjd@msn.com	(515) 238-3557	340	t	2026-06-04 20:43:48.529083+00	\N	0
138	Michael	Mahoney	irishmmjd@msn.com	(515) 238-3557	340	t	2026-06-04 20:43:48.529333+00	\N	0
139	Kelly	Maxfield	kmaxfield10@yahoo.com	(780) 255-9198	\N	t	2026-06-04 20:43:48.52953+00	\N	0
140	Darlene	May	\N	(503) 739-3673	68	t	2026-06-04 20:43:48.529715+00	\N	0
141	Rick	May	\N	(503) 739-3673	68	t	2026-06-04 20:43:48.529893+00	\N	0
142	Marilyn	McBride	mcbridemarilyn123@gmail.com	(616) 862-0812	226	t	2026-06-04 20:43:48.530073+00	\N	0
143	Kathy	McCune	kmc062509@yahoo.com	(316) 641-2918	376	t	2026-06-04 20:43:48.530324+00	\N	0
144	Kendall	McCune	kmc062509@yahoo.com	(316) 641-2918	376	t	2026-06-04 20:43:48.530602+00	\N	0
145	Karen	McGee	terrymcgee6@gmail.com	(320) 469-1069	657	t	2026-06-04 20:43:48.530861+00	\N	0
146	Terry	McGee	terrymcgee6@gmail.com	(320) 469-1069	657	t	2026-06-04 20:43:48.531109+00	\N	0
147	Doris	Mingay	djmingay@mt.net	(406) 980-2011	1028	t	2026-06-04 20:43:48.531327+00	\N	0
148	Martin	Mingay	mmingay1955@gmail.com	(406) 980-1033	1028	t	2026-06-04 20:43:48.531575+00	\N	0
149	Jeanne	Mitchell	jemitchell6@gmail.com	(303) 325-3388	251	t	2026-06-04 20:43:48.531769+00	\N	0
150	Mary	Nalan	mjnalan@yahoo.com	(641) 430-9886	959	t	2026-06-04 20:43:48.531937+00	\N	0
151	Doral	Nall	nalscanyon@yahoo,com	(480) 227-4689	368	t	2026-06-04 20:43:48.532101+00	\N	0
152	Bob	Nelson	kathyragona1962@gmail.com	(563) 505-7830	240	t	2026-06-04 20:43:48.532277+00	\N	0
153	Boyd	Nelson	boydnelson16@icloud.com	(715) 781-9203	642	t	2026-06-04 20:43:48.532515+00	\N	0
154	Kathy	Nelson	kathyragona1962@gmail.com	(563) 505-7830	642	t	2026-06-04 20:43:48.532778+00	\N	0
155	Donna	O'Connor	jimoconnor@westelk.com	(970) 361-2649	164	t	2026-06-04 20:43:48.533051+00	\N	0
156	Jim	O'Connor	jimoconnor@westelk.com	(970) 275-9294	164	t	2026-06-04 20:43:48.53331+00	\N	0
157	Mike	Petschl	mbpetschl@myctl.net	(612) 296-8313	833	t	2026-06-04 20:43:48.533504+00	\N	0
158	Gary	Piper	pied4piper@aol.com	(319) 321-3872	181	t	2026-06-04 20:43:48.533675+00	\N	0
159	Paula	Piper	pied4piper@aol.com	(319) 321-3872	181	t	2026-06-04 20:43:48.533851+00	\N	0
161	Curt	Posselt	cposs0210@gmail.com	(920) 585-2039	151	t	2026-06-04 20:43:48.534295+00	\N	0
162	Bruce	Preston	bruceboss1577@gmail.com	(260) 438-3069	915	t	2026-06-04 20:43:48.534608+00	\N	0
163	Deb	Preston	vikings136472@gmail.com	(260) 437-0245	915	t	2026-06-04 20:43:48.534805+00	\N	0
164	Mary	Ramos	\N	(480) 330-8134	\N	t	2026-06-04 20:43:48.534982+00	\N	0
165	Jan	Reiner	ljreiner55@hotmail.com	(612) 868-4033	845	t	2026-06-04 20:43:48.535162+00	\N	0
166	Lyle	Reiner	ljreiner55@hotmail.com	(612) 868-4033	845	t	2026-06-04 20:43:48.535374+00	\N	0
169	Barb	Roberts	brobertsmn@gmail.com	(218) 371-1304	152	t	2026-06-04 20:43:48.535936+00	\N	0
170	Ann	Roebbeke	danojd280@outlook.com	(612) 616-2029	826	t	2026-06-04 20:43:48.536105+00	\N	0
171	Dan	Roebbeke	danojd280@outlook.com	(612) 616-2029	826	t	2026-06-04 20:43:48.536269+00	\N	0
172	Glenn	Roiger	glenroiger@hotmail.com	(320) 834-2027	760	t	2026-06-04 20:43:48.536442+00	\N	0
173	Mary	Roiger	roiger@gctel.net	(320) 491-7464	760	t	2026-06-04 20:43:48.536654+00	\N	0
174	Mike	Roslin	sandyroslin@gmail.com	(612) 201-3217	643	t	2026-06-04 20:43:48.536856+00	\N	0
175	Jack	Roth	jackroth243@gmail.com	(719) 691-2431	566	t	2026-06-04 20:43:48.53704+00	\N	0
176	Sue	Roth	jackroth243@gmail.com	(719) 688-7684	566	t	2026-06-04 20:43:48.53725+00	\N	0
177	Mike	Rowan	mikejrowan@shaw.ca	(403) 870-8972	756	t	2026-06-04 20:43:48.537451+00	\N	0
178	Mary	Russell	wyrealty@tctwest.net	(307) 272-0004	585	t	2026-06-04 20:43:48.537649+00	\N	0
179	Char	Satter	charlene.satter44@gmail.com	(507) 360-1783	574	t	2026-06-04 20:43:48.537824+00	\N	0
180	Greg	Schiller	julie.schiller@hotmail.com	(612) 703-6504	679	t	2026-06-04 20:43:48.537988+00	\N	0
181	Julie	Schiller	julie.schiller@hotmail.com	(612) 703-6504	679	t	2026-06-04 20:43:48.538159+00	\N	0
182	Harvey	Schilling	harvey3140@gmail.com	(701) 226-1860	722	t	2026-06-04 20:43:48.538432+00	\N	0
183	Norm	Schnider	nschnider2@gmail.com	(208) 215-8879	862	t	2026-06-04 20:43:48.538692+00	\N	0
184	Brenda	Seidal	brendabelbas@gmail.com	(204) 851-3378	\N	t	2026-06-04 20:43:48.538889+00	\N	0
185	Don	Seidal	doncc60@yahoo.com	(810) 325-0573	383	t	2026-06-04 20:43:48.53911+00	\N	0
186	Barb	Shinnick	barbjshinnick@yahoo.com	(612) 360-8865	641	t	2026-06-04 20:43:48.539314+00	\N	0
187	Jeff	Shoemaker	\N	(425) 299-8828	\N	t	2026-06-04 20:43:48.539494+00	\N	0
188	Kris	Shoemaker	\N	(425) 299-8828	\N	t	2026-06-04 20:43:48.539668+00	\N	0
189	Karen	Smith	klsmith103@gmail.com	(480) 519-0100	712	t	2026-06-04 20:43:48.539915+00	\N	0
190	Stan	Smith	ssrambo69@gmail.com	(972) 288-6535	644	t	2026-06-04 20:43:48.540234+00	\N	0
191	Jerry	Snyder	yvetteandjerry@comcast.net	(509) 309-4835	336	t	2026-06-04 20:43:48.540546+00	\N	0
192	Yvette	Snyder	yvetteandjerry@comcast.net	(509) 309-4836	336	t	2026-06-04 20:43:48.540807+00	\N	0
193	Ken	Sobolik	marysobolik@yahoo.com	(701) 740-5074	998	t	2026-06-04 20:43:48.541103+00	\N	0
194	Mary	Sobolik	marysobolik@yahoo.com	(701) 740-1920	998	t	2026-06-04 20:43:48.541442+00	\N	0
195	Marianne	Squibb	m1squibb@gmail.com	(360) 421-3720	433	t	2026-06-04 20:43:48.541746+00	\N	0
196	Joleen	Squire	tjjsquire@mediacombb.net	(952) 826-9647	367	t	2026-06-04 20:43:48.541967+00	\N	0
197	Tom	Squire	tjjsquire@mediacombb.net	(952) 826-9647	367	t	2026-06-04 20:43:48.542183+00	\N	0
198	Karen	Stewart	lstewart@gwtc.net	(605) 660-7876	352	t	2026-06-04 20:43:48.542384+00	\N	0
199	Larry	Stewart	lstewart@gwtc.net	(605) 660-7876	352	t	2026-06-04 20:43:48.542629+00	\N	0
200	Lynette	Still	lasstill@gmail.com	(701) 642-8921	772	t	2026-06-04 20:43:48.542947+00	\N	0
201	Richard	Still	rwstill53@yahoo.com	(701) 899-1670	772	t	2026-06-04 20:43:48.543143+00	\N	0
202	Elaine	Stith	girlsclub2008@gmail.com	(405) 615-6415	483	t	2026-06-04 20:43:48.543332+00	\N	0
203	Ken	Stith	boysclub2008@gmail.com	(405) 620-3722	483	t	2026-06-04 20:43:48.543559+00	\N	0
204	Jerry	Stork	jerobstorkies@gmail.com	(509) 590-6418	939	t	2026-06-04 20:43:48.543753+00	\N	0
205	Roberta	Stork	jerobstorkies@gmail.com	\N	939	t	2026-06-04 20:43:48.544021+00	\N	0
206	Bob	Strong	jrstrong@q.com	(612) 799-3070	432	t	2026-06-04 20:43:48.54432+00	\N	0
207	George	Swigert	gmswigert@aol.com	(515) 290-3849	908	t	2026-06-04 20:43:48.544614+00	\N	0
208	Marge	Swigert	gmswigert@aol.com	(515) 290-3849	908	t	2026-06-04 20:43:48.544863+00	\N	0
209	Jack	Symes	jack@richardstransport.com	(306) 519-9694	\N	t	2026-06-04 20:43:48.545065+00	\N	0
210	Marcia	Symes	jack@richardstransport.com	(306) 519-9694	\N	t	2026-06-04 20:43:48.545295+00	\N	0
211	Lynn	Taylor	sltaylor@prodigy.net	(530) 828-7065	988	t	2026-06-04 20:43:48.545502+00	\N	0
212	Scott	Taylor	papascotttaylor@gmail.com	(530) 965-1710	988	t	2026-06-04 20:43:48.545756+00	\N	0
213	Dana	Thingelstad	thingelstad31@gmail.com	(406) 253-3262	187	t	2026-06-04 20:43:48.545969+00	\N	0
215	Marie	Thomson	mariethomson@midco.net	(701) 371-0814	487	t	2026-06-04 20:43:48.546321+00	\N	0
216	Beth	Thorson	bathorson@yahoo.com	(253) 370-1091	625	t	2026-06-04 20:43:48.546488+00	\N	0
217	Judy	Trenkamp	judytrenkamp5@gmail.com	(419) 204-5569	603	t	2026-06-04 20:43:48.546717+00	\N	0
218	Janet	Tryzinski	triz4@sbcglobal.net	(480) 466-3231	\N	t	2026-06-04 20:43:48.547081+00	\N	0
219	Mario	Tryzinski	roycham59@gmail.com	(480) 466-3231	\N	t	2026-06-04 20:43:48.547393+00	\N	0
220	Glenn	Turner	grt12354@gmail.com	(715) 213-5563	633	t	2026-06-04 20:43:48.547605+00	\N	0
221	Sandy	VanLishout	harleygirl_57@hotmail.com	(701) 226-0048	718	t	2026-06-04 20:43:48.547852+00	\N	0
222	Cindy	Virlee	carv091077@gmail.com	(920) 619-1533	\N	t	2026-06-04 20:43:48.548054+00	\N	0
223	Randy	Virlee	carv091077@gmail.com	(920) 619-1533	\N	t	2026-06-04 20:43:48.54826+00	\N	0
224	Jerry	Wagner	nj2wags@aol.com	(303) 680-7232	720	t	2026-06-04 20:43:48.548455+00	\N	0
225	Nancy	Wagner	nj2wags@aol.com	(303) 596-3264	720	t	2026-06-04 20:43:48.548653+00	\N	0
226	Ken	Wallace	kenmar22@aol.com	\N	\N	t	2026-06-04 20:43:48.548839+00	\N	0
227	Joan	Ward	joaniem.ward@gmail.com	(419) 706-1388	374	t	2026-06-04 20:43:48.549037+00	\N	0
228	Mike	Ward	wardjm1@netzero.com	(419) 660-1338	374	t	2026-06-04 20:43:48.549219+00	\N	0
229	Ron	Ward	camper474@hotmail.com	(616) 550-8350	474	t	2026-06-04 20:43:48.549403+00	\N	0
230	Dorothy	Watkins	dwatkins54@live.com	(541) 777-0314	871	t	2026-06-04 20:43:48.5496+00	\N	0
231	Jim	Weller	jimweller601@gmail.com	(701) 667-1284	718	t	2026-06-04 20:43:48.54979+00	\N	0
232	Rhonda	Whitten	rj.whitten@icloud.com	(780) 886-0144	737	t	2026-06-04 20:43:48.549969+00	\N	0
233	Cathy	Williams	\N	\N	\N	t	2026-06-04 20:43:48.550159+00	\N	0
234	Kathy	Williams	williams2055@yahoo.com	(920) 407-2225	674	t	2026-06-04 20:43:48.550338+00	\N	0
235	Kim	Wilson	klbeachbums@hotmail.com	(785) 545-5760	618	t	2026-06-04 20:43:48.550531+00	\N	0
236	Joyce	Wilvers	roland.wilvers@gmail.com	(780) 904-9129	\N	t	2026-06-04 20:43:48.550758+00	\N	0
237	Roland	Wilvers	roland.wilvers@gmail.com	(780) 475-4503	\N	t	2026-06-04 20:43:48.551055+00	\N	0
160	Connie	Posselt	cposs0210@gmail.com	(920) 585-2039	151	t	2026-06-04 20:43:48.534013+00	\N	3
214	Debbie	Thingelstad	debbiet31@hotmail.com	(406) 212-6205	187	t	2026-06-04 20:43:48.546148+00	\N	2
98	Susan	Hansen	susanannhansen@shaw.ca	(403) 803-5749	239	t	2026-06-04 20:43:48.519953+00	99	0
4	Phil	Ask	ask.phil71@gmail.com	(320) 219-1740	270	t	2026-06-04 20:43:48.497053+00	3	0
77	Debra	Aman	dgaman@abe.midco.net	(605) 380-6559	929	t	2026-06-04 20:43:48.515313+00	239	0
239	Dale	Aman	daman81@abe.midco.net	(605) 216-4487	929	t	2026-06-04 20:43:48.55158+00	77	0
78	Dale	Banitt	dalebanitt@gmail.com	(651) 764-0902	\N	t	2026-06-04 20:43:48.515563+00	7	0
11	Gail	Beech	beech.gail@gmail.com	(701) 640-1366	853	t	2026-06-04 20:43:48.499009+00	12	0
12	Paul	Beech	beech.paul2010@gmail.com	(701) 640-3466	853	t	2026-06-04 20:43:48.499212+00	11	0
99	Sven	Hansen	svenhansen@shaw.ca	(403) 542-6689	239	t	2026-06-04 20:43:48.520164+00	98	6
167	Mike	Richey	richeymichael26@gmail.com	(815) 764-5320	622	t	2026-06-04 20:43:48.535583+00	\N	5
168	Terri	Richey	terririchey19@gmail.com	(815) 761-3535	622	t	2026-06-04 20:43:48.535765+00	\N	1
240	Dave	Olsen	\N	\N	\N	t	2026-07-04 02:38:52.486111+00	241	0
241	Teri	Olsen	\N	\N	\N	t	2026-07-04 02:39:15.80321+00	240	0
\.


--
-- Data for Name: PlayoffConfigs; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffConfigs" ("Id", "SeasonId", "MatchDurationMins", "DisplayMode", "IsGenerated", "TiebreakerBalls") FROM stdin;
1	1	120	ScaleToFit	t	1
2	13	90	ScaleToFit	t	1
\.


--
-- Data for Name: PlayoffDayParams; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffDayParams" ("Id", "PlayoffConfigId", "DayNumber", "GameDate", "StartTime", "EndTime", "MatchLengthMins") FROM stdin;
211	1	1	2026-02-28	08:30:00	19:00:00	150
212	1	2	2026-03-01	12:00:00	18:00:00	180
213	1	3	2026-03-02	08:30:00	18:00:00	120
214	1	4	2026-03-03	08:30:00	18:00:00	120
215	1	5	2026-03-04	08:30:00	18:00:00	120
216	2	1	2027-02-27	08:30:00	18:00:00	120
217	2	2	2027-02-28	08:30:00	18:00:00	120
218	2	3	2027-03-01	08:30:00	18:00:00	120
219	2	4	2027-03-02	08:30:00	18:00:00	120
\.


--
-- Data for Name: PlayoffGames; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffGames" ("Id", "PlayoffMatchId", "GameNumber", "Team1Score", "Team2Score", "IsForfeit", "EnteredBy", "EnteredAt") FROM stdin;
\.


--
-- Data for Name: PlayoffMatches; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffMatches" ("Id", "SeasonId", "PlayoffRoundId", "Seed1", "Seed2", "Team1Id", "Team2Id", "CourtId", "ScheduledDate", "ScheduledTime", "Status", "WinnerId", "EnteredBy", "EnteredAt", "BracketSlot", "IsBye", "NextMatchId", "NextMatchIsTop") FROM stdin;
514	1	156	0	\N	\N	\N	1	2026-03-01	15:00:00	scheduled	\N	\N	\N	0	f	\N	t
515	1	155	0	\N	\N	\N	1	2026-03-01	12:00:00	scheduled	\N	\N	\N	0	f	514	t
516	1	155	0	\N	\N	\N	2	2026-03-01	12:00:00	scheduled	\N	\N	\N	1	f	514	f
517	1	154	1	\N	247	\N	1	2026-02-28	13:30:00	scheduled	\N	\N	\N	0	f	515	t
518	1	154	4	\N	228	\N	2	2026-02-28	13:30:00	scheduled	\N	\N	\N	1	f	515	f
519	1	154	3	\N	251	\N	1	2026-02-28	16:00:00	scheduled	\N	\N	\N	2	f	516	t
520	1	154	2	\N	233	\N	2	2026-02-28	16:00:00	scheduled	\N	\N	\N	3	f	516	f
521	1	153	8	9	244	271	1	2026-02-28	08:30:00	scheduled	\N	\N	\N	0	f	517	f
522	1	153	5	12	262	236	2	2026-02-28	08:30:00	scheduled	\N	\N	\N	1	f	518	f
523	1	153	6	11	266	260	1	2026-02-28	11:00:00	scheduled	\N	\N	\N	2	f	519	f
524	1	153	7	10	257	239	2	2026-02-28	11:00:00	scheduled	\N	\N	\N	3	f	520	f
525	13	160	0	\N	\N	\N	5	2027-02-27	14:30:00	scheduled	\N	\N	\N	0	f	\N	t
526	13	159	0	\N	\N	\N	5	2027-02-27	12:30:00	scheduled	\N	\N	\N	0	f	525	t
527	13	159	0	\N	\N	\N	4	2027-02-27	12:30:00	scheduled	\N	\N	\N	1	f	525	f
528	13	158	1	\N	\N	\N	5	2027-02-27	10:30:00	scheduled	\N	\N	\N	0	f	526	t
529	13	158	4	\N	\N	\N	4	2027-02-27	10:30:00	scheduled	\N	\N	\N	1	f	526	f
530	13	158	3	\N	\N	\N	2	2027-02-27	10:30:00	scheduled	\N	\N	\N	2	f	527	t
531	13	158	2	\N	\N	\N	1	2027-02-27	10:30:00	scheduled	\N	\N	\N	3	f	527	f
532	13	157	8	9	\N	\N	5	2027-02-27	08:30:00	scheduled	\N	\N	\N	0	f	528	f
533	13	157	5	12	\N	\N	4	2027-02-27	08:30:00	scheduled	\N	\N	\N	1	f	529	f
534	13	157	6	11	\N	\N	2	2027-02-27	08:30:00	scheduled	\N	\N	\N	2	f	530	f
535	13	157	7	10	\N	\N	1	2027-02-27	08:30:00	scheduled	\N	\N	\N	3	f	531	f
\.


--
-- Data for Name: PlayoffRounds; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffRounds" ("Id", "SeasonId", "RoundNumber", "RoundName", "MatchDate", "DurationBetweenRoundsMins", "EndTime", "StartTime") FROM stdin;
153	1	1	Round 1	2026-02-28	30	\N	08:30:00
154	1	2	Quarter-Finals	2026-02-28	30	\N	13:30:00
155	1	3	Semi-Finals	2026-03-01	30	\N	12:00:00
156	1	4	Final	2026-03-01	30	\N	15:00:00
157	13	1	Round 1	2027-02-27	30	\N	08:30:00
158	13	2	Quarter-Finals	2027-02-27	30	\N	10:30:00
159	13	3	Semi-Finals	2027-02-27	30	\N	12:30:00
160	13	4	Final	2027-02-27	30	\N	14:30:00
\.


--
-- Data for Name: PlayoffSeedings; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PlayoffSeedings" ("Id", "SeasonId", "Seed", "TeamId") FROM stdin;
873	1	1	247
874	1	2	233
875	1	3	251
876	1	4	228
877	1	5	262
878	1	6	266
879	1	7	257
880	1	8	244
881	1	9	271
882	1	10	239
883	1	11	260
884	1	12	236
\.


--
-- Data for Name: ReportParameters; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ReportParameters" ("Id", "ReportId", "ParameterName", "ParameterLabel", "IsRequired", "DefaultSource", "DisplayOrder") FROM stdin;
\.


--
-- Data for Name: Reports; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Reports" ("Id", "Name", "ReportPath", "Description", "DisplayOrder", "IsActive", "CreatedDate", "ModifiedDate") FROM stdin;
1	Team Listing	Reports/TeamListing.rdlc	Lists all teams and rosters for the current season, organized by division and time slot.	1	t	2026-06-13 19:51:21.871299+00	2026-06-13 19:51:21.871299+00
2	Schedules - Generic	Reports/SchedulesGeneric.rdlc	Prints all schedule templates for the current season, showing weekly court assignments.	2	t	2026-06-13 19:51:21.871367+00	2026-06-13 19:51:21.871367+00
\.


--
-- Data for Name: ScheduleDivisions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ScheduleDivisions" ("Id", "DivisionId", "TemplateId", "TemplateWeekNumber", "MatchDate", "Team1Id", "Team2Id", "CourtId", "CreatedDate", "Team1Score1", "Team1Score2", "Team2Score1", "Team2Score2") FROM stdin;
5339	1	76	6	2026-02-16	227	228	2	2026-07-04 01:25:33.872201+00	\N	\N	\N	\N
5340	1	76	6	2026-02-16	226	229	1	2026-07-04 01:25:33.872203+00	\N	\N	\N	\N
5341	1	76	7	2026-02-23	226	227	1	2026-07-04 01:25:33.872206+00	\N	\N	\N	\N
5342	1	76	7	2026-02-23	228	229	2	2026-07-04 01:25:33.872208+00	\N	\N	\N	\N
5343	1	76	8	2026-03-02	227	229	1	2026-07-04 01:25:33.872211+00	\N	\N	\N	\N
5344	1	76	8	2026-03-02	226	228	2	2026-07-04 01:25:33.872213+00	\N	\N	\N	\N
5355	2	76	6	2026-02-16	231	232	2	2026-07-04 01:25:33.980476+00	\N	\N	\N	\N
5356	2	76	6	2026-02-16	230	233	1	2026-07-04 01:25:33.980478+00	\N	\N	\N	\N
5357	2	76	7	2026-02-23	230	231	1	2026-07-04 01:25:33.980481+00	\N	\N	\N	\N
5358	2	76	7	2026-02-23	232	233	2	2026-07-04 01:25:33.980484+00	\N	\N	\N	\N
5359	2	76	8	2026-03-02	231	233	1	2026-07-04 01:25:33.980486+00	\N	\N	\N	\N
5360	2	76	8	2026-03-02	230	232	2	2026-07-04 01:25:33.980489+00	\N	\N	\N	\N
5371	4	76	6	2026-02-17	235	236	2	2026-07-04 01:25:34.090487+00	\N	\N	\N	\N
5372	4	76	6	2026-02-17	234	237	1	2026-07-04 01:25:34.09049+00	\N	\N	\N	\N
5373	4	76	7	2026-02-24	234	235	1	2026-07-04 01:25:34.090493+00	\N	\N	\N	\N
5374	4	76	7	2026-02-24	236	237	2	2026-07-04 01:25:34.090495+00	\N	\N	\N	\N
5375	4	76	8	2026-03-03	235	237	1	2026-07-04 01:25:34.090498+00	\N	\N	\N	\N
5376	4	76	8	2026-03-03	234	236	2	2026-07-04 01:25:34.090501+00	\N	\N	\N	\N
5387	5	76	6	2026-02-17	239	240	2	2026-07-04 01:25:34.198495+00	\N	\N	\N	\N
5388	5	76	6	2026-02-17	238	241	1	2026-07-04 01:25:34.198499+00	\N	\N	\N	\N
5389	5	76	7	2026-02-24	238	239	1	2026-07-04 01:25:34.198502+00	\N	\N	\N	\N
5390	5	76	7	2026-02-24	240	241	2	2026-07-04 01:25:34.198505+00	\N	\N	\N	\N
5391	5	76	8	2026-03-03	239	241	1	2026-07-04 01:25:34.198509+00	\N	\N	\N	\N
5392	5	76	8	2026-03-03	238	240	2	2026-07-04 01:25:34.198513+00	\N	\N	\N	\N
5403	7	76	6	2026-02-18	243	244	2	2026-07-04 01:25:34.298594+00	\N	\N	\N	\N
5404	7	76	6	2026-02-18	242	245	1	2026-07-04 01:25:34.298598+00	\N	\N	\N	\N
5405	7	76	7	2026-02-25	242	243	1	2026-07-04 01:25:34.298603+00	\N	\N	\N	\N
5406	7	76	7	2026-02-25	244	245	2	2026-07-04 01:25:34.298607+00	\N	\N	\N	\N
5407	7	76	8	2026-03-04	243	245	1	2026-07-04 01:25:34.298611+00	\N	\N	\N	\N
5408	7	76	8	2026-03-04	242	244	2	2026-07-04 01:25:34.298616+00	\N	\N	\N	\N
5419	8	76	6	2026-02-18	247	248	2	2026-07-04 01:25:34.403434+00	\N	\N	\N	\N
5420	8	76	6	2026-02-18	246	249	1	2026-07-04 01:25:34.403437+00	\N	\N	\N	\N
5421	8	76	7	2026-02-25	246	247	1	2026-07-04 01:25:34.403441+00	\N	\N	\N	\N
5422	8	76	7	2026-02-25	248	249	2	2026-07-04 01:25:34.403444+00	\N	\N	\N	\N
5423	8	76	8	2026-03-04	247	249	1	2026-07-04 01:25:34.403447+00	\N	\N	\N	\N
5424	8	76	8	2026-03-04	246	248	2	2026-07-04 01:25:34.403451+00	\N	\N	\N	\N
5435	9	76	6	2026-02-18	251	252	2	2026-07-04 01:25:34.514685+00	\N	\N	\N	\N
5330	1	76	1	2026-01-12	228	229	2	2026-07-04 01:25:33.872174+00	12	6	8	12
5345	2	76	1	2026-01-12	230	231	1	2026-07-04 01:25:33.980398+00	12	12	8	6
5346	2	76	1	2026-01-12	232	233	2	2026-07-04 01:25:33.980445+00	4	2	12	12
5362	4	76	1	2026-01-13	236	237	2	2026-07-04 01:25:34.090453+00	12	8	6	12
5377	5	76	1	2026-01-13	238	239	1	2026-07-04 01:25:34.198394+00	11	7	12	12
5378	5	76	1	2026-01-13	240	241	2	2026-07-04 01:25:34.198459+00	5	6	12	12
5393	7	76	1	2026-01-14	242	243	1	2026-07-04 01:25:34.298471+00	7	12	12	3
5394	7	76	1	2026-01-14	244	245	2	2026-07-04 01:25:34.298547+00	5	9	12	12
5409	8	76	1	2026-01-14	246	247	1	2026-07-04 01:25:34.403341+00	10	0	12	12
5425	9	76	1	2026-01-14	250	251	1	2026-07-04 01:25:34.514571+00	5	8	12	12
5426	9	76	1	2026-01-14	252	253	2	2026-07-04 01:25:34.514648+00	9	9	12	12
5331	1	76	2	2026-01-19	226	228	1	2026-07-04 01:25:33.87218+00	5	12	12	9
5332	1	76	2	2026-01-19	227	229	2	2026-07-04 01:25:33.872183+00	12	11	5	12
5347	2	76	2	2026-01-19	230	232	1	2026-07-04 01:25:33.980451+00	4	12	12	8
5348	2	76	2	2026-01-19	231	233	2	2026-07-04 01:25:33.980455+00	4	12	12	8
5364	4	76	2	2026-01-20	235	237	2	2026-07-04 01:25:34.090464+00	12	11	10	12
5379	5	76	2	2026-01-20	238	240	1	2026-07-04 01:25:34.198465+00	12	12	9	9
5380	5	76	2	2026-01-20	239	241	2	2026-07-04 01:25:34.19847+00	12	11	7	12
5395	7	76	2	2026-01-21	242	244	1	2026-07-04 01:25:34.298556+00	7	1	12	12
5396	7	76	2	2026-01-21	243	245	2	2026-07-04 01:25:34.298561+00	12	9	11	12
5411	8	76	2	2026-01-21	246	248	1	2026-07-04 01:25:34.4034+00	12	2	9	12
5427	9	76	2	2026-01-21	250	252	1	2026-07-04 01:25:34.514655+00	12	11	9	12
5428	9	76	2	2026-01-21	251	253	2	2026-07-04 01:25:34.51466+00	12	12	1	3
5333	1	76	3	2026-01-26	227	228	1	2026-07-04 01:25:33.872186+00	7	9	12	12
5334	1	76	3	2026-01-26	226	229	2	2026-07-04 01:25:33.872189+00	6	12	12	2
5349	2	76	3	2026-01-26	231	232	1	2026-07-04 01:25:33.980458+00	12	7	6	12
5365	4	76	3	2026-01-27	235	236	1	2026-07-04 01:25:34.090468+00	10	8	12	12
5366	4	76	3	2026-01-27	234	237	2	2026-07-04 01:25:34.09047+00	12	10	6	12
5381	5	76	3	2026-01-27	239	240	1	2026-07-04 01:25:34.198474+00	12	12	2	4
5397	7	76	3	2026-01-28	243	244	1	2026-07-04 01:25:34.298567+00	10	5	12	12
5382	5	76	3	2026-01-27	238	241	2	2026-07-04 01:25:34.198477+00	9	12	12	10
5398	7	76	3	2026-01-28	242	245	2	2026-07-04 01:25:34.298572+00	12	12	10	4
5413	8	76	3	2026-01-28	247	248	1	2026-07-04 01:25:34.40341+00	12	12	10	9
5429	9	76	3	2026-01-28	251	252	1	2026-07-04 01:25:34.514664+00	12	12	10	6
5430	9	76	3	2026-01-28	250	253	2	2026-07-04 01:25:34.514668+00	5	12	12	7
5336	1	76	4	2026-02-02	228	229	1	2026-07-04 01:25:33.872194+00	12	5	9	12
5335	1	76	4	2026-02-02	226	227	2	2026-07-04 01:25:33.872191+00	5	3	12	12
5352	2	76	4	2026-02-02	232	233	1	2026-07-04 01:25:33.980467+00	8	4	12	12
5351	2	76	4	2026-02-02	230	231	2	2026-07-04 01:25:33.980464+00	12	12	4	9
5367	4	76	4	2026-02-03	234	235	2	2026-07-04 01:25:34.090475+00	7	12	12	3
5384	5	76	4	2026-02-03	240	241	1	2026-07-04 01:25:34.198485+00	2	7	12	12
5383	5	76	4	2026-02-03	238	239	2	2026-07-04 01:25:34.198481+00	6	12	12	6
5400	7	76	4	2026-02-04	244	245	1	2026-07-04 01:25:34.29858+00	12	12	2	9
5399	7	76	4	2026-02-04	242	243	2	2026-07-04 01:25:34.298576+00	12	9	7	12
5416	8	76	4	2026-02-04	248	249	1	2026-07-04 01:25:34.403421+00	8	7	12	12
5432	9	76	4	2026-02-04	252	253	1	2026-07-04 01:25:34.514675+00	4	2	12	12
5431	9	76	4	2026-02-04	250	251	2	2026-07-04 01:25:34.514671+00	12	0	10	12
5338	1	76	5	2026-02-09	227	229	1	2026-07-04 01:25:33.872199+00	12	12	9	5
5337	1	76	5	2026-02-09	226	228	2	2026-07-04 01:25:33.872196+00	4	7	12	12
5354	2	76	5	2026-02-09	231	233	1	2026-07-04 01:25:33.980473+00	3	6	12	12
5353	2	76	5	2026-02-09	230	232	2	2026-07-04 01:25:33.98047+00	10	6	12	12
5369	4	76	5	2026-02-10	234	236	2	2026-07-04 01:25:34.090481+00	12	3	7	12
5386	5	76	5	2026-02-10	239	241	1	2026-07-04 01:25:34.198492+00	7	12	12	2
5402	7	76	5	2026-02-11	243	245	1	2026-07-04 01:25:34.29859+00	8	12	12	7
5385	5	76	5	2026-02-10	238	240	2	2026-07-04 01:25:34.198489+00	8	12	12	9
5401	7	76	5	2026-02-11	242	244	2	2026-07-04 01:25:34.298586+00	6	10	12	12
5418	8	76	5	2026-02-11	247	249	1	2026-07-04 01:25:34.403427+00	7	12	12	4
5434	9	76	5	2026-02-11	251	253	1	2026-07-04 01:25:34.514681+00	12	12	7	3
5433	9	76	5	2026-02-11	250	252	2	2026-07-04 01:25:34.514678+00	12	12	3	7
5436	9	76	6	2026-02-18	250	253	1	2026-07-04 01:25:34.514688+00	\N	\N	\N	\N
5437	9	76	7	2026-02-25	250	251	1	2026-07-04 01:25:34.514692+00	\N	\N	\N	\N
5438	9	76	7	2026-02-25	252	253	2	2026-07-04 01:25:34.514695+00	\N	\N	\N	\N
5439	9	76	8	2026-03-04	251	253	1	2026-07-04 01:25:34.514699+00	\N	\N	\N	\N
5440	9	76	8	2026-03-04	250	252	2	2026-07-04 01:25:34.514702+00	\N	\N	\N	\N
5451	10	76	6	2026-02-19	255	256	2	2026-07-04 01:25:34.623603+00	\N	\N	\N	\N
5452	10	76	6	2026-02-19	254	257	1	2026-07-04 01:25:34.623607+00	\N	\N	\N	\N
5453	10	76	7	2026-02-26	254	255	1	2026-07-04 01:25:34.62361+00	\N	\N	\N	\N
5454	10	76	7	2026-02-26	256	257	2	2026-07-04 01:25:34.623613+00	\N	\N	\N	\N
5455	10	76	8	2026-03-05	255	257	1	2026-07-04 01:25:34.623617+00	\N	\N	\N	\N
5456	10	76	8	2026-03-05	254	256	2	2026-07-04 01:25:34.62362+00	\N	\N	\N	\N
5467	11	76	6	2026-02-19	259	260	2	2026-07-04 01:25:34.728552+00	\N	\N	\N	\N
5468	11	76	6	2026-02-19	258	261	1	2026-07-04 01:25:34.728556+00	\N	\N	\N	\N
5469	11	76	7	2026-02-26	258	259	1	2026-07-04 01:25:34.728559+00	\N	\N	\N	\N
5470	11	76	7	2026-02-26	260	261	2	2026-07-04 01:25:34.728564+00	\N	\N	\N	\N
5471	11	76	8	2026-03-05	259	261	1	2026-07-04 01:25:34.728568+00	\N	\N	\N	\N
5472	11	76	8	2026-03-05	258	260	2	2026-07-04 01:25:34.728571+00	\N	\N	\N	\N
5483	13	76	6	2026-02-20	263	264	2	2026-07-04 01:25:34.837474+00	\N	\N	\N	\N
5484	13	76	6	2026-02-20	262	265	1	2026-07-04 01:25:34.837477+00	\N	\N	\N	\N
5485	13	76	7	2026-02-27	262	263	1	2026-07-04 01:25:34.837481+00	\N	\N	\N	\N
5486	13	76	7	2026-02-27	264	265	2	2026-07-04 01:25:34.837484+00	\N	\N	\N	\N
5487	13	76	8	2026-03-06	263	265	1	2026-07-04 01:25:34.837488+00	\N	\N	\N	\N
5488	13	76	8	2026-03-06	262	264	2	2026-07-04 01:25:34.837491+00	\N	\N	\N	\N
5499	14	76	6	2026-02-20	267	268	2	2026-07-04 01:25:34.947486+00	\N	\N	\N	\N
5500	14	76	6	2026-02-20	266	269	1	2026-07-04 01:25:34.947489+00	\N	\N	\N	\N
5501	14	76	7	2026-02-27	266	267	1	2026-07-04 01:25:34.947492+00	\N	\N	\N	\N
5502	14	76	7	2026-02-27	268	269	2	2026-07-04 01:25:34.947495+00	\N	\N	\N	\N
5503	14	76	8	2026-03-06	267	269	1	2026-07-04 01:25:34.947498+00	\N	\N	\N	\N
5504	14	76	8	2026-03-06	266	268	2	2026-07-04 01:25:34.947501+00	\N	\N	\N	\N
5515	15	76	6	2026-02-20	271	272	2	2026-07-04 01:25:35.055936+00	\N	\N	\N	\N
5516	15	76	6	2026-02-20	270	273	1	2026-07-04 01:25:35.05594+00	\N	\N	\N	\N
5517	15	76	7	2026-02-27	270	271	1	2026-07-04 01:25:35.055943+00	\N	\N	\N	\N
5518	15	76	7	2026-02-27	272	273	2	2026-07-04 01:25:35.055946+00	\N	\N	\N	\N
5519	15	76	8	2026-03-06	271	273	1	2026-07-04 01:25:35.055949+00	\N	\N	\N	\N
5520	15	76	8	2026-03-06	270	272	2	2026-07-04 01:25:35.055952+00	\N	\N	\N	\N
5329	1	76	1	2026-01-12	226	227	1	2026-07-04 01:25:33.872114+00	12	12	8	4
5361	4	76	1	2026-01-13	234	235	1	2026-07-04 01:25:34.090404+00	12	8	4	12
5410	8	76	1	2026-01-14	248	249	2	2026-07-04 01:25:34.403393+00	6	3	12	12
5441	10	76	1	2026-01-15	254	255	1	2026-07-04 01:25:34.623493+00	12	6	9	12
5442	10	76	1	2026-01-15	256	257	2	2026-07-04 01:25:34.623568+00	11	9	12	12
5457	11	76	1	2026-01-15	258	259	1	2026-07-04 01:25:34.728396+00	12	12	4	7
5458	11	76	1	2026-01-15	260	261	2	2026-07-04 01:25:34.728505+00	6	12	12	4
5473	13	76	1	2026-01-16	262	263	1	2026-07-04 01:25:34.837366+00	12	12	2	4
5474	13	76	1	2026-01-16	264	265	2	2026-07-04 01:25:34.837437+00	12	4	11	12
5489	14	76	1	2026-01-16	266	267	1	2026-07-04 01:25:34.947378+00	12	5	5	12
5490	14	76	1	2026-01-16	268	269	2	2026-07-04 01:25:34.947452+00	5	2	12	12
5505	15	76	1	2026-01-16	270	271	1	2026-07-04 01:25:35.055826+00	12	5	4	12
5459	11	76	2	2026-01-22	258	260	1	2026-07-04 01:25:34.728515+00	6	12	12	9
5506	15	76	1	2026-01-16	272	273	2	2026-07-04 01:25:35.055898+00	12	12	2	10
5363	4	76	2	2026-01-20	234	236	1	2026-07-04 01:25:34.09046+00	12	6	10	12
5412	8	76	2	2026-01-21	247	249	2	2026-07-04 01:25:34.403406+00	12	12	10	5
5443	10	76	2	2026-01-22	254	256	1	2026-07-04 01:25:34.623575+00	10	12	12	5
5444	10	76	2	2026-01-22	255	257	2	2026-07-04 01:25:34.623578+00	12	12	5	6
5460	11	76	2	2026-01-22	259	261	2	2026-07-04 01:25:34.72852+00	4	10	12	12
5475	13	76	2	2026-01-23	262	264	1	2026-07-04 01:25:34.837444+00	12	12	4	5
5476	13	76	2	2026-01-23	263	265	2	2026-07-04 01:25:34.83745+00	8	12	12	6
5491	14	76	2	2026-01-23	266	268	1	2026-07-04 01:25:34.947458+00	12	12	5	3
5492	14	76	2	2026-01-23	267	269	2	2026-07-04 01:25:34.947463+00	7	12	12	7
5508	15	76	2	2026-01-23	271	273	2	2026-07-04 01:25:35.05591+00	12	4	9	12
5507	15	76	2	2026-01-23	270	272	1	2026-07-04 01:25:35.055905+00	8	12	12	10
5350	2	76	3	2026-01-26	230	233	2	2026-07-04 01:25:33.980461+00	7	12	12	11
5414	8	76	3	2026-01-28	246	249	2	2026-07-04 01:25:34.403413+00	12	6	11	12
5445	10	76	3	2026-01-29	255	256	1	2026-07-04 01:25:34.623584+00	12	7	3	12
5446	10	76	3	2026-01-29	254	257	2	2026-07-04 01:25:34.623587+00	11	12	12	5
5461	11	76	3	2026-01-29	259	260	1	2026-07-04 01:25:34.728525+00	12	4	3	12
5462	11	76	3	2026-01-29	258	261	2	2026-07-04 01:25:34.728529+00	10	12	12	8
5477	13	76	3	2026-01-30	263	264	1	2026-07-04 01:25:34.837454+00	6	8	12	12
5478	13	76	3	2026-01-30	262	265	2	2026-07-04 01:25:34.837457+00	11	6	12	12
5493	14	76	3	2026-01-30	267	268	1	2026-07-04 01:25:34.947467+00	12	12	0	6
5494	14	76	3	2026-01-30	266	269	2	2026-07-04 01:25:34.94747+00	10	12	12	9
5509	15	76	3	2026-01-30	271	272	1	2026-07-04 01:25:35.055914+00	12	12	9	5
5510	15	76	3	2026-01-30	270	273	2	2026-07-04 01:25:35.055919+00	12	10	8	12
5368	4	76	4	2026-02-03	236	237	1	2026-07-04 01:25:34.090478+00	12	7	9	12
5415	8	76	4	2026-02-04	246	247	2	2026-07-04 01:25:34.403418+00	3	7	12	12
5448	10	76	4	2026-02-05	256	257	1	2026-07-04 01:25:34.623595+00	4	4	12	12
5447	10	76	4	2026-02-05	254	255	2	2026-07-04 01:25:34.623591+00	12	12	7	8
5464	11	76	4	2026-02-05	260	261	1	2026-07-04 01:25:34.728538+00	5	12	12	9
5463	11	76	4	2026-02-05	258	259	2	2026-07-04 01:25:34.728534+00	7	7	12	12
5480	13	76	4	2026-02-06	264	265	1	2026-07-04 01:25:34.837464+00	5	0	12	12
5479	13	76	4	2026-02-06	262	263	2	2026-07-04 01:25:34.83746+00	12	12	1	6
5495	14	76	4	2026-02-06	266	267	2	2026-07-04 01:25:34.947474+00	12	12	6	5
5496	14	76	4	2026-02-06	268	269	1	2026-07-04 01:25:34.947476+00	8	12	12	10
5512	15	76	4	2026-02-06	272	273	1	2026-07-04 01:25:35.055926+00	8	6	12	12
5511	15	76	4	2026-02-06	270	271	2	2026-07-04 01:25:35.055922+00	10	11	12	12
5370	4	76	5	2026-02-10	235	237	1	2026-07-04 01:25:34.090485+00	11	0	12	12
5417	8	76	5	2026-02-11	246	248	2	2026-07-04 01:25:34.403424+00	4	12	12	3
5450	10	76	5	2026-02-12	255	257	1	2026-07-04 01:25:34.623601+00	5	4	12	12
5466	11	76	5	2026-02-12	259	261	1	2026-07-04 01:25:34.728548+00	12	5	9	12
5449	10	76	5	2026-02-12	254	256	2	2026-07-04 01:25:34.623598+00	12	12	3	11
5465	11	76	5	2026-02-12	258	260	2	2026-07-04 01:25:34.728543+00	7	12	12	9
5482	13	76	5	2026-02-13	263	265	1	2026-07-04 01:25:34.83747+00	12	6	11	12
5481	13	76	5	2026-02-13	262	264	2	2026-07-04 01:25:34.837467+00	12	12	11	7
5497	14	76	5	2026-02-13	266	268	2	2026-07-04 01:25:34.94748+00	12	12	0	3
5498	14	76	5	2026-02-13	267	269	1	2026-07-04 01:25:34.947482+00	10	12	12	11
5513	15	76	5	2026-02-13	270	272	2	2026-07-04 01:25:35.05593+00	10	12	12	4
5514	15	76	5	2026-02-13	271	273	1	2026-07-04 01:25:35.055933+00	6	12	12	10
5521	155	78	1	2027-01-11	630	631	5	2026-07-04 18:05:14.92533+00	\N	\N	\N	\N
5522	155	78	1	2027-01-11	632	633	4	2026-07-04 18:05:14.934021+00	\N	\N	\N	\N
5523	155	78	1	2027-01-11	692	693	2	2026-07-04 18:05:14.934057+00	\N	\N	\N	\N
5524	155	78	2	2027-01-18	630	633	2	2026-07-04 18:05:14.93408+00	\N	\N	\N	\N
5525	155	78	2	2027-01-18	631	693	4	2026-07-04 18:05:14.93409+00	\N	\N	\N	\N
5526	155	78	2	2027-01-18	632	692	5	2026-07-04 18:05:14.934097+00	\N	\N	\N	\N
5527	155	78	3	2027-01-25	630	693	5	2026-07-04 18:05:14.934108+00	\N	\N	\N	\N
5528	155	78	3	2027-01-25	633	692	4	2026-07-04 18:05:14.934113+00	\N	\N	\N	\N
5529	155	78	3	2027-01-25	631	632	2	2026-07-04 18:05:14.934119+00	\N	\N	\N	\N
5530	155	78	4	2027-02-01	630	692	4	2026-07-04 18:05:14.934124+00	\N	\N	\N	\N
5531	155	78	4	2027-02-01	693	632	5	2026-07-04 18:05:14.93413+00	\N	\N	\N	\N
5532	155	78	4	2027-02-01	633	631	2	2026-07-04 18:05:14.934134+00	\N	\N	\N	\N
5533	155	78	5	2027-02-08	630	632	4	2026-07-04 18:05:14.934139+00	\N	\N	\N	\N
5534	155	78	5	2027-02-08	692	631	5	2026-07-04 18:05:14.934144+00	\N	\N	\N	\N
5535	155	78	5	2027-02-08	693	633	2	2026-07-04 18:05:14.934149+00	\N	\N	\N	\N
5536	155	78	6	2027-02-15	630	631	4	2026-07-04 18:05:14.934155+00	\N	\N	\N	\N
5537	155	78	6	2027-02-15	632	633	5	2026-07-04 18:05:14.93416+00	\N	\N	\N	\N
5538	155	78	6	2027-02-15	692	693	2	2026-07-04 18:05:14.934165+00	\N	\N	\N	\N
5539	155	78	7	2027-02-22	630	633	5	2026-07-04 18:05:14.934171+00	\N	\N	\N	\N
5540	155	78	7	2027-02-22	631	693	4	2026-07-04 18:05:14.934175+00	\N	\N	\N	\N
5541	155	78	7	2027-02-22	632	692	2	2026-07-04 18:05:14.934181+00	\N	\N	\N	\N
5542	155	78	8	2027-03-01	630	693	2	2026-07-04 18:05:14.934185+00	\N	\N	\N	\N
5543	155	78	8	2027-03-01	633	692	5	2026-07-04 18:05:14.934189+00	\N	\N	\N	\N
5544	155	78	8	2027-03-01	631	632	4	2026-07-04 18:05:14.934194+00	\N	\N	\N	\N
5545	157	77	1	2027-01-11	634	635	5	2026-07-04 18:05:15.045825+00	\N	\N	\N	\N
5546	157	77	1	2027-01-11	636	637	4	2026-07-04 18:05:15.045904+00	\N	\N	\N	\N
5547	157	77	2	2027-01-18	634	636	5	2026-07-04 18:05:15.045916+00	\N	\N	\N	\N
5548	157	77	2	2027-01-18	635	637	4	2026-07-04 18:05:15.045922+00	\N	\N	\N	\N
5549	157	77	3	2027-01-25	634	637	5	2026-07-04 18:05:15.045928+00	\N	\N	\N	\N
5550	157	77	3	2027-01-25	635	636	4	2026-07-04 18:05:15.045934+00	\N	\N	\N	\N
5551	157	77	4	2027-02-01	634	635	4	2026-07-04 18:05:15.045977+00	\N	\N	\N	\N
5552	157	77	4	2027-02-01	636	637	5	2026-07-04 18:05:15.045986+00	\N	\N	\N	\N
5553	157	77	5	2027-02-08	634	637	4	2026-07-04 18:05:15.045992+00	\N	\N	\N	\N
5554	157	77	5	2027-02-08	635	636	5	2026-07-04 18:05:15.045998+00	\N	\N	\N	\N
5555	157	77	6	2027-02-15	634	636	4	2026-07-04 18:05:15.046004+00	\N	\N	\N	\N
5556	157	77	6	2027-02-15	637	635	5	2026-07-04 18:05:15.046009+00	\N	\N	\N	\N
5557	157	77	7	2027-02-22	634	635	5	2026-07-04 18:05:15.046014+00	\N	\N	\N	\N
5558	157	77	7	2027-02-22	636	637	4	2026-07-04 18:05:15.04602+00	\N	\N	\N	\N
5559	157	77	8	2027-03-01	634	637	5	2026-07-04 18:05:15.046027+00	\N	\N	\N	\N
5560	157	77	8	2027-03-01	635	636	4	2026-07-04 18:05:15.046032+00	\N	\N	\N	\N
5561	152	77	1	2027-01-12	638	639	5	2026-07-04 18:05:15.162374+00	\N	\N	\N	\N
5562	152	77	1	2027-01-12	640	641	4	2026-07-04 18:05:15.162453+00	\N	\N	\N	\N
5563	152	77	2	2027-01-19	638	640	5	2026-07-04 18:05:15.162464+00	\N	\N	\N	\N
5564	152	77	2	2027-01-19	639	641	4	2026-07-04 18:05:15.162472+00	\N	\N	\N	\N
5565	152	77	3	2027-01-26	638	641	5	2026-07-04 18:05:15.162479+00	\N	\N	\N	\N
5566	152	77	3	2027-01-26	639	640	4	2026-07-04 18:05:15.162485+00	\N	\N	\N	\N
5567	152	77	4	2027-02-02	638	639	4	2026-07-04 18:05:15.16249+00	\N	\N	\N	\N
5568	152	77	4	2027-02-02	640	641	5	2026-07-04 18:05:15.162495+00	\N	\N	\N	\N
5569	152	77	5	2027-02-09	638	641	4	2026-07-04 18:05:15.162503+00	\N	\N	\N	\N
5570	152	77	5	2027-02-09	639	640	5	2026-07-04 18:05:15.162508+00	\N	\N	\N	\N
5571	152	77	6	2027-02-16	638	640	4	2026-07-04 18:05:15.162514+00	\N	\N	\N	\N
5572	152	77	6	2027-02-16	641	639	5	2026-07-04 18:05:15.162519+00	\N	\N	\N	\N
5573	152	77	7	2027-02-23	638	639	5	2026-07-04 18:05:15.162524+00	\N	\N	\N	\N
5574	152	77	7	2027-02-23	640	641	4	2026-07-04 18:05:15.162529+00	\N	\N	\N	\N
5575	152	77	8	2027-03-02	638	641	5	2026-07-04 18:05:15.162534+00	\N	\N	\N	\N
5576	152	77	8	2027-03-02	639	640	4	2026-07-04 18:05:15.162539+00	\N	\N	\N	\N
5577	160	77	1	2027-01-12	642	643	5	2026-07-04 18:05:15.300729+00	\N	\N	\N	\N
5578	160	77	1	2027-01-12	644	645	4	2026-07-04 18:05:15.300861+00	\N	\N	\N	\N
5579	160	77	2	2027-01-19	642	644	5	2026-07-04 18:05:15.300874+00	\N	\N	\N	\N
5580	160	77	2	2027-01-19	643	645	4	2026-07-04 18:05:15.300881+00	\N	\N	\N	\N
5581	160	77	3	2027-01-26	642	645	5	2026-07-04 18:05:15.300891+00	\N	\N	\N	\N
5582	160	77	3	2027-01-26	643	644	4	2026-07-04 18:05:15.300899+00	\N	\N	\N	\N
5583	160	77	4	2027-02-02	642	643	4	2026-07-04 18:05:15.300911+00	\N	\N	\N	\N
5584	160	77	4	2027-02-02	644	645	5	2026-07-04 18:05:15.300918+00	\N	\N	\N	\N
5585	160	77	5	2027-02-09	642	645	4	2026-07-04 18:05:15.300929+00	\N	\N	\N	\N
5586	160	77	5	2027-02-09	643	644	5	2026-07-04 18:05:15.300937+00	\N	\N	\N	\N
5587	160	77	6	2027-02-16	642	644	4	2026-07-04 18:05:15.300945+00	\N	\N	\N	\N
5588	160	77	6	2027-02-16	645	643	5	2026-07-04 18:05:15.300952+00	\N	\N	\N	\N
5589	160	77	7	2027-02-23	642	643	5	2026-07-04 18:05:15.30096+00	\N	\N	\N	\N
5590	160	77	7	2027-02-23	644	645	4	2026-07-04 18:05:15.300967+00	\N	\N	\N	\N
5591	160	77	8	2027-03-02	642	645	5	2026-07-04 18:05:15.300976+00	\N	\N	\N	\N
5592	160	77	8	2027-03-02	643	644	4	2026-07-04 18:05:15.300985+00	\N	\N	\N	\N
5593	156	77	1	2027-01-13	646	647	5	2026-07-04 18:05:15.413546+00	\N	\N	\N	\N
5594	156	77	1	2027-01-13	648	649	4	2026-07-04 18:05:15.413701+00	\N	\N	\N	\N
5595	156	77	2	2027-01-20	646	648	5	2026-07-04 18:05:15.413711+00	\N	\N	\N	\N
5596	156	77	2	2027-01-20	647	649	4	2026-07-04 18:05:15.413717+00	\N	\N	\N	\N
5597	156	77	3	2027-01-27	646	649	5	2026-07-04 18:05:15.413723+00	\N	\N	\N	\N
5598	156	77	3	2027-01-27	647	648	4	2026-07-04 18:05:15.413727+00	\N	\N	\N	\N
5599	156	77	4	2027-02-03	646	647	4	2026-07-04 18:05:15.413732+00	\N	\N	\N	\N
5600	156	77	4	2027-02-03	648	649	5	2026-07-04 18:05:15.413737+00	\N	\N	\N	\N
5601	156	77	5	2027-02-10	646	649	4	2026-07-04 18:05:15.413742+00	\N	\N	\N	\N
5602	156	77	5	2027-02-10	647	648	5	2026-07-04 18:05:15.413746+00	\N	\N	\N	\N
5603	156	77	6	2027-02-17	646	648	4	2026-07-04 18:05:15.41375+00	\N	\N	\N	\N
5604	156	77	6	2027-02-17	649	647	5	2026-07-04 18:05:15.413755+00	\N	\N	\N	\N
5605	156	77	7	2027-02-24	646	647	5	2026-07-04 18:05:15.413759+00	\N	\N	\N	\N
5606	156	77	7	2027-02-24	648	649	4	2026-07-04 18:05:15.413763+00	\N	\N	\N	\N
5607	156	77	8	2027-03-03	646	649	5	2026-07-04 18:05:15.413806+00	\N	\N	\N	\N
5608	156	77	8	2027-03-03	647	648	4	2026-07-04 18:05:15.413818+00	\N	\N	\N	\N
5609	159	77	1	2027-01-13	650	651	5	2026-07-04 18:05:15.522449+00	\N	\N	\N	\N
5610	159	77	1	2027-01-13	652	653	4	2026-07-04 18:05:15.522541+00	\N	\N	\N	\N
5611	159	77	2	2027-01-20	650	652	5	2026-07-04 18:05:15.522549+00	\N	\N	\N	\N
5612	159	77	2	2027-01-20	651	653	4	2026-07-04 18:05:15.522553+00	\N	\N	\N	\N
5613	159	77	3	2027-01-27	650	653	5	2026-07-04 18:05:15.522557+00	\N	\N	\N	\N
5614	159	77	3	2027-01-27	651	652	4	2026-07-04 18:05:15.52256+00	\N	\N	\N	\N
5615	159	77	4	2027-02-03	650	651	4	2026-07-04 18:05:15.522563+00	\N	\N	\N	\N
5616	159	77	4	2027-02-03	652	653	5	2026-07-04 18:05:15.522567+00	\N	\N	\N	\N
5617	159	77	5	2027-02-10	650	653	4	2026-07-04 18:05:15.52257+00	\N	\N	\N	\N
5618	159	77	5	2027-02-10	651	652	5	2026-07-04 18:05:15.522573+00	\N	\N	\N	\N
5619	159	77	6	2027-02-17	650	652	4	2026-07-04 18:05:15.522576+00	\N	\N	\N	\N
5620	159	77	6	2027-02-17	653	651	5	2026-07-04 18:05:15.52258+00	\N	\N	\N	\N
5621	159	77	7	2027-02-24	650	651	5	2026-07-04 18:05:15.52259+00	\N	\N	\N	\N
5622	159	77	7	2027-02-24	652	653	4	2026-07-04 18:05:15.522593+00	\N	\N	\N	\N
5623	159	77	8	2027-03-03	650	653	5	2026-07-04 18:05:15.522604+00	\N	\N	\N	\N
5624	159	77	8	2027-03-03	651	652	4	2026-07-04 18:05:15.522608+00	\N	\N	\N	\N
5625	158	77	1	2027-01-13	654	655	5	2026-07-04 18:05:15.637301+00	\N	\N	\N	\N
5626	158	77	1	2027-01-13	656	657	4	2026-07-04 18:05:15.637369+00	\N	\N	\N	\N
5627	158	77	2	2027-01-20	654	656	5	2026-07-04 18:05:15.637379+00	\N	\N	\N	\N
5628	158	77	2	2027-01-20	655	657	4	2026-07-04 18:05:15.637383+00	\N	\N	\N	\N
5629	158	77	3	2027-01-27	654	657	5	2026-07-04 18:05:15.637388+00	\N	\N	\N	\N
5630	158	77	3	2027-01-27	655	656	4	2026-07-04 18:05:15.637393+00	\N	\N	\N	\N
5631	158	77	4	2027-02-03	654	655	4	2026-07-04 18:05:15.637398+00	\N	\N	\N	\N
5632	158	77	4	2027-02-03	656	657	5	2026-07-04 18:05:15.63743+00	\N	\N	\N	\N
5633	158	77	5	2027-02-10	654	657	4	2026-07-04 18:05:15.637435+00	\N	\N	\N	\N
5634	158	77	5	2027-02-10	655	656	5	2026-07-04 18:05:15.63744+00	\N	\N	\N	\N
5635	158	77	6	2027-02-17	654	656	4	2026-07-04 18:05:15.637444+00	\N	\N	\N	\N
5636	158	77	6	2027-02-17	657	655	5	2026-07-04 18:05:15.637449+00	\N	\N	\N	\N
5637	158	77	7	2027-02-24	654	655	5	2026-07-04 18:05:15.637453+00	\N	\N	\N	\N
5638	158	77	7	2027-02-24	656	657	4	2026-07-04 18:05:15.637457+00	\N	\N	\N	\N
5639	158	77	8	2027-03-03	654	657	5	2026-07-04 18:05:15.637461+00	\N	\N	\N	\N
5640	158	77	8	2027-03-03	655	656	4	2026-07-04 18:05:15.637466+00	\N	\N	\N	\N
5641	154	77	1	2027-01-14	658	659	5	2026-07-04 18:05:15.74813+00	\N	\N	\N	\N
5642	154	77	1	2027-01-14	660	661	4	2026-07-04 18:05:15.748209+00	\N	\N	\N	\N
5643	154	77	2	2027-01-21	658	660	5	2026-07-04 18:05:15.748215+00	\N	\N	\N	\N
5644	154	77	2	2027-01-21	659	661	4	2026-07-04 18:05:15.748219+00	\N	\N	\N	\N
5645	154	77	3	2027-01-28	658	661	5	2026-07-04 18:05:15.748223+00	\N	\N	\N	\N
5646	154	77	3	2027-01-28	659	660	4	2026-07-04 18:05:15.748226+00	\N	\N	\N	\N
5647	154	77	4	2027-02-04	658	659	4	2026-07-04 18:05:15.748229+00	\N	\N	\N	\N
5648	154	77	4	2027-02-04	660	661	5	2026-07-04 18:05:15.748232+00	\N	\N	\N	\N
5649	154	77	5	2027-02-11	658	661	4	2026-07-04 18:05:15.748236+00	\N	\N	\N	\N
5650	154	77	5	2027-02-11	659	660	5	2026-07-04 18:05:15.748239+00	\N	\N	\N	\N
5651	154	77	6	2027-02-18	658	660	4	2026-07-04 18:05:15.748242+00	\N	\N	\N	\N
5652	154	77	6	2027-02-18	661	659	5	2026-07-04 18:05:15.748245+00	\N	\N	\N	\N
5653	154	77	7	2027-02-25	658	659	5	2026-07-04 18:05:15.748248+00	\N	\N	\N	\N
5654	154	77	7	2027-02-25	660	661	4	2026-07-04 18:05:15.74825+00	\N	\N	\N	\N
5655	154	77	8	2027-03-04	658	661	5	2026-07-04 18:05:15.748253+00	\N	\N	\N	\N
5656	154	77	8	2027-03-04	659	660	4	2026-07-04 18:05:15.748256+00	\N	\N	\N	\N
5657	161	77	1	2027-01-14	662	663	5	2026-07-04 18:05:15.863557+00	\N	\N	\N	\N
5658	161	77	1	2027-01-14	664	665	4	2026-07-04 18:05:15.863657+00	\N	\N	\N	\N
5659	161	77	2	2027-01-21	662	664	5	2026-07-04 18:05:15.863667+00	\N	\N	\N	\N
5660	161	77	2	2027-01-21	663	665	4	2026-07-04 18:05:15.863673+00	\N	\N	\N	\N
5661	161	77	3	2027-01-28	662	665	5	2026-07-04 18:05:15.863681+00	\N	\N	\N	\N
5662	161	77	3	2027-01-28	663	664	4	2026-07-04 18:05:15.863688+00	\N	\N	\N	\N
5663	161	77	4	2027-02-04	662	663	4	2026-07-04 18:05:15.863694+00	\N	\N	\N	\N
5664	161	77	4	2027-02-04	664	665	5	2026-07-04 18:05:15.863701+00	\N	\N	\N	\N
5665	161	77	5	2027-02-11	662	665	4	2026-07-04 18:05:15.863708+00	\N	\N	\N	\N
5666	161	77	5	2027-02-11	663	664	5	2026-07-04 18:05:15.863715+00	\N	\N	\N	\N
5667	161	77	6	2027-02-18	662	664	4	2026-07-04 18:05:15.863721+00	\N	\N	\N	\N
5668	161	77	6	2027-02-18	665	663	5	2026-07-04 18:05:15.863727+00	\N	\N	\N	\N
5669	161	77	7	2027-02-25	662	663	5	2026-07-04 18:05:15.863735+00	\N	\N	\N	\N
5670	161	77	7	2027-02-25	664	665	4	2026-07-04 18:05:15.863742+00	\N	\N	\N	\N
5671	161	77	8	2027-03-04	662	665	5	2026-07-04 18:05:15.863749+00	\N	\N	\N	\N
5672	161	77	8	2027-03-04	663	664	4	2026-07-04 18:05:15.863755+00	\N	\N	\N	\N
5673	153	78	1	2027-01-15	666	667	5	2026-07-04 18:05:15.973479+00	\N	\N	\N	\N
5674	153	78	1	2027-01-15	668	669	4	2026-07-04 18:05:15.973564+00	\N	\N	\N	\N
5675	153	78	1	2027-01-15	694	695	2	2026-07-04 18:05:15.97357+00	\N	\N	\N	\N
5676	153	78	2	2027-01-22	666	669	2	2026-07-04 18:05:15.973575+00	\N	\N	\N	\N
5677	153	78	2	2027-01-22	667	695	4	2026-07-04 18:05:15.973581+00	\N	\N	\N	\N
5678	153	78	2	2027-01-22	668	694	5	2026-07-04 18:05:15.973585+00	\N	\N	\N	\N
5679	153	78	3	2027-01-29	666	695	5	2026-07-04 18:05:15.973589+00	\N	\N	\N	\N
5680	153	78	3	2027-01-29	669	694	4	2026-07-04 18:05:15.973593+00	\N	\N	\N	\N
5681	153	78	3	2027-01-29	667	668	2	2026-07-04 18:05:15.973598+00	\N	\N	\N	\N
5682	153	78	4	2027-02-05	666	694	4	2026-07-04 18:05:15.973601+00	\N	\N	\N	\N
5683	153	78	4	2027-02-05	695	668	5	2026-07-04 18:05:15.973605+00	\N	\N	\N	\N
5684	153	78	4	2027-02-05	669	667	2	2026-07-04 18:05:15.97361+00	\N	\N	\N	\N
5685	153	78	5	2027-02-12	666	668	4	2026-07-04 18:05:15.973614+00	\N	\N	\N	\N
5686	153	78	5	2027-02-12	694	667	5	2026-07-04 18:05:15.973617+00	\N	\N	\N	\N
5687	153	78	5	2027-02-12	695	669	2	2026-07-04 18:05:15.973622+00	\N	\N	\N	\N
5688	153	78	6	2027-02-19	666	667	4	2026-07-04 18:05:15.973625+00	\N	\N	\N	\N
5689	153	78	6	2027-02-19	668	669	5	2026-07-04 18:05:15.973629+00	\N	\N	\N	\N
5690	153	78	6	2027-02-19	694	695	2	2026-07-04 18:05:15.973633+00	\N	\N	\N	\N
5691	153	78	7	2027-02-26	666	669	5	2026-07-04 18:05:15.973638+00	\N	\N	\N	\N
5692	153	78	7	2027-02-26	667	695	4	2026-07-04 18:05:15.973641+00	\N	\N	\N	\N
5693	153	78	7	2027-02-26	668	694	2	2026-07-04 18:05:15.973645+00	\N	\N	\N	\N
5694	153	78	8	2027-03-05	666	695	2	2026-07-04 18:05:15.973649+00	\N	\N	\N	\N
5695	153	78	8	2027-03-05	669	694	5	2026-07-04 18:05:15.973653+00	\N	\N	\N	\N
5696	153	78	8	2027-03-05	667	668	4	2026-07-04 18:05:15.973657+00	\N	\N	\N	\N
5697	163	77	1	2027-01-15	670	671	5	2026-07-04 18:05:16.092595+00	\N	\N	\N	\N
5698	163	77	1	2027-01-15	672	673	4	2026-07-04 18:05:16.092655+00	\N	\N	\N	\N
5699	163	77	2	2027-01-22	670	672	5	2026-07-04 18:05:16.092659+00	\N	\N	\N	\N
5700	163	77	2	2027-01-22	671	673	4	2026-07-04 18:05:16.092662+00	\N	\N	\N	\N
5701	163	77	3	2027-01-29	670	673	5	2026-07-04 18:05:16.092666+00	\N	\N	\N	\N
5702	163	77	3	2027-01-29	671	672	4	2026-07-04 18:05:16.092669+00	\N	\N	\N	\N
5703	163	77	4	2027-02-05	670	671	4	2026-07-04 18:05:16.092671+00	\N	\N	\N	\N
5704	163	77	4	2027-02-05	672	673	5	2026-07-04 18:05:16.092674+00	\N	\N	\N	\N
5705	163	77	5	2027-02-12	670	673	4	2026-07-04 18:05:16.092677+00	\N	\N	\N	\N
5706	163	77	5	2027-02-12	671	672	5	2026-07-04 18:05:16.09268+00	\N	\N	\N	\N
5707	163	77	6	2027-02-19	670	672	4	2026-07-04 18:05:16.092682+00	\N	\N	\N	\N
5708	163	77	6	2027-02-19	673	671	5	2026-07-04 18:05:16.092685+00	\N	\N	\N	\N
5709	163	77	7	2027-02-26	670	671	5	2026-07-04 18:05:16.092687+00	\N	\N	\N	\N
5710	163	77	7	2027-02-26	672	673	4	2026-07-04 18:05:16.09269+00	\N	\N	\N	\N
5711	163	77	8	2027-03-05	670	673	5	2026-07-04 18:05:16.092692+00	\N	\N	\N	\N
5712	163	77	8	2027-03-05	671	672	4	2026-07-04 18:05:16.092695+00	\N	\N	\N	\N
5713	162	77	1	2027-01-15	674	675	5	2026-07-04 18:05:16.208523+00	\N	\N	\N	\N
5714	162	77	1	2027-01-15	676	677	4	2026-07-04 18:05:16.208587+00	\N	\N	\N	\N
5715	162	77	2	2027-01-22	674	676	5	2026-07-04 18:05:16.208595+00	\N	\N	\N	\N
5716	162	77	2	2027-01-22	675	677	4	2026-07-04 18:05:16.208598+00	\N	\N	\N	\N
5717	162	77	3	2027-01-29	674	677	5	2026-07-04 18:05:16.208602+00	\N	\N	\N	\N
5718	162	77	3	2027-01-29	675	676	4	2026-07-04 18:05:16.208605+00	\N	\N	\N	\N
5719	162	77	4	2027-02-05	674	675	4	2026-07-04 18:05:16.208612+00	\N	\N	\N	\N
5720	162	77	4	2027-02-05	676	677	5	2026-07-04 18:05:16.208615+00	\N	\N	\N	\N
5721	162	77	5	2027-02-12	674	677	4	2026-07-04 18:05:16.208618+00	\N	\N	\N	\N
5722	162	77	5	2027-02-12	675	676	5	2026-07-04 18:05:16.20862+00	\N	\N	\N	\N
5723	162	77	6	2027-02-19	674	676	4	2026-07-04 18:05:16.208622+00	\N	\N	\N	\N
5724	162	77	6	2027-02-19	677	675	5	2026-07-04 18:05:16.208626+00	\N	\N	\N	\N
5725	162	77	7	2027-02-26	674	675	5	2026-07-04 18:05:16.208628+00	\N	\N	\N	\N
5726	162	77	7	2027-02-26	676	677	4	2026-07-04 18:05:16.208631+00	\N	\N	\N	\N
5727	162	77	8	2027-03-05	674	677	5	2026-07-04 18:05:16.208633+00	\N	\N	\N	\N
5728	162	77	8	2027-03-05	675	676	4	2026-07-04 18:05:16.222742+00	\N	\N	\N	\N
\.


--
-- Data for Name: ScheduleTemplateMatches; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ScheduleTemplateMatches" ("Id", "TemplateWeekId", "Slot1", "Slot2", "CourtId") FROM stdin;
1581	595	A	B	1
1582	595	C	D	2
1583	596	A	C	1
1584	596	B	D	2
1593	601	A	B	1
1594	601	C	D	2
1585	597	B	C	1
1586	597	A	D	2
1587	598	A	B	2
1588	598	C	D	1
1589	599	A	C	2
1590	599	B	D	1
1591	600	B	C	2
1592	600	A	D	1
1595	602	B	D	1
1596	602	A	C	2
1597	603	A	B	5
1598	603	C	D	4
1599	604	A	C	5
1600	604	B	D	4
1601	605	A	D	5
1602	605	B	C	4
1603	606	A	B	4
1604	606	C	D	5
1605	607	A	D	4
1606	607	B	C	5
1607	608	A	C	4
1608	608	D	B	5
1609	609	A	B	5
1610	609	C	D	4
1611	610	A	D	5
1612	610	B	C	4
1613	611	A	B	5
1614	611	C	D	4
1615	611	E	F	2
1616	612	A	D	2
1617	612	B	F	4
1618	612	C	E	5
1619	613	A	F	5
1620	613	D	E	4
1621	613	B	C	2
1622	614	A	E	4
1623	614	F	C	5
1624	614	D	B	2
1625	615	A	C	4
1626	615	E	B	5
1627	615	F	D	2
1628	616	A	B	4
1629	616	C	D	5
1630	616	E	F	2
1631	617	A	D	5
1632	617	B	F	4
1633	617	C	E	2
1634	618	A	F	2
1635	618	D	E	5
1636	618	B	C	4
\.


--
-- Data for Name: ScheduleTemplateWeeks; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ScheduleTemplateWeeks" ("Id", "TemplateId", "WeekNumber") FROM stdin;
595	76	1
596	76	2
597	76	3
598	76	4
599	76	5
600	76	6
601	76	7
602	76	8
603	77	1
604	77	2
605	77	3
606	77	4
607	77	5
608	77	6
609	77	7
610	77	8
611	78	1
612	78	2
613	78	3
614	78	4
615	78	5
616	78	6
617	78	7
618	78	8
\.


--
-- Data for Name: ScheduleTemplates; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ScheduleTemplates" ("Id", "SeasonId", "TeamCount", "WeekCount", "GeneratedAt", "IsLocked") FROM stdin;
76	1	4	8	2026-07-02 23:05:29.848669+00	t
77	13	4	8	2026-07-04 18:04:54.272221+00	f
78	13	6	8	2026-07-04 18:04:54.565316+00	f
\.


--
-- Data for Name: ScheduleWeeks; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ScheduleWeeks" ("Id", "DivisionId", "WeekNumber", "MatchDate") FROM stdin;
\.


--
-- Data for Name: SeasonCourts; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SeasonCourts" ("Id", "SeasonId", "CourtId", "SortOrder") FROM stdin;
172	1	1	0
173	1	2	1
178	13	5	0
179	13	4	1
180	13	2	2
181	13	1	3
\.


--
-- Data for Name: SeasonDaySlots; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SeasonDaySlots" ("Id", "SeasonId", "DaySlotId") FROM stdin;
749	13	1
750	13	2
751	13	3
752	13	4
753	13	5
739	1	1
740	1	2
741	1	3
742	1	4
743	1	5
\.


--
-- Data for Name: SeasonFees; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SeasonFees" ("Id", "PlayerId", "SeasonId", "AmountOwing", "AmountPaid", "PaidDate", "Notes", "CreatedAt") FROM stdin;
527	34	13	10.00	0	\N	\N	2026-06-24 20:37:43.902977+00
528	35	13	10.00	0	\N	\N	2026-06-24 20:37:43.93126+00
529	36	13	10.00	0	\N	\N	2026-06-24 20:37:43.934391+00
530	40	13	10.00	0	\N	\N	2026-06-24 20:37:43.93671+00
531	41	13	10.00	0	\N	\N	2026-06-24 20:37:43.938978+00
532	42	13	10.00	0	\N	\N	2026-06-24 20:37:43.941523+00
533	49	13	10.00	0	\N	\N	2026-06-24 20:37:43.94362+00
534	51	13	10.00	0	\N	\N	2026-06-24 20:37:43.945943+00
535	54	13	10.00	0	\N	\N	2026-06-24 20:37:43.948005+00
536	55	13	10.00	0	\N	\N	2026-06-24 20:37:43.949987+00
537	56	13	10.00	0	\N	\N	2026-06-24 20:37:43.952572+00
538	57	13	10.00	0	\N	\N	2026-06-24 20:37:43.954756+00
539	60	13	10.00	0	\N	\N	2026-06-24 20:37:43.956862+00
540	61	13	10.00	0	\N	\N	2026-06-24 20:37:43.959273+00
541	64	13	10.00	0	\N	\N	2026-06-24 20:37:43.96147+00
542	65	13	10.00	0	\N	\N	2026-06-24 20:37:43.963543+00
543	76	13	10.00	0	\N	\N	2026-06-24 20:37:43.965904+00
544	81	13	10.00	0	\N	\N	2026-06-24 20:37:43.968066+00
545	82	13	10.00	0	\N	\N	2026-06-24 20:37:43.970242+00
546	83	13	10.00	0	\N	\N	2026-06-24 20:37:43.972471+00
547	93	13	10.00	0	\N	\N	2026-06-24 20:37:43.974536+00
548	94	13	10.00	0	\N	\N	2026-06-24 20:37:43.976554+00
549	100	13	10.00	0	\N	\N	2026-06-24 20:37:43.978924+00
550	101	13	10.00	0	\N	\N	2026-06-24 20:37:43.98105+00
551	105	13	10.00	0	\N	\N	2026-06-24 20:37:43.983132+00
552	106	13	10.00	0	\N	\N	2026-06-24 20:37:43.985149+00
553	114	13	10.00	0	\N	\N	2026-06-24 20:37:43.998316+00
554	120	13	10.00	0	\N	\N	2026-06-24 20:37:44.000913+00
555	121	13	10.00	0	\N	\N	2026-06-24 20:37:44.003347+00
556	123	13	10.00	0	\N	\N	2026-06-24 20:37:44.005448+00
557	124	13	10.00	0	\N	\N	2026-06-24 20:37:44.007746+00
558	130	13	10.00	0	\N	\N	2026-06-24 20:37:44.009931+00
559	133	13	10.00	0	\N	\N	2026-06-24 20:37:44.012118+00
560	136	13	10.00	0	\N	\N	2026-06-24 20:37:44.01458+00
561	148	13	10.00	0	\N	\N	2026-06-24 20:37:44.016857+00
562	149	13	10.00	0	\N	\N	2026-06-24 20:37:44.018998+00
563	159	13	10.00	0	\N	\N	2026-06-24 20:37:44.021323+00
564	164	13	10.00	0	\N	\N	2026-06-24 20:37:44.023437+00
565	166	13	10.00	0	\N	\N	2026-06-24 20:37:44.025493+00
566	171	13	10.00	0	\N	\N	2026-06-24 20:37:44.027787+00
567	177	13	10.00	0	\N	\N	2026-06-24 20:37:44.029999+00
568	185	13	10.00	0	\N	\N	2026-06-24 20:37:44.032181+00
569	195	13	10.00	0	\N	\N	2026-06-24 20:37:44.034549+00
570	202	13	10.00	0	\N	\N	2026-06-24 20:37:44.036634+00
571	203	13	10.00	0	\N	\N	2026-06-24 20:37:44.038562+00
572	221	13	10.00	0	\N	\N	2026-06-24 20:37:44.040576+00
573	231	13	10.00	0	\N	\N	2026-06-24 20:37:44.042694+00
574	31	13	10.00	0	\N	\N	2026-06-24 20:37:44.044736+00
575	18	13	10.00	0	\N	\N	2026-06-24 20:37:44.046693+00
576	39	13	10.00	0	\N	\N	2026-06-24 20:37:44.049373+00
577	17	13	10.00	0	\N	\N	2026-06-24 20:37:44.051412+00
578	116	13	10.00	0	\N	\N	2026-06-24 20:37:44.057836+00
579	87	13	10.00	0	\N	\N	2026-06-24 20:37:44.060296+00
580	68	13	10.00	0	\N	\N	2026-06-24 20:37:44.06269+00
581	70	13	10.00	0	\N	\N	2026-06-24 20:37:44.065133+00
582	80	13	10.00	0	\N	\N	2026-06-24 20:37:44.067593+00
583	146	13	10.00	0	\N	\N	2026-06-24 20:37:44.069796+00
584	132	13	10.00	0	\N	\N	2026-06-24 20:37:44.071902+00
585	84	13	10.00	0	\N	\N	2026-06-24 20:37:44.074109+00
586	170	13	10.00	0	\N	\N	2026-06-24 20:37:44.076411+00
587	192	13	10.00	0	\N	\N	2026-06-24 20:37:44.079132+00
588	169	13	10.00	0	\N	\N	2026-06-24 20:37:44.081227+00
589	176	13	10.00	0	\N	\N	2026-06-24 20:37:44.083598+00
590	92	13	10.00	0	\N	\N	2026-06-24 20:37:44.085636+00
591	69	13	10.00	0	\N	\N	2026-06-24 20:37:44.087592+00
592	180	13	10.00	0	\N	\N	2026-06-24 20:37:44.089549+00
593	115	13	10.00	0	\N	\N	2026-06-24 20:37:44.091767+00
594	97	13	10.00	0	\N	\N	2026-06-24 20:37:44.093861+00
595	108	13	10.00	0	\N	\N	2026-06-24 20:37:44.09597+00
596	112	13	10.00	0	\N	\N	2026-06-24 20:37:44.097893+00
597	22	13	10.00	0	\N	\N	2026-06-24 20:37:44.100437+00
598	156	13	10.00	0	\N	\N	2026-06-24 20:37:44.102676+00
599	59	13	10.00	0	\N	\N	2026-06-24 20:37:44.105042+00
600	197	13	10.00	0	\N	\N	2026-06-24 20:37:44.107218+00
601	135	13	10.00	0	\N	\N	2026-06-24 20:37:44.109188+00
602	127	13	10.00	0	\N	\N	2026-06-24 20:37:44.111251+00
603	227	13	10.00	0	\N	\N	2026-06-24 20:37:44.113968+00
604	98	13	10.00	0	\N	\N	2026-06-24 20:37:44.116423+00
605	173	13	10.00	0	\N	\N	2026-06-24 20:37:44.119069+00
606	73	13	10.00	0	\N	\N	2026-06-24 20:37:44.121288+00
607	200	13	10.00	0	\N	\N	2026-06-24 20:37:44.123537+00
608	11	13	10.00	0	\N	\N	2026-06-24 20:37:44.125552+00
609	103	13	10.00	0	\N	\N	2026-06-24 20:37:44.127738+00
610	189	13	10.00	0	\N	\N	2026-06-24 20:37:44.130298+00
611	161	13	10.00	0	\N	\N	2026-06-24 20:37:44.132504+00
612	117	13	10.00	0	\N	\N	2026-06-24 20:37:44.134434+00
613	235	13	10.00	0	\N	\N	2026-06-24 20:37:44.136785+00
614	88	13	10.00	0	\N	\N	2026-06-24 20:37:44.138858+00
615	125	13	10.00	0	\N	\N	2026-06-24 20:37:44.141085+00
616	119	13	10.00	0	\N	\N	2026-06-24 20:37:44.143062+00
617	113	13	10.00	0	\N	\N	2026-06-24 20:37:44.145904+00
618	153	13	10.00	0	\N	\N	2026-06-24 20:37:44.14813+00
619	147	13	10.00	0	\N	\N	2026-06-24 20:37:44.151359+00
620	214	13	10.00	0	\N	\N	2026-06-24 20:37:44.15438+00
621	9	13	10.00	0	\N	\N	2026-06-24 20:37:44.160789+00
622	196	13	10.00	0	\N	\N	2026-06-24 20:37:44.163059+00
623	15	13	10.00	0	\N	\N	2026-06-24 20:37:44.165265+00
624	79	13	10.00	0	\N	\N	2026-06-24 20:37:44.167977+00
625	210	13	10.00	0	\N	\N	2026-06-24 20:37:44.17016+00
626	26	13	10.00	0	\N	\N	2026-06-24 20:37:44.172168+00
627	48	13	10.00	0	\N	\N	2026-06-24 20:37:44.174722+00
628	85	13	10.00	0	\N	\N	2026-06-24 20:37:44.176846+00
629	72	13	10.00	0	\N	\N	2026-06-24 20:37:44.179019+00
630	95	13	10.00	0	\N	\N	2026-06-24 20:37:44.181773+00
631	19	13	10.00	0	\N	\N	2026-06-24 20:37:44.184263+00
632	160	13	10.00	0	\N	\N	2026-06-24 20:37:44.186763+00
633	232	13	10.00	0	\N	\N	2026-06-24 20:37:44.189563+00
634	172	13	10.00	0	\N	\N	2026-06-24 20:37:44.191909+00
635	21	13	10.00	0	\N	\N	2026-06-24 20:37:44.194497+00
636	131	13	10.00	0	\N	\N	2026-06-24 20:37:44.196691+00
637	3	13	10.00	0	\N	\N	2026-06-24 20:37:44.198896+00
638	198	13	10.00	0	\N	\N	2026-06-24 20:37:44.201319+00
639	212	13	10.00	0	\N	\N	2026-06-24 20:37:44.203717+00
640	28	13	10.00	0	\N	\N	2026-06-24 20:37:44.206059+00
641	37	13	10.00	0	\N	\N	2026-06-24 20:37:44.208565+00
642	204	13	10.00	0	\N	\N	2026-06-24 20:37:44.2108+00
643	5	13	10.00	0	\N	\N	2026-06-24 20:37:44.213094+00
644	104	13	10.00	0	\N	\N	2026-06-24 20:37:44.215746+00
645	165	13	10.00	0	\N	\N	2026-06-24 20:37:44.218195+00
646	151	13	10.00	0	\N	\N	2026-06-24 20:37:44.220616+00
647	91	13	10.00	0	\N	\N	2026-06-24 20:37:44.223514+00
648	209	13	10.00	0	\N	\N	2026-06-24 20:37:44.225769+00
649	74	13	10.00	0	\N	\N	2026-06-24 20:37:44.228436+00
650	179	13	10.00	0	\N	\N	2026-06-24 20:37:44.231806+00
651	29	13	10.00	0	\N	\N	2026-06-24 20:37:44.234074+00
652	4	13	10.00	0	\N	\N	2026-06-24 20:37:44.236883+00
653	138	13	10.00	0	\N	\N	2026-06-24 20:37:44.239099+00
654	225	13	10.00	0	\N	\N	2026-06-24 20:37:44.241201+00
655	181	13	10.00	0	\N	\N	2026-06-24 20:37:44.24369+00
656	205	13	10.00	0	\N	\N	2026-06-24 20:37:44.246016+00
657	96	13	10.00	0	\N	\N	2026-06-24 20:37:44.248204+00
658	67	13	10.00	0	\N	\N	2026-06-24 20:37:44.250615+00
659	201	13	10.00	0	\N	\N	2026-06-24 20:37:44.253087+00
9	9	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.372506+00
1	17	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:38:05.529813+00
660	63	13	10.00	0	\N	\N	2026-06-24 20:37:44.255205+00
4	3	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.3684+00
661	10	13	10.00	0	\N	\N	2026-06-24 20:37:44.257782+00
662	90	13	10.00	0	\N	\N	2026-06-24 20:37:44.259914+00
663	216	13	10.00	0	\N	\N	2026-06-24 20:37:44.262117+00
664	45	13	10.00	0	\N	\N	2026-06-24 20:37:44.26457+00
665	174	13	10.00	0	\N	\N	2026-06-24 20:37:44.267525+00
666	213	13	10.00	0	\N	\N	2026-06-24 20:37:44.270223+00
667	6	13	10.00	0	\N	\N	2026-06-24 20:37:44.272867+00
668	107	13	10.00	0	\N	\N	2026-06-24 20:37:44.275149+00
669	134	13	10.00	0	\N	\N	2026-06-24 20:37:44.277497+00
670	220	13	10.00	0	\N	\N	2026-06-24 20:37:44.279607+00
671	86	13	10.00	0	\N	\N	2026-06-24 20:37:44.281663+00
672	144	13	10.00	0	\N	\N	2026-06-24 20:37:44.283845+00
673	168	13	10.00	0	\N	\N	2026-06-24 20:37:44.286357+00
674	89	13	10.00	0	\N	\N	2026-06-24 20:37:44.288475+00
675	219	13	10.00	0	\N	\N	2026-06-24 20:37:44.290662+00
676	175	13	10.00	0	\N	\N	2026-06-24 20:37:44.293417+00
677	102	13	10.00	0	\N	\N	2026-06-24 20:37:44.296151+00
678	167	13	10.00	0	\N	\N	2026-06-24 20:37:44.29897+00
679	66	13	10.00	0	\N	\N	2026-06-24 20:37:44.301557+00
680	109	13	10.00	0	\N	\N	2026-06-24 20:37:44.303869+00
681	158	13	10.00	0	\N	\N	2026-06-24 20:37:44.306321+00
682	155	13	10.00	0	\N	\N	2026-06-24 20:37:44.308531+00
683	13	13	10.00	0	\N	\N	2026-06-24 20:37:44.310696+00
684	118	13	10.00	0	\N	\N	2026-06-24 20:37:44.313306+00
685	111	13	10.00	0	\N	\N	2026-06-24 20:37:44.315977+00
686	2	13	10.00	0	\N	\N	2026-06-24 20:37:44.318273+00
687	16	13	10.00	0	\N	\N	2026-06-24 20:37:44.320753+00
688	207	13	10.00	0	\N	\N	2026-06-24 20:37:44.322944+00
689	199	13	10.00	0	\N	\N	2026-06-24 20:37:44.325138+00
690	62	13	10.00	0	\N	\N	2026-06-24 20:37:44.327237+00
691	75	13	10.00	0	\N	\N	2026-06-24 20:37:44.330364+00
692	128	13	10.00	0	\N	\N	2026-06-24 20:37:44.332595+00
693	126	13	10.00	0	\N	\N	2026-06-24 20:37:44.334956+00
694	194	13	10.00	0	\N	\N	2026-06-24 20:37:44.337196+00
695	99	13	10.00	0	\N	\N	2026-06-24 20:37:44.339428+00
696	142	13	10.00	0	\N	\N	2026-06-24 20:37:44.341585+00
697	152	13	10.00	0	\N	\N	2026-06-24 20:37:44.344668+00
698	46	13	10.00	0	\N	\N	2026-06-24 20:37:44.350298+00
699	206	13	10.00	0	\N	\N	2026-06-24 20:37:44.352992+00
700	224	13	10.00	0	\N	\N	2026-06-24 20:37:44.355266+00
701	157	13	10.00	0	\N	\N	2026-06-24 20:37:44.358114+00
702	32	13	10.00	0	\N	\N	2026-06-24 20:37:44.360434+00
703	53	13	10.00	0	\N	\N	2026-06-24 20:37:44.363373+00
704	230	13	10.00	0	\N	\N	2026-06-24 20:37:44.365808+00
705	183	13	10.00	0	\N	\N	2026-06-24 20:37:44.36819+00
706	38	13	10.00	0	\N	\N	2026-06-24 20:37:44.370728+00
707	150	13	10.00	0	\N	\N	2026-06-24 20:37:44.37298+00
708	140	13	10.00	0	\N	\N	2026-06-24 20:37:44.375051+00
709	193	13	10.00	0	\N	\N	2026-06-24 20:37:44.377713+00
710	12	13	10.00	0	\N	\N	2026-06-24 20:37:44.379836+00
711	137	13	10.00	0	\N	\N	2026-06-24 20:37:44.381802+00
712	208	13	10.00	0	\N	\N	2026-06-24 20:37:44.383824+00
713	24	13	10.00	0	\N	\N	2026-06-24 20:37:44.386342+00
714	191	13	10.00	0	\N	\N	2026-06-24 20:37:44.388498+00
715	217	13	10.00	0	\N	\N	2026-06-24 20:37:44.390713+00
716	141	13	10.00	0	\N	\N	2026-06-24 20:37:44.393241+00
717	122	13	10.00	0	\N	\N	2026-06-24 20:37:44.395431+00
718	218	13	10.00	0	\N	\N	2026-06-24 20:37:44.397868+00
719	154	13	10.00	0	\N	\N	2026-06-24 20:37:44.400527+00
720	186	13	10.00	0	\N	\N	2026-06-24 20:37:44.40275+00
721	234	13	10.00	0	\N	\N	2026-06-24 20:37:44.405342+00
722	211	13	10.00	0	\N	\N	2026-06-24 20:37:44.408028+00
723	47	13	10.00	0	\N	\N	2026-06-24 20:37:44.410356+00
724	20	13	10.00	0	\N	\N	2026-06-24 20:37:44.412742+00
725	182	13	10.00	0	\N	\N	2026-06-24 20:37:44.416027+00
726	33	13	10.00	0	\N	\N	2026-06-24 20:37:44.418514+00
727	1	13	10.00	0	\N	\N	2026-06-24 20:37:44.421715+00
728	110	13	10.00	0	\N	\N	2026-06-24 20:37:44.4241+00
729	215	13	10.00	0	\N	\N	2026-06-24 20:37:44.426404+00
730	178	13	10.00	0	\N	\N	2026-06-24 20:37:44.428728+00
731	145	13	10.00	0	\N	\N	2026-06-24 20:37:44.430988+00
732	27	13	10.00	0	\N	\N	2026-06-24 20:37:44.433537+00
733	129	13	10.00	0	\N	\N	2026-06-24 20:37:44.435891+00
734	23	13	10.00	0	\N	\N	2026-06-24 20:37:44.43796+00
37	41	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.395043+00
38	42	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.396057+00
39	45	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.396913+00
40	46	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.397783+00
735	143	13	10.00	0	\N	\N	2026-06-24 20:37:44.4404+00
3	2	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.358325+00
5	4	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.369314+00
6	5	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.370251+00
7	6	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.371061+00
8	8	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.371802+00
10	10	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.373232+00
11	11	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.373941+00
12	12	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.375103+00
13	13	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.375825+00
14	15	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.376555+00
15	16	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.377286+00
16	18	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.378004+00
17	19	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.378792+00
18	20	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.379506+00
19	21	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.380237+00
20	22	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.380961+00
21	23	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.381702+00
22	24	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.382434+00
23	26	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.383186+00
24	27	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.384031+00
25	28	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.384855+00
26	29	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.38561+00
27	31	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.386378+00
28	32	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.387133+00
29	33	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.387895+00
30	34	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.388678+00
31	35	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.389497+00
32	36	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.390393+00
33	37	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.391224+00
34	38	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.392342+00
35	39	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.393293+00
36	40	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.394121+00
41	47	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.398787+00
42	48	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.399645+00
43	49	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.400499+00
44	51	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.401485+00
45	53	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.402388+00
46	54	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.403246+00
47	55	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.404255+00
48	56	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.405234+00
49	57	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.406148+00
50	58	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.407463+00
51	59	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.408391+00
52	60	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.409289+00
53	61	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.410294+00
736	58	13	10.00	0	\N	\N	2026-06-24 20:37:44.442815+00
737	228	13	10.00	0	\N	\N	2026-06-24 20:37:44.444947+00
54	62	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.411327+00
55	63	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.412253+00
56	64	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.413244+00
57	65	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.414167+00
58	66	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.415195+00
59	67	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.416347+00
60	68	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.417331+00
61	69	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.41827+00
62	70	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.419314+00
63	72	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.420274+00
64	73	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.421272+00
65	74	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.422509+00
66	75	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.423643+00
67	76	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.424611+00
68	79	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.425579+00
69	80	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.426697+00
70	81	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.42769+00
71	82	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.428659+00
72	83	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.429629+00
73	84	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.430685+00
74	85	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.43167+00
75	86	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.432668+00
76	87	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.433657+00
77	88	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.434676+00
78	89	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.435929+00
79	90	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.43721+00
80	91	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.438609+00
81	92	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.440086+00
82	93	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.44143+00
83	94	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.442721+00
84	95	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.444044+00
85	96	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.44534+00
86	97	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.446639+00
87	98	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.448106+00
88	99	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.449419+00
89	100	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.450727+00
90	101	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.452056+00
91	102	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.453483+00
92	103	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.454949+00
93	104	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.456377+00
94	105	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.457702+00
95	106	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.45906+00
96	107	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.460489+00
97	108	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.461901+00
98	109	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.463408+00
99	110	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.464759+00
100	111	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.466176+00
101	112	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.467584+00
102	113	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.469152+00
103	114	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.470814+00
104	115	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.472295+00
105	116	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.473703+00
106	117	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.475165+00
107	118	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.476691+00
108	119	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.478089+00
109	120	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.479491+00
110	121	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.480881+00
111	122	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.482277+00
112	123	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.48369+00
113	124	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.485087+00
114	125	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.486665+00
115	126	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.488148+00
116	127	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.498092+00
117	128	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.499559+00
118	129	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.500998+00
119	130	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.502429+00
120	131	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.503835+00
121	132	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.505498+00
122	133	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.506973+00
123	134	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.508453+00
124	135	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.509974+00
125	136	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.511785+00
126	137	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.513292+00
127	138	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.514792+00
128	140	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.51649+00
129	141	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.518028+00
130	142	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.519566+00
131	143	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.521109+00
132	144	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.522716+00
133	145	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.524189+00
134	146	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.525647+00
135	147	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.527121+00
136	148	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.528713+00
137	149	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.530273+00
138	150	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.532313+00
139	151	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.533921+00
140	152	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.535488+00
141	153	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.537053+00
142	154	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.53867+00
143	155	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.540252+00
144	156	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.541827+00
145	157	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.543419+00
146	158	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.545168+00
147	159	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.546749+00
148	160	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.550053+00
149	161	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.551467+00
150	164	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.552919+00
151	165	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.554362+00
152	166	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.555883+00
153	167	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.557357+00
154	168	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.558813+00
155	169	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.560253+00
156	170	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.561681+00
157	171	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.563272+00
158	172	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.564806+00
159	173	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.566238+00
160	174	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.567683+00
161	175	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.569271+00
162	176	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.570868+00
163	177	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.572287+00
164	178	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.573613+00
165	179	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.575076+00
166	180	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.576528+00
167	181	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.577843+00
168	182	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.579152+00
169	183	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.580624+00
170	185	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.581869+00
171	186	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.58317+00
172	189	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.584375+00
173	191	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.585493+00
174	192	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.586603+00
175	193	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.587598+00
176	194	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.588678+00
177	195	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.589791+00
178	196	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.590787+00
179	197	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.591813+00
180	198	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.59286+00
181	199	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.593879+00
182	200	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.594955+00
183	201	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.595917+00
184	202	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.596904+00
185	203	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.597927+00
186	204	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.598836+00
187	205	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.599939+00
188	206	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.600976+00
189	207	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.60193+00
190	208	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.602779+00
191	209	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.603822+00
192	210	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.60479+00
193	211	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.605799+00
194	212	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.606735+00
195	213	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.607652+00
196	214	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.608602+00
197	215	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.609528+00
198	216	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.610514+00
199	217	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.611659+00
200	218	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.613008+00
201	219	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.614544+00
202	220	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.615997+00
203	221	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.617131+00
204	224	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.618053+00
205	225	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.618853+00
206	227	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.619796+00
207	228	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.620725+00
208	230	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.621551+00
209	231	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.622502+00
210	232	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.623452+00
211	234	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.624344+00
212	235	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.625202+00
738	8	13	10.00	0	\N	\N	2026-06-24 20:37:44.447211+00
2	1	1	10.00	10.00	2026-06-24	\N	2026-06-24 18:56:39.346474+00
740	7	13	10.00	0	\N	\N	2026-07-04 02:30:03.841806+00
741	78	13	10.00	0	\N	\N	2026-07-04 02:30:03.972478+00
742	139	13	10.00	0	\N	\N	2026-07-04 02:33:10.969782+00
743	44	13	10.00	0	\N	\N	2026-07-04 02:35:45.482242+00
744	43	13	10.00	0	\N	\N	2026-07-04 02:35:45.597563+00
\.


--
-- Data for Name: SeasonParameters; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SeasonParameters" ("Id", "SeasonId", "Key", "Value", "Description", "IsActive") FROM stdin;
2	1	SeasonFeeAmount	10.00	Seasonal play fee for this season	t
6	13	SeasonFeeAmount	10.00	Seasonal play fee for this season	t
\.


--
-- Data for Name: SeasonTimeSlots; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SeasonTimeSlots" ("Id", "SeasonId", "TimeSlotId") FROM stdin;
444	1	3
445	1	11
446	1	16
450	13	3
451	13	11
452	13	16
\.


--
-- Data for Name: Seasons; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Seasons" ("Id", "LeagueId", "Name", "StartDate", "EndDate", "GamesPerSeason", "PlayersPerTeamMinimum", "PlayersPerTeamMaximum", "PointsForWin", "PointsForTie", "PointsForLoss", "PointsForNoShow", "PointsToWinGame", "GamesPerMatch", "ScoringMode", "TeamsInPlayoffs", "FirstPlaceGuaranteed", "PlayoffType", "PlayoffGamesPerMatch", "PlayoffScoringMode", "IsCurrent", "WeeksInSeason", "MaxTeamsInDivision", "Status", "PlayoffStartDate", "PlayoffEndDate", "CreatedAt", "IsLocked", "PlayoffTiebreakerFormat", "ForfeitOpponentPlusMinus", "ForfeitPlusMinus", "CourtDisplayStyle") FROM stdin;
13	1	Season 2027	2027-01-04	\N	0	4	5	2	1	0	-1	12	2	games_mode	12	t	ladder	2	match_play	t	8	8	Setup	2027-02-27	\N	2026-06-24 20:37:43.579333+00	f	1b1p	6	-6	number
1	1	Season 2026	2026-01-05	\N	8	4	5	2	1	0	-1	12	2	games_mode	12	t	ladder	2	match_play	f	8	8	Setup	2026-02-28	\N	2026-06-04 20:43:48.418998+00	f	1b1p	6	-6	number
\.


--
-- Data for Name: SpareLists; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SpareLists" ("Id", "LeagueId", "PlayerId", "IsActive", "Notes") FROM stdin;
4	1	34	t	\N
5	1	35	t	\N
6	1	36	t	\N
8	1	40	t	\N
9	1	41	t	\N
10	1	42	t	\N
11	1	49	t	\N
12	1	51	t	\N
13	1	54	t	\N
14	1	55	t	\N
15	1	56	t	\N
16	1	57	t	\N
17	1	60	t	\N
18	1	61	t	\N
19	1	64	t	\N
20	1	65	t	\N
21	1	76	t	\N
22	1	81	t	\N
23	1	82	t	\N
24	1	83	t	\N
25	1	93	t	\N
26	1	94	t	\N
28	1	100	t	\N
29	1	101	t	\N
30	1	105	t	\N
31	1	106	t	\N
32	1	114	t	\N
33	1	120	t	\N
34	1	121	t	\N
35	1	123	t	\N
36	1	124	t	\N
37	1	130	t	\N
38	1	133	t	\N
39	1	136	t	\N
40	1	148	t	\N
41	1	149	t	\N
42	1	159	t	\N
43	1	164	t	\N
44	1	166	t	\N
45	1	171	t	\N
46	1	177	t	\N
47	1	185	t	\N
48	1	195	t	\N
49	1	202	t	\N
50	1	203	t	\N
51	1	221	t	\N
52	1	231	t	\N
76	1	31	t	\N
79	1	18	t	\N
7	1	39	t	\N
83	1	17	t	\N
\.


--
-- Data for Name: TeamApplicantMembers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamApplicantMembers" ("Id", "TeamApplicantId", "PlayerId", "FirstName", "LastName", "Email", "Phone", "Notes", "CreatedPlayerId") FROM stdin;
\.


--
-- Data for Name: TeamApplicants; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamApplicants" ("Id", "LeagueId", "SeasonId", "GroupName", "ContactEmail", "ContactPhone", "PreferredDivisionId", "Notes", "Status", "PlacedTeamId", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: TeamPlayers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamPlayers" ("Id", "TeamId", "PlayerId", "Role", "IsActive", "JoinedDate") FROM stdin;
77	233	174	player	t	2026-06-12
78	233	69	player	t	2026-06-12
79	233	68	player	t	2026-06-12
80	233	22	captain	t	2026-06-12
82	234	172	player	t	2026-06-12
83	234	115	player	t	2026-06-12
84	234	45	player	t	2026-06-12
81	234	173	captain	t	2026-06-12
86	235	98	player	t	2026-06-12
87	235	96	player	t	2026-06-12
88	235	95	player	t	2026-06-12
85	235	99	captain	t	2026-06-12
89	236	205	player	t	2026-06-12
91	236	29	player	t	2026-06-12
92	236	28	player	t	2026-06-12
90	236	204	captain	t	2026-06-12
93	237	234	player	t	2026-06-12
95	237	82	player	t	2026-06-12
96	237	15	player	t	2026-06-12
94	237	83	captain	t	2026-06-12
97	238	208	player	t	2026-06-12
98	238	207	player	t	2026-06-12
99	238	189	player	t	2026-06-12
100	238	132	captain	t	2026-06-12
36	226	217	player	t	2026-06-12
38	226	186	player	t	2026-06-12
39	226	90	player	t	2026-06-12
40	227	135	player	t	2026-06-12
42	227	91	player	t	2026-06-12
43	227	16	player	t	2026-06-12
41	227	92	captain	t	2026-06-12
1436	630	186	player	t	2026-06-24
104	239	165	player	t	2026-06-12
37	226	215	captain	t	2026-06-12
44	228	149	player	t	2026-06-12
46	228	145	player	t	2026-06-12
47	228	34	player	t	2026-06-12
45	228	146	captain	t	2026-06-12
48	229	104	player	t	2026-06-12
49	229	103	player	t	2026-06-12
105	239	114	player	t	2026-06-12
106	239	100	player	t	2026-06-12
103	239	166	captain	t	2026-06-12
108	240	113	player	t	2026-06-12
109	240	112	player	t	2026-06-12
110	240	107	player	t	2026-06-12
107	240	119	captain	t	2026-06-12
111	241	201	player	t	2026-06-12
112	241	200	player	t	2026-06-12
114	241	13	player	t	2026-06-12
113	241	185	captain	t	2026-06-12
115	242	228	player	t	2026-06-12
116	242	227	player	t	2026-06-12
65	230	175	player	t	2026-06-12
117	242	87	player	t	2026-06-12
66	230	80	player	t	2026-06-12
67	230	79	player	t	2026-06-12
64	230	176	captain	t	2026-06-12
68	231	230	player	t	2026-06-12
69	231	118	player	t	2026-06-12
71	231	70	player	t	2026-06-12
72	231	53	player	t	2026-06-12
70	231	102	captain	t	2026-06-12
73	232	181	player	t	2026-06-12
74	232	180	player	t	2026-06-12
76	232	8	player	t	2026-06-12
75	232	142	captain	t	2026-06-12
118	242	86	captain	t	2026-06-12
119	243	225	player	t	2026-06-12
121	243	177	player	t	2026-06-12
122	243	31	player	t	2026-06-12
120	243	224	captain	t	2026-06-12
123	244	178	player	t	2026-06-12
124	244	157	player	t	2026-06-12
126	244	1	player	t	2026-06-12
125	244	34	captain	t	2026-06-12
127	245	179	player	t	2026-06-12
128	245	168	player	t	2026-06-12
129	245	167	player	t	2026-06-12
130	245	2	captain	t	2026-06-12
131	246	126	player	t	2026-06-12
132	246	125	player	t	2026-06-12
133	246	48	player	t	2026-06-12
134	246	47	player	t	2026-06-12
135	246	36	captain	t	2026-06-12
136	247	154	player	t	2026-06-12
137	247	152	player	t	2026-06-12
138	247	138	player	t	2026-06-12
139	247	137	captain	t	2026-06-12
140	248	216	player	t	2026-06-12
142	248	73	player	t	2026-06-12
143	248	72	player	t	2026-06-12
141	248	206	captain	t	2026-06-12
144	249	18	player	t	2026-06-12
146	249	4	player	t	2026-06-12
147	249	3	player	t	2026-06-12
145	249	17	captain	t	2026-06-12
149	250	153	player	t	2026-06-12
150	250	42	player	t	2026-06-12
151	250	41	player	t	2026-06-12
148	250	220	captain	t	2026-06-12
153	251	211	player	t	2026-06-12
154	251	10	player	t	2026-06-12
155	251	9	player	t	2026-06-12
152	251	212	captain	t	2026-06-12
157	252	196	player	t	2026-06-12
158	252	151	player	t	2026-06-12
159	252	110	player	t	2026-06-12
160	252	109	player	t	2026-06-12
156	252	197	captain	t	2026-06-12
161	253	219	player	t	2026-06-12
162	253	218	player	t	2026-06-12
164	253	39	player	t	2026-06-12
163	253	40	captain	t	2026-06-12
165	254	235	player	t	2026-06-12
167	254	136	player	t	2026-06-12
168	254	97	player	t	2026-06-12
166	254	183	captain	t	2026-06-12
170	255	140	player	t	2026-06-12
171	255	122	player	t	2026-06-12
172	255	111	player	t	2026-06-12
169	255	141	captain	t	2026-06-12
173	256	75	player	t	2026-06-12
174	256	74	player	t	2026-06-12
175	256	12	player	t	2026-06-12
176	256	11	player	t	2026-06-12
177	256	5	captain	t	2026-06-12
178	257	144	player	t	2026-06-12
180	257	21	player	t	2026-06-12
181	257	20	player	t	2026-06-12
179	257	143	captain	t	2026-06-12
182	258	117	player	t	2026-06-12
183	258	116	player	t	2026-06-12
185	258	84	player	t	2026-06-12
184	258	85	captain	t	2026-06-12
186	259	156	player	t	2026-06-12
187	259	155	player	t	2026-06-12
189	259	108	player	t	2026-06-12
188	259	134	captain	t	2026-06-12
190	260	203	player	t	2026-06-12
191	260	202	player	t	2026-06-12
192	260	194	player	t	2026-06-12
194	260	81	player	t	2026-06-12
193	260	193	captain	t	2026-06-12
196	261	170	player	t	2026-06-12
197	261	89	player	t	2026-06-12
198	261	88	player	t	2026-06-12
195	261	171	captain	t	2026-06-12
199	262	199	player	t	2026-06-12
200	262	198	player	t	2026-06-12
202	262	62	player	t	2026-06-12
201	262	63	captain	t	2026-06-12
203	263	182	player	t	2026-06-12
204	263	169	player	t	2026-06-12
206	263	6	player	t	2026-06-12
205	263	19	captain	t	2026-06-12
207	264	161	player	t	2026-06-12
209	264	67	player	t	2026-06-12
210	264	66	player	t	2026-06-12
208	264	160	captain	t	2026-06-12
211	265	214	player	t	2026-06-12
212	265	213	player	t	2026-06-12
214	265	127	player	t	2026-06-12
213	265	128	captain	t	2026-06-12
215	266	232	player	t	2026-06-12
216	266	231	player	t	2026-06-12
217	266	221	player	t	2026-06-12
218	266	210	player	t	2026-06-12
219	266	209	captain	t	2026-06-12
221	267	120	player	t	2026-06-12
222	267	57	player	t	2026-06-12
223	267	56	player	t	2026-06-12
220	267	121	captain	t	2026-06-12
225	268	46	player	t	2026-06-12
226	268	24	player	t	2026-06-12
227	268	23	player	t	2026-06-12
224	268	131	captain	t	2026-06-12
228	269	159	player	t	2026-06-12
229	269	158	player	t	2026-06-12
230	269	130	player	t	2026-06-12
231	269	129	captain	t	2026-06-12
232	270	192	player	t	2026-06-12
233	270	191	player	t	2026-06-12
235	270	147	player	t	2026-06-12
234	270	148	captain	t	2026-06-12
236	271	61	player	t	2026-06-12
237	271	60	player	t	2026-06-12
239	271	49	player	t	2026-06-12
238	271	51	captain	t	2026-06-12
240	272	59	player	t	2026-06-12
241	272	58	player	t	2026-06-12
243	272	32	player	t	2026-06-12
242	272	33	captain	t	2026-06-12
244	273	150	player	t	2026-06-12
246	273	27	player	t	2026-06-12
247	273	26	player	t	2026-06-12
245	273	133	captain	t	2026-06-12
1437	630	217	player	t	2026-06-24
1438	630	90	player	t	2026-06-24
1439	630	215	captain	t	2026-06-24
1440	631	16	player	t	2026-06-24
1441	631	92	captain	t	2026-06-24
1442	631	135	player	t	2026-06-24
1443	631	91	player	t	2026-06-24
1444	632	149	player	t	2026-06-24
1445	632	146	captain	t	2026-06-24
1446	632	34	player	t	2026-06-24
1447	632	145	player	t	2026-06-24
1449	633	38	captain	t	2026-06-24
1450	633	37	player	t	2026-06-24
1452	634	175	player	t	2026-06-24
1453	634	176	captain	t	2026-06-24
1456	635	102	captain	t	2026-06-24
1457	635	53	player	t	2026-06-24
1458	635	70	player	t	2026-06-24
1459	635	118	player	t	2026-06-24
1460	635	230	player	t	2026-06-24
1461	636	142	captain	t	2026-06-24
1462	636	8	player	t	2026-06-24
1463	636	180	player	t	2026-06-24
1464	636	181	player	t	2026-06-24
1465	637	69	player	t	2026-06-24
1466	637	174	player	t	2026-06-24
1467	637	68	player	t	2026-06-24
1468	637	22	captain	t	2026-06-24
1469	638	45	player	t	2026-06-24
1470	638	115	player	t	2026-06-24
1471	638	172	player	t	2026-06-24
1472	638	173	captain	t	2026-06-24
1473	639	95	player	t	2026-06-24
1474	639	96	player	t	2026-06-24
1475	639	98	player	t	2026-06-24
1476	639	99	captain	t	2026-06-24
1477	640	29	player	t	2026-06-24
1478	640	28	player	t	2026-06-24
1479	640	204	captain	t	2026-06-24
1480	640	205	player	t	2026-06-24
1481	641	83	captain	t	2026-06-24
1482	641	82	player	t	2026-06-24
1483	641	234	player	t	2026-06-24
1484	641	15	player	t	2026-06-24
1485	642	207	player	t	2026-06-24
1486	642	132	captain	t	2026-06-24
1487	642	208	player	t	2026-06-24
1488	642	189	player	t	2026-06-24
1489	643	165	player	t	2026-06-24
1490	643	114	player	t	2026-06-24
1491	643	100	player	t	2026-06-24
1492	643	166	captain	t	2026-06-24
1493	644	107	player	t	2026-06-24
1494	644	112	player	t	2026-06-24
1495	644	113	player	t	2026-06-24
1496	644	119	captain	t	2026-06-24
1497	645	13	player	t	2026-06-24
1498	645	200	player	t	2026-06-24
1499	645	201	player	t	2026-06-24
1500	645	185	captain	t	2026-06-24
1501	646	227	player	t	2026-06-24
1502	646	228	player	t	2026-06-24
1503	646	87	player	t	2026-06-24
1504	646	86	captain	t	2026-06-24
1505	647	225	player	t	2026-06-24
1506	647	177	player	t	2026-06-24
1507	647	31	player	t	2026-06-24
1508	647	224	captain	t	2026-06-24
1509	648	178	player	t	2026-06-24
1512	648	34	captain	t	2026-06-24
1513	649	167	player	t	2026-06-24
1514	649	179	player	t	2026-06-24
1515	649	168	player	t	2026-06-24
1516	649	2	captain	t	2026-06-24
1517	650	126	player	t	2026-06-24
1518	650	125	player	t	2026-06-24
1519	650	48	player	t	2026-06-24
1520	650	47	player	t	2026-06-24
1521	650	36	captain	t	2026-06-24
1522	651	154	player	t	2026-06-24
1523	651	152	player	t	2026-06-24
1524	651	138	player	t	2026-06-24
1525	651	137	captain	t	2026-06-24
1526	652	216	player	t	2026-06-24
1527	652	73	player	t	2026-06-24
1528	652	72	player	t	2026-06-24
1529	652	206	captain	t	2026-06-24
1530	653	18	player	t	2026-06-24
1531	653	4	player	t	2026-06-24
1532	653	3	player	t	2026-06-24
1533	653	17	captain	t	2026-06-24
1534	654	153	player	t	2026-06-24
1535	654	42	player	t	2026-06-24
1536	654	41	player	t	2026-06-24
1537	654	220	captain	t	2026-06-24
1538	655	10	player	t	2026-06-24
1539	655	211	player	t	2026-06-24
1540	655	9	player	t	2026-06-24
1541	655	212	captain	t	2026-06-24
1542	656	151	player	t	2026-06-24
1543	656	110	player	t	2026-06-24
1544	656	109	player	t	2026-06-24
1545	656	196	player	t	2026-06-24
1546	656	197	captain	t	2026-06-24
1547	657	219	player	t	2026-06-24
1548	657	218	player	t	2026-06-24
1549	657	39	player	t	2026-06-24
1550	657	40	captain	t	2026-06-24
1551	658	235	player	t	2026-06-24
1552	658	136	player	t	2026-06-24
1553	658	97	player	t	2026-06-24
1554	658	183	captain	t	2026-06-24
1555	659	140	player	t	2026-06-24
1556	659	122	player	t	2026-06-24
1557	659	111	player	t	2026-06-24
1558	659	141	captain	t	2026-06-24
1563	660	5	captain	t	2026-06-24
1564	661	144	player	t	2026-06-24
1565	661	21	player	t	2026-06-24
1566	661	20	player	t	2026-06-24
1567	661	143	captain	t	2026-06-24
1568	662	117	player	t	2026-06-24
1569	662	116	player	t	2026-06-24
1570	662	84	player	t	2026-06-24
1571	662	85	captain	t	2026-06-24
1572	663	156	player	t	2026-06-24
1573	663	155	player	t	2026-06-24
1574	663	108	player	t	2026-06-24
1575	663	134	captain	t	2026-06-24
1578	664	194	player	t	2026-06-24
1579	664	81	player	t	2026-06-24
1580	664	193	captain	t	2026-06-24
1581	665	170	player	t	2026-06-24
1582	665	89	player	t	2026-06-24
1583	665	88	player	t	2026-06-24
1584	665	171	captain	t	2026-06-24
1585	666	199	player	t	2026-06-24
1586	666	198	player	t	2026-06-24
1587	666	62	player	t	2026-06-24
1588	666	63	captain	t	2026-06-24
1589	667	19	captain	t	2026-06-24
1591	667	169	player	t	2026-06-24
1593	668	67	player	t	2026-06-24
1594	668	66	player	t	2026-06-24
1595	668	160	captain	t	2026-06-24
1596	668	161	player	t	2026-06-24
1597	669	214	player	t	2026-06-24
1598	669	213	player	t	2026-06-24
1599	669	127	player	t	2026-06-24
1600	669	128	captain	t	2026-06-24
1601	670	232	player	t	2026-06-24
1602	670	231	player	t	2026-06-24
1603	670	221	player	t	2026-06-24
1604	670	210	player	t	2026-06-24
1605	670	209	captain	t	2026-06-24
1606	671	120	player	t	2026-06-24
1607	671	57	player	t	2026-06-24
1608	671	56	player	t	2026-06-24
1609	671	121	captain	t	2026-06-24
1610	672	46	player	t	2026-06-24
1611	672	24	player	t	2026-06-24
1612	672	23	player	t	2026-06-24
1613	672	131	captain	t	2026-06-24
1614	673	129	captain	t	2026-06-24
1615	673	159	player	t	2026-06-24
1616	673	158	player	t	2026-06-24
1617	673	130	player	t	2026-06-24
1618	674	192	player	t	2026-06-24
1619	674	191	player	t	2026-06-24
1620	674	147	player	t	2026-06-24
1621	674	148	captain	t	2026-06-24
1622	675	61	player	t	2026-06-24
1623	675	60	player	t	2026-06-24
1624	675	49	player	t	2026-06-24
1625	675	51	captain	t	2026-06-24
1626	676	59	player	t	2026-06-24
1627	676	58	player	t	2026-06-24
1628	676	32	player	t	2026-06-24
1629	676	33	captain	t	2026-06-24
1630	677	150	player	t	2026-06-24
1631	677	27	player	t	2026-06-24
1632	677	26	player	t	2026-06-24
1633	677	133	captain	t	2026-06-24
50	229	38	captain	t	2026-06-12
51	229	37	player	t	2026-06-12
1637	692	181	player	t	2026-07-03
1638	692	180	player	t	2026-07-03
1639	692	8	player	t	2026-07-03
1640	692	6	captain	t	2026-07-03
1641	693	104	player	t	2026-07-03
1643	693	7	player	t	2026-07-03
1644	693	78	player	t	2026-07-03
1642	693	103	captain	t	2026-07-03
1645	648	139	player	t	2026-07-03
1646	648	35	player	t	2026-07-03
1647	694	106	player	t	2026-07-03
1648	694	105	player	t	2026-07-03
1649	694	44	player	t	2026-07-03
1650	694	43	captain	t	2026-07-03
\.


--
-- Data for Name: TeamStandings; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamStandings" ("Id", "TeamId", "DivisionId", "Wins", "Losses", "Ties", "NoShows", "StandingsPoints", "PointsFor", "PointsAgainst", "PlusMinus", "DivisionRank", "PlayoffSeed") FROM stdin;
\.


--
-- Data for Name: Teams; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Teams" ("Id", "DivisionId", "TeamLetter", "SystemName", "DisplayName", "CaptainPlayerId", "IsActive", "CreatedAt", "SortOrder") FROM stdin;
230	2	A	A-Mo-1300	A-Roth	176	t	2026-06-11 20:34:59.823615+00	1-1300-A
231	2	B	B-Mo-1300	B-Hoewisch	102	t	2026-06-11 20:34:59.823678+00	1-1300-B
232	2	C	C-Mo-1300	C-McBride	142	t	2026-06-11 20:34:59.823686+00	1-1300-C
233	2	D	D-Mo-1300	D-Bregar	22	t	2026-06-11 20:34:59.823693+00	1-1300-D
234	4	A	A-Tu-0900	A-Roiger	173	t	2026-06-11 20:34:59.825375+00	2-0900-A
235	4	B	B-Tu-0900	B-Hansen	99	t	2026-06-11 20:34:59.825406+00	2-0900-B
236	4	C	C-Tu-0900	C-Stork	204	t	2026-06-11 20:34:59.825415+00	2-0900-C
237	4	D	D-Tu-0900	D-Gohde	83	t	2026-06-11 20:34:59.825421+00	2-0900-D
238	5	A	A-Tu-1300	A-Leeper	132	t	2026-06-11 20:34:59.826989+00	2-1300-A
239	5	B	B-Tu-1300	B-Reiner	166	t	2026-06-11 20:34:59.827023+00	2-1300-B
240	5	C	C-Tu-1300	C-Knowlton	119	t	2026-06-11 20:34:59.827032+00	2-1300-C
241	5	D	D-Tu-1300	D-Seidal	185	t	2026-06-11 20:34:59.82704+00	2-1300-D
242	7	A	A-We-0900	A-Greenlee	86	t	2026-06-11 20:34:59.82869+00	3-0900-A
243	7	B	B-We-0900	B-Wagner	224	t	2026-06-11 20:34:59.828733+00	3-0900-B
244	7	C	C-We-0900	C-Burgi	34	t	2026-06-11 20:34:59.828745+00	3-0900-C
245	7	D	D-We-0900	D-Anderson	2	t	2026-06-11 20:34:59.828751+00	3-0900-D
246	8	A	A-We-1300	A-Carlson	36	t	2026-06-11 20:34:59.830381+00	3-1300-A
247	8	B	B-We-1300	B-Mahoney	137	t	2026-06-11 20:34:59.830414+00	3-1300-B
248	8	C	C-We-1300	C-Strong	206	t	2026-06-11 20:34:59.830423+00	3-1300-C
249	8	D	D-We-1300	D-Billings	17	t	2026-06-11 20:34:59.830429+00	3-1300-D
250	9	A	A-We-1530	A-Turner	220	t	2026-06-11 20:34:59.832133+00	3-1530-A
251	9	B	B-We-1530	B-Taylor	212	t	2026-06-11 20:34:59.832166+00	3-1530-B
252	9	C	C-We-1530	C-Squire	197	t	2026-06-11 20:34:59.832175+00	3-1530-C
254	10	A	A-Th-0900	A-Schnider	183	t	2026-06-11 20:34:59.833917+00	4-0900-A
253	9	D	D-We-1530	D-Chamberlin	40	t	2026-06-11 20:34:59.832184+00	3-1530-D
255	10	B	B-Th-0900	B-May	141	t	2026-06-11 20:34:59.83396+00	4-0900-B
256	10	C	C-Th-0900	C-Austin	5	t	2026-06-11 20:34:59.833971+00	4-0900-C
257	10	D	D-Th-0900	D-McCune	143	t	2026-06-11 20:34:59.833981+00	4-0900-D
258	11	A	A-Th-1300	A-Goulet	85	t	2026-06-11 20:34:59.835593+00	4-1300-A
259	11	B	B-Th-1300	B-Litzinger	134	t	2026-06-11 20:34:59.835624+00	4-1300-B
260	11	C	C-Th-1300	C-Sobolik	193	t	2026-06-11 20:34:59.835655+00	4-1300-C
261	11	D	D-Th-1300	D-Roebbeke	171	t	2026-06-11 20:34:59.835662+00	4-1300-D
262	13	A	A-Fr-0900	A-Ellsworth	63	t	2026-06-11 20:34:59.83763+00	5-0900-A
263	13	B	B-Fr-0900	B-Blosser	19	t	2026-06-11 20:34:59.837674+00	5-0900-B
264	13	C	C-Fr-0900	C-Posselt	160	t	2026-06-11 20:34:59.837687+00	5-0900-C
265	13	D	D-Fr-0900	D-Kulesa	128	t	2026-06-11 20:34:59.837694+00	5-0900-D
266	14	A	A-Fr-1300	A-Symes	209	t	2026-06-11 20:34:59.839769+00	5-1300-A
267	14	B	B-Fr-1300	B-Kopecky	121	t	2026-06-11 20:34:59.839811+00	5-1300-B
268	14	C	C-Fr-1300	C-Lane	131	t	2026-06-11 20:34:59.83982+00	5-1300-C
269	14	D	D-Fr-1300	D-Landon	129	t	2026-06-11 20:34:59.839827+00	5-1300-D
270	15	A	A-Fr-1530	A-Mingay	148	t	2026-06-11 20:34:59.841718+00	5-1530-A
271	15	B	B-Fr-1530	B-Davis	51	t	2026-06-11 20:34:59.841757+00	5-1530-B
272	15	C	C-Fr-1530	C-Bulthuis	33	t	2026-06-11 20:34:59.841768+00	5-1530-C
273	15	D	D-Fr-1530	D-Linahon	133	t	2026-06-11 20:34:59.841775+00	5-1530-D
650	159	A	A-We-1300	A-Carlson	36	t	2026-06-24 20:37:43.807091+00	3-1300-A
228	1	C	C-Mo-0900	C-McGee	146	t	2026-06-11 20:34:59.791502+00	1-0900-C
630	155	A	A-Mo-0900	A-Thomson	215	t	2026-06-24 20:37:43.741626+00	
651	159	B	B-We-1300	B-Mahoney	137	t	2026-06-24 20:37:43.809003+00	3-1300-B
227	1	B	B-Mo-0900	B-Guck	92	t	2026-06-11 20:34:59.791214+00	1-0900-B
226	1	A	A-Mo-0900	A-Thomson	215	t	2026-06-11 20:34:59.782162+00	1-0900-A
652	159	C	C-We-1300	C-Strong	206	t	2026-06-24 20:37:43.811049+00	3-1300-C
653	159	D	D-We-1300	D-Billings	17	t	2026-06-24 20:37:43.813025+00	3-1300-D
229	1	D	D-Mo-0900	D-Casper	38	t	2026-06-11 20:34:59.791519+00	1-0900-D
631	155	B	B-Mo-0900	B-Guck	92	t	2026-06-24 20:37:43.751913+00	
632	155	C	C-Mo-0900	C-McGee	146	t	2026-06-24 20:37:43.766405+00	
633	155	D	D-Mo-0900	D-Casper	38	t	2026-06-24 20:37:43.7689+00	
634	157	A	A-Mo-1300	A-Roth	176	t	2026-06-24 20:37:43.770732+00	
635	157	B	B-Mo-1300	B-Hoewisch	102	t	2026-06-24 20:37:43.772597+00	
636	157	C	C-Mo-1300	C-McBride	142	t	2026-06-24 20:37:43.774416+00	
637	157	D	D-Mo-1300	D-Bregar	22	t	2026-06-24 20:37:43.776242+00	
642	160	A	A-Tu-1300	A-Leeper	132	t	2026-06-24 20:37:43.785448+00	
643	160	B	B-Tu-1300	B-Reiner	166	t	2026-06-24 20:37:43.793106+00	
644	160	C	C-Tu-1300	C-Knowlton	119	t	2026-06-24 20:37:43.79511+00	
645	160	D	D-Tu-1300	D-Seidal	185	t	2026-06-24 20:37:43.796992+00	
646	156	A	A-We-0900	A-Greenlee	86	t	2026-06-24 20:37:43.799222+00	
647	156	B	B-We-0900	B-Wagner	224	t	2026-06-24 20:37:43.801211+00	
648	156	C	C-We-0900	C-Burgi	34	t	2026-06-24 20:37:43.803162+00	
649	156	D	D-We-0900	D-Anderson	2	t	2026-06-24 20:37:43.805156+00	
654	158	A	A-We-1530	A-Turner	220	t	2026-06-24 20:37:43.815339+00	
655	158	B	B-We-1530	B-Taylor	212	t	2026-06-24 20:37:43.817431+00	
638	152	A	A-Tu-0900	A-Roiger	173	t	2026-06-24 20:37:43.778166+00	2-0900-A
639	152	B	B-Tu-0900	B-Hansen	99	t	2026-06-24 20:37:43.779961+00	2-0900-B
640	152	C	C-Tu-0900	C-Stork	204	t	2026-06-24 20:37:43.781736+00	2-0900-C
641	152	D	D-Tu-0900	D-Gohde	83	t	2026-06-24 20:37:43.783586+00	2-0900-D
656	158	C	C-We-1530	C-Squire	197	t	2026-06-24 20:37:43.819793+00	
657	158	D	D-We-1530	D-Chamberlin	40	t	2026-06-24 20:37:43.821991+00	
658	154	A	A-Th-0900	A-Schnider	183	t	2026-06-24 20:37:43.824025+00	
659	154	B	B-Th-0900	B-May	141	t	2026-06-24 20:37:43.826027+00	
660	154	C	C-Th-0900	C-Austin	5	t	2026-06-24 20:37:43.828169+00	
661	154	D	D-Th-0900	D-McCune	143	t	2026-06-24 20:37:43.830432+00	
662	161	A	A-Th-1300	A-Goulet	85	t	2026-06-24 20:37:43.83273+00	
663	161	B	B-Th-1300	B-Litzinger	134	t	2026-06-24 20:37:43.834772+00	
664	161	C	C-Th-1300	C-Sobolik	193	t	2026-06-24 20:37:43.837079+00	
665	161	D	D-Th-1300	D-Roebbeke	171	t	2026-06-24 20:37:43.839206+00	
670	163	A	A-Fr-1300	A-Symes	209	t	2026-06-24 20:37:43.849831+00	
671	163	B	B-Fr-1300	B-Kopecky	121	t	2026-06-24 20:37:43.85193+00	
672	163	C	C-Fr-1300	C-Lane	131	t	2026-06-24 20:37:43.854778+00	
673	163	D	D-Fr-1300	D-Landon	129	t	2026-06-24 20:37:43.857121+00	
674	162	A	A-Fr-1530	A-Mingay	148	t	2026-06-24 20:37:43.85939+00	
675	162	B	B-Fr-1530	B-Davis	51	t	2026-06-24 20:37:43.862179+00	
676	162	C	C-Fr-1530	C-Bulthuis	33	t	2026-06-24 20:37:43.864783+00	
677	162	D	D-Fr-1530	D-Linahon	133	t	2026-06-24 20:37:43.867263+00	
666	153	A	A-Fr-0900	A-Ellsworth	63	t	2026-06-24 20:37:43.841328+00	5-0900-A
667	153	B	B-Fr-0900	B-Blosser	19	t	2026-06-24 20:37:43.843389+00	5-0900-B
668	153	C	C-Fr-0900	C-Posselt	160	t	2026-06-24 20:37:43.845502+00	5-0900-C
669	153	D	D-Fr-0900	D-Kulesa	128	t	2026-06-24 20:37:43.847703+00	5-0900-D
692	155	E	E-Mo-0900	E-Banitt	6	t	2026-07-04 02:27:48.59753+00	1-0900-E
693	155	F	F-Mo-0900	F-Holmes	103	t	2026-07-04 02:27:50.020199+00	1-0900-F
695	153	F	F-Fr-0900	F-Fr-0900	\N	t	2026-07-04 02:35:20.084055+00	5-0900-F
694	153	E	E-Fr-0900	E-Clark	43	t	2026-07-04 02:35:16.455953+00	5-0900-E
\.


--
-- Data for Name: TimeSlots; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TimeSlots" ("Id", "Timeslot12h", "Timeslot24h", "SortOrder", "IsActive") FROM stdin;
1	8:00 AM	0800	1	t
2	8:30 AM	0830	2	t
3	9:00 AM	0900	3	t
4	9:30 AM	0930	4	t
5	10:00 AM	1000	5	t
6	10:30 AM	1030	6	t
7	11:00 AM	1100	7	t
8	11:30 AM	1130	8	t
9	12:00 PM	1200	9	t
10	12:30 PM	1230	10	t
11	1:00 PM	1300	11	t
12	1:30 PM	1330	12	t
13	2:00 PM	1400	13	t
14	2:30 PM	1430	14	t
15	3:00 PM	1500	15	t
16	3:30 PM	1530	16	t
17	4:00 PM	1600	17	t
18	4:30 PM	1630	18	t
19	5:00 PM	1700	19	t
20	5:30 PM	1730	20	t
21	6:00 PM	1800	21	t
22	6:30 PM	1830	22	t
23	7:00 PM	1900	23	t
24	7:30 PM	1930	24	t
25	8:00 PM	2000	25	t
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260604203734_InitialPostgresSchema	9.0.0
20260606014510_AddPlayerPartnerLink	9.0.0
20260606235458_AddTeamFoundToLookingForTeam	9.0.0
20260607000924_ReplaceTeamFoundWithSeasonId	9.0.0
20260607003104_AddNewIdeasTable	9.0.0
20260609202918_RemoveDeprecatedPlayerLookingForTeamField	9.0.0
20260609231804_RemoveSeasonIsActiveAddStatus	9.0.0
20260611203303_AddTeamSortOrderRemoveIsByeTeam	9.0.0
20260611215107_FixLookingForTeamConstraint	9.0.0
20260611222955_UpdateCourtEntity	9.0.0
20260611230353_AddIsLockedToSeason	9.0.0
20260612193010_AddScheduleTemplates	9.0.0
20260613191449_AddReports	9.0.0
20260613194053_AddReportTables	9.0.0
20260614000448_AddSortOrderToCourts	9.0.0
20260614230449_AddIsLockedToScheduleTemplate	9.0.0
20260615202126_AddScheduleDivisions	9.0.0
20260618224650_AddPlayoffTiebreakerFormat	9.0.0
20260619023437_AddScoreColumnsToScheduleDivisions	9.0.0
20260623015514_AddNotesToSpareList	9.0.0
20260623230646_AddFeeParameters	9.0.0
20260624195526_RemoveUnusedTables	9.0.0
20260624235502_AddTeamApplicants	9.0.0
20260625201046_EnhanceLookingForTeam	9.0.0
20260626015103_AddNameToLookingForTeamGroup	9.0.0
20260626022214_AddPreferredDaysAndTimes	9.0.0
20260626022711_AddPreferredDaysAndTimesToLFT	9.0.0
20260626213716_AddGroupLeaderAndUniqueGroupName	9.0.0
20260627035220_MakeScoresNullable	9.0.0
20260627040746_NullifyDefaultScores	9.0.0
20260629225736_AddSeasonForfeitPlusMinusParameters	9.0.0
20260629233248_UpdateScoringViewAddPointsColumns	9.0.0
20260630000443_CreateStandingsView	9.0.0
20260630180859_RemoveGameIntervalFromSeason	9.0.0
20260630180935_AddStandingsSeeding	9.0.0
20260630181610_AddStandingsH2HTiebreaker	9.0.0
20260630215230_FixSeasonSeedOrdering	9.0.0
20260701204029_AddPlayoffConfigAndBracketStructure	9.0.0
20260701213031_AddPlayoffConfigTiebreakerBalls	9.0.0
20260701214954_RenamePlayoffDayParamsGapToMatchLength	9.0.0
20260701222603_AddPlayoffCourts	9.0.0
20260702225250_AddSeasonCourtsPriorityAndDisplayStyle_RemovePlayoffCourts	9.0.0
20260704015347_AddPlayerRoleAndPlayerRolesTable	9.0.0
20260704185807_AddDesignJsonToAnnouncement	9.0.0
\.


--
-- Name: Announcements_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Announcements_Id_seq"', 1, false);


--
-- Name: AppParameters_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."AppParameters_Id_seq"', 25, true);


--
-- Name: ClubDocuments_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ClubDocuments_Id_seq"', 8, true);


--
-- Name: Courts_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Courts_Id_seq"', 11, true);


--
-- Name: DaySlots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."DaySlots_Id_seq"', 7, true);


--
-- Name: Divisions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Divisions_Id_seq"', 170, true);


--
-- Name: EmailListMembers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."EmailListMembers_Id_seq"', 1, false);


--
-- Name: EmailLists_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."EmailLists_Id_seq"', 1, false);


--
-- Name: EmailLogs_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."EmailLogs_Id_seq"', 1, false);


--
-- Name: Games_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Games_Id_seq"', 1, false);


--
-- Name: InitiationFees_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."InitiationFees_Id_seq"', 240, true);


--
-- Name: LeagueParameters_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LeagueParameters_Id_seq"', 2, true);


--
-- Name: Leagues_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Leagues_Id_seq"', 2, true);


--
-- Name: LookingForTeamDivisions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LookingForTeamDivisions_Id_seq"', 1, false);


--
-- Name: LookingForTeamGroups_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LookingForTeamGroups_Id_seq"', 18, true);


--
-- Name: LookingForTeamPreferredDay_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LookingForTeamPreferredDay_Id_seq"', 9, true);


--
-- Name: LookingForTeamPreferredTime_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LookingForTeamPreferredTime_Id_seq"', 7, true);


--
-- Name: LookingForTeams_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."LookingForTeams_Id_seq"', 117, true);


--
-- Name: MatchTeamResults_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."MatchTeamResults_Id_seq"', 1, false);


--
-- Name: Matches_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Matches_Id_seq"', 1, false);


--
-- Name: NewIdeas_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."NewIdeas_Id_seq"', 6, true);


--
-- Name: PlayerRoles_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayerRoles_Id_seq"', 7, false);


--
-- Name: Players_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Players_Id_seq"', 241, true);


--
-- Name: PlayoffConfigs_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffConfigs_Id_seq"', 2, true);


--
-- Name: PlayoffDayParams_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffDayParams_Id_seq"', 219, true);


--
-- Name: PlayoffGames_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffGames_Id_seq"', 38, true);


--
-- Name: PlayoffMatches_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffMatches_Id_seq"', 535, true);


--
-- Name: PlayoffRounds_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffRounds_Id_seq"', 160, true);


--
-- Name: PlayoffSeedings_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."PlayoffSeedings_Id_seq"', 884, true);


--
-- Name: ReportParameters_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ReportParameters_Id_seq"', 1, false);


--
-- Name: Reports_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Reports_Id_seq"', 3, true);


--
-- Name: ScheduleDivisions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ScheduleDivisions_Id_seq"', 5728, true);


--
-- Name: ScheduleTemplateMatches_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ScheduleTemplateMatches_Id_seq"', 1636, true);


--
-- Name: ScheduleTemplateWeeks_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ScheduleTemplateWeeks_Id_seq"', 618, true);


--
-- Name: ScheduleTemplates_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ScheduleTemplates_Id_seq"', 78, true);


--
-- Name: ScheduleWeeks_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."ScheduleWeeks_Id_seq"', 1, false);


--
-- Name: SeasonCourts_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SeasonCourts_Id_seq"', 351, true);


--
-- Name: SeasonDaySlots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SeasonDaySlots_Id_seq"', 753, true);


--
-- Name: SeasonFees_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SeasonFees_Id_seq"', 746, true);


--
-- Name: SeasonParameters_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SeasonParameters_Id_seq"', 7, true);


--
-- Name: SeasonTimeSlots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SeasonTimeSlots_Id_seq"', 452, true);


--
-- Name: Seasons_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Seasons_Id_seq"', 14, true);


--
-- Name: SpareLists_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."SpareLists_Id_seq"', 83, true);


--
-- Name: TeamApplicantMembers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."TeamApplicantMembers_Id_seq"', 1, false);


--
-- Name: TeamApplicants_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."TeamApplicants_Id_seq"', 1, false);


--
-- Name: TeamPlayers_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."TeamPlayers_Id_seq"', 1652, true);


--
-- Name: TeamStandings_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."TeamStandings_Id_seq"', 1, false);


--
-- Name: Teams_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Teams_Id_seq"', 695, true);


--
-- Name: TimeSlots_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."TimeSlots_Id_seq"', 25, true);


--
-- Name: ScheduleTemplates IX_ScheduleTemplates_SeasonId_TeamCount; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplates"
    ADD CONSTRAINT "IX_ScheduleTemplates_SeasonId_TeamCount" UNIQUE ("SeasonId", "TeamCount");


--
-- Name: Announcements PK_Announcements; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Announcements"
    ADD CONSTRAINT "PK_Announcements" PRIMARY KEY ("Id");


--
-- Name: AppParameters PK_AppParameters; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."AppParameters"
    ADD CONSTRAINT "PK_AppParameters" PRIMARY KEY ("Id");


--
-- Name: ClubDocuments PK_ClubDocuments; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ClubDocuments"
    ADD CONSTRAINT "PK_ClubDocuments" PRIMARY KEY ("Id");


--
-- Name: Courts PK_Courts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Courts"
    ADD CONSTRAINT "PK_Courts" PRIMARY KEY ("Id");


--
-- Name: DaySlots PK_DaySlots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DaySlots"
    ADD CONSTRAINT "PK_DaySlots" PRIMARY KEY ("Id");


--
-- Name: Divisions PK_Divisions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Divisions"
    ADD CONSTRAINT "PK_Divisions" PRIMARY KEY ("Id");


--
-- Name: EmailListMembers PK_EmailListMembers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailListMembers"
    ADD CONSTRAINT "PK_EmailListMembers" PRIMARY KEY ("Id");


--
-- Name: EmailLists PK_EmailLists; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailLists"
    ADD CONSTRAINT "PK_EmailLists" PRIMARY KEY ("Id");


--
-- Name: EmailLogs PK_EmailLogs; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailLogs"
    ADD CONSTRAINT "PK_EmailLogs" PRIMARY KEY ("Id");


--
-- Name: Games PK_Games; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Games"
    ADD CONSTRAINT "PK_Games" PRIMARY KEY ("Id");


--
-- Name: InitiationFees PK_InitiationFees; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiationFees"
    ADD CONSTRAINT "PK_InitiationFees" PRIMARY KEY ("Id");


--
-- Name: LeagueParameters PK_LeagueParameters; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LeagueParameters"
    ADD CONSTRAINT "PK_LeagueParameters" PRIMARY KEY ("Id");


--
-- Name: Leagues PK_Leagues; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Leagues"
    ADD CONSTRAINT "PK_Leagues" PRIMARY KEY ("Id");


--
-- Name: LookingForTeamDivisions PK_LookingForTeamDivisions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamDivisions"
    ADD CONSTRAINT "PK_LookingForTeamDivisions" PRIMARY KEY ("Id");


--
-- Name: LookingForTeamGroups PK_LookingForTeamGroups; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamGroups"
    ADD CONSTRAINT "PK_LookingForTeamGroups" PRIMARY KEY ("Id");


--
-- Name: LookingForTeamPreferredDays PK_LookingForTeamPreferredDays; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredDays"
    ADD CONSTRAINT "PK_LookingForTeamPreferredDays" PRIMARY KEY ("Id");


--
-- Name: LookingForTeamPreferredTimes PK_LookingForTeamPreferredTimes; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredTimes"
    ADD CONSTRAINT "PK_LookingForTeamPreferredTimes" PRIMARY KEY ("Id");


--
-- Name: LookingForTeams PK_LookingForTeams; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "PK_LookingForTeams" PRIMARY KEY ("Id");


--
-- Name: MatchTeamResults PK_MatchTeamResults; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MatchTeamResults"
    ADD CONSTRAINT "PK_MatchTeamResults" PRIMARY KEY ("Id");


--
-- Name: Matches PK_Matches; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Matches"
    ADD CONSTRAINT "PK_Matches" PRIMARY KEY ("Id");


--
-- Name: NewIdeas PK_NewIdeas; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."NewIdeas"
    ADD CONSTRAINT "PK_NewIdeas" PRIMARY KEY ("Id");


--
-- Name: PlayerRoles PK_PlayerRoles; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayerRoles"
    ADD CONSTRAINT "PK_PlayerRoles" PRIMARY KEY ("Id");


--
-- Name: Players PK_Players; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Players"
    ADD CONSTRAINT "PK_Players" PRIMARY KEY ("Id");


--
-- Name: PlayoffConfigs PK_PlayoffConfigs; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffConfigs"
    ADD CONSTRAINT "PK_PlayoffConfigs" PRIMARY KEY ("Id");


--
-- Name: PlayoffDayParams PK_PlayoffDayParams; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffDayParams"
    ADD CONSTRAINT "PK_PlayoffDayParams" PRIMARY KEY ("Id");


--
-- Name: PlayoffGames PK_PlayoffGames; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffGames"
    ADD CONSTRAINT "PK_PlayoffGames" PRIMARY KEY ("Id");


--
-- Name: PlayoffMatches PK_PlayoffMatches; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "PK_PlayoffMatches" PRIMARY KEY ("Id");


--
-- Name: PlayoffRounds PK_PlayoffRounds; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffRounds"
    ADD CONSTRAINT "PK_PlayoffRounds" PRIMARY KEY ("Id");


--
-- Name: PlayoffSeedings PK_PlayoffSeedings; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffSeedings"
    ADD CONSTRAINT "PK_PlayoffSeedings" PRIMARY KEY ("Id");


--
-- Name: ScheduleDivisions PK_ScheduleDivisions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "PK_ScheduleDivisions" PRIMARY KEY ("Id");


--
-- Name: ScheduleWeeks PK_ScheduleWeeks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleWeeks"
    ADD CONSTRAINT "PK_ScheduleWeeks" PRIMARY KEY ("Id");


--
-- Name: SeasonCourts PK_SeasonCourts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonCourts"
    ADD CONSTRAINT "PK_SeasonCourts" PRIMARY KEY ("Id");


--
-- Name: SeasonDaySlots PK_SeasonDaySlots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonDaySlots"
    ADD CONSTRAINT "PK_SeasonDaySlots" PRIMARY KEY ("Id");


--
-- Name: SeasonFees PK_SeasonFees; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonFees"
    ADD CONSTRAINT "PK_SeasonFees" PRIMARY KEY ("Id");


--
-- Name: SeasonParameters PK_SeasonParameters; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonParameters"
    ADD CONSTRAINT "PK_SeasonParameters" PRIMARY KEY ("Id");


--
-- Name: SeasonTimeSlots PK_SeasonTimeSlots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonTimeSlots"
    ADD CONSTRAINT "PK_SeasonTimeSlots" PRIMARY KEY ("Id");


--
-- Name: Seasons PK_Seasons; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Seasons"
    ADD CONSTRAINT "PK_Seasons" PRIMARY KEY ("Id");


--
-- Name: SpareLists PK_SpareLists; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SpareLists"
    ADD CONSTRAINT "PK_SpareLists" PRIMARY KEY ("Id");


--
-- Name: TeamApplicantMembers PK_TeamApplicantMembers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicantMembers"
    ADD CONSTRAINT "PK_TeamApplicantMembers" PRIMARY KEY ("Id");


--
-- Name: TeamApplicants PK_TeamApplicants; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicants"
    ADD CONSTRAINT "PK_TeamApplicants" PRIMARY KEY ("Id");


--
-- Name: TeamPlayers PK_TeamPlayers; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamPlayers"
    ADD CONSTRAINT "PK_TeamPlayers" PRIMARY KEY ("Id");


--
-- Name: TeamStandings PK_TeamStandings; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamStandings"
    ADD CONSTRAINT "PK_TeamStandings" PRIMARY KEY ("Id");


--
-- Name: Teams PK_Teams; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "PK_Teams" PRIMARY KEY ("Id");


--
-- Name: TimeSlots PK_TimeSlots; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TimeSlots"
    ADD CONSTRAINT "PK_TimeSlots" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: ReportParameters ReportParameters_ReportId_ParameterName_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReportParameters"
    ADD CONSTRAINT "ReportParameters_ReportId_ParameterName_key" UNIQUE ("ReportId", "ParameterName");


--
-- Name: ReportParameters ReportParameters_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReportParameters"
    ADD CONSTRAINT "ReportParameters_pkey" PRIMARY KEY ("Id");


--
-- Name: Reports Reports_Name_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Reports"
    ADD CONSTRAINT "Reports_Name_key" UNIQUE ("Name");


--
-- Name: Reports Reports_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Reports"
    ADD CONSTRAINT "Reports_pkey" PRIMARY KEY ("Id");


--
-- Name: ScheduleTemplateMatches ScheduleTemplateMatches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateMatches"
    ADD CONSTRAINT "ScheduleTemplateMatches_pkey" PRIMARY KEY ("Id");


--
-- Name: ScheduleTemplateWeeks ScheduleTemplateWeeks_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateWeeks"
    ADD CONSTRAINT "ScheduleTemplateWeeks_pkey" PRIMARY KEY ("Id");


--
-- Name: ScheduleTemplates ScheduleTemplates_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplates"
    ADD CONSTRAINT "ScheduleTemplates_pkey" PRIMARY KEY ("Id");


--
-- Name: IX_Announcements_LeagueId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Announcements_LeagueId" ON public."Announcements" USING btree ("LeagueId");


--
-- Name: IX_AppParameters_Key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_AppParameters_Key" ON public."AppParameters" USING btree ("Key");


--
-- Name: IX_Divisions_DaySlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Divisions_DaySlotId" ON public."Divisions" USING btree ("DaySlotId");


--
-- Name: IX_Divisions_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Divisions_SeasonId" ON public."Divisions" USING btree ("SeasonId");


--
-- Name: IX_Divisions_TimeSlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Divisions_TimeSlotId" ON public."Divisions" USING btree ("TimeSlotId");


--
-- Name: IX_EmailListMembers_EmailListId_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_EmailListMembers_EmailListId_PlayerId" ON public."EmailListMembers" USING btree ("EmailListId", "PlayerId");


--
-- Name: IX_EmailListMembers_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EmailListMembers_PlayerId" ON public."EmailListMembers" USING btree ("PlayerId");


--
-- Name: IX_EmailLists_LeagueId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EmailLists_LeagueId" ON public."EmailLists" USING btree ("LeagueId");


--
-- Name: IX_EmailLogs_LeagueId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_EmailLogs_LeagueId" ON public."EmailLogs" USING btree ("LeagueId");


--
-- Name: IX_Games_MatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Games_MatchId" ON public."Games" USING btree ("MatchId");


--
-- Name: IX_InitiationFees_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_InitiationFees_PlayerId" ON public."InitiationFees" USING btree ("PlayerId");


--
-- Name: IX_LeagueParameters_LeagueId_Key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LeagueParameters_LeagueId_Key" ON public."LeagueParameters" USING btree ("LeagueId", "Key");


--
-- Name: IX_LookingForTeamDivisions_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeamDivisions_DivisionId" ON public."LookingForTeamDivisions" USING btree ("DivisionId");


--
-- Name: IX_LookingForTeamDivisions_LookingForTeamId_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LookingForTeamDivisions_LookingForTeamId_DivisionId" ON public."LookingForTeamDivisions" USING btree ("LookingForTeamId", "DivisionId");


--
-- Name: IX_LookingForTeamGroups_GroupLeaderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeamGroups_GroupLeaderId" ON public."LookingForTeamGroups" USING btree ("GroupLeaderId");


--
-- Name: IX_LookingForTeamGroups_LeagueId_SeasonId_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LookingForTeamGroups_LeagueId_SeasonId_Name" ON public."LookingForTeamGroups" USING btree ("LeagueId", "SeasonId", "Name");


--
-- Name: IX_LookingForTeamGroups_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeamGroups_SeasonId" ON public."LookingForTeamGroups" USING btree ("SeasonId");


--
-- Name: IX_LookingForTeamPreferredDays_DaySlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeamPreferredDays_DaySlotId" ON public."LookingForTeamPreferredDays" USING btree ("DaySlotId");


--
-- Name: IX_LookingForTeamPreferredDays_LookingForTeamId_DaySlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LookingForTeamPreferredDays_LookingForTeamId_DaySlotId" ON public."LookingForTeamPreferredDays" USING btree ("LookingForTeamId", "DaySlotId");


--
-- Name: IX_LookingForTeamPreferredTimes_LookingForTeamId_TimeSlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LookingForTeamPreferredTimes_LookingForTeamId_TimeSlotId" ON public."LookingForTeamPreferredTimes" USING btree ("LookingForTeamId", "TimeSlotId");


--
-- Name: IX_LookingForTeamPreferredTimes_TimeSlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeamPreferredTimes_TimeSlotId" ON public."LookingForTeamPreferredTimes" USING btree ("TimeSlotId");


--
-- Name: IX_LookingForTeams_LeagueId_PlayerId_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_LookingForTeams_LeagueId_PlayerId_SeasonId" ON public."LookingForTeams" USING btree ("LeagueId", "PlayerId", "SeasonId");


--
-- Name: IX_LookingForTeams_LookingForTeamGroupId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeams_LookingForTeamGroupId" ON public."LookingForTeams" USING btree ("LookingForTeamGroupId");


--
-- Name: IX_LookingForTeams_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeams_PlayerId" ON public."LookingForTeams" USING btree ("PlayerId");


--
-- Name: IX_LookingForTeams_PreferredTeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeams_PreferredTeamId" ON public."LookingForTeams" USING btree ("PreferredTeamId");


--
-- Name: IX_LookingForTeams_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeams_SeasonId" ON public."LookingForTeams" USING btree ("SeasonId");


--
-- Name: IX_LookingForTeams_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_LookingForTeams_TeamId" ON public."LookingForTeams" USING btree ("TeamId");


--
-- Name: IX_MatchTeamResults_MatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MatchTeamResults_MatchId" ON public."MatchTeamResults" USING btree ("MatchId");


--
-- Name: IX_MatchTeamResults_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_MatchTeamResults_TeamId" ON public."MatchTeamResults" USING btree ("TeamId");


--
-- Name: IX_Matches_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Matches_CourtId" ON public."Matches" USING btree ("CourtId");


--
-- Name: IX_Matches_ScheduleWeekId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Matches_ScheduleWeekId" ON public."Matches" USING btree ("ScheduleWeekId");


--
-- Name: IX_Matches_Team1Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Matches_Team1Id" ON public."Matches" USING btree ("Team1Id");


--
-- Name: IX_Matches_Team2Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Matches_Team2Id" ON public."Matches" USING btree ("Team2Id");


--
-- Name: IX_Players_PartnerPlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Players_PartnerPlayerId" ON public."Players" USING btree ("PartnerPlayerId");


--
-- Name: IX_PlayoffConfigs_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffConfigs_SeasonId" ON public."PlayoffConfigs" USING btree ("SeasonId");


--
-- Name: IX_PlayoffDayParams_PlayoffConfigId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffDayParams_PlayoffConfigId" ON public."PlayoffDayParams" USING btree ("PlayoffConfigId");


--
-- Name: IX_PlayoffGames_PlayoffMatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffGames_PlayoffMatchId" ON public."PlayoffGames" USING btree ("PlayoffMatchId");


--
-- Name: IX_PlayoffMatches_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_CourtId" ON public."PlayoffMatches" USING btree ("CourtId");


--
-- Name: IX_PlayoffMatches_NextMatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_NextMatchId" ON public."PlayoffMatches" USING btree ("NextMatchId");


--
-- Name: IX_PlayoffMatches_PlayoffRoundId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_PlayoffRoundId" ON public."PlayoffMatches" USING btree ("PlayoffRoundId");


--
-- Name: IX_PlayoffMatches_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_SeasonId" ON public."PlayoffMatches" USING btree ("SeasonId");


--
-- Name: IX_PlayoffMatches_Team1Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_Team1Id" ON public."PlayoffMatches" USING btree ("Team1Id");


--
-- Name: IX_PlayoffMatches_Team2Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_Team2Id" ON public."PlayoffMatches" USING btree ("Team2Id");


--
-- Name: IX_PlayoffMatches_WinnerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffMatches_WinnerId" ON public."PlayoffMatches" USING btree ("WinnerId");


--
-- Name: IX_PlayoffRounds_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffRounds_SeasonId" ON public."PlayoffRounds" USING btree ("SeasonId");


--
-- Name: IX_PlayoffSeedings_SeasonId_Seed; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_PlayoffSeedings_SeasonId_Seed" ON public."PlayoffSeedings" USING btree ("SeasonId", "Seed");


--
-- Name: IX_PlayoffSeedings_TeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_PlayoffSeedings_TeamId" ON public."PlayoffSeedings" USING btree ("TeamId");


--
-- Name: IX_Reports_Name; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Reports_Name" ON public."Reports" USING btree ("Name");


--
-- Name: IX_ScheduleDivisions_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleDivisions_CourtId" ON public."ScheduleDivisions" USING btree ("CourtId");


--
-- Name: IX_ScheduleDivisions_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleDivisions_DivisionId" ON public."ScheduleDivisions" USING btree ("DivisionId");


--
-- Name: IX_ScheduleDivisions_Team1Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleDivisions_Team1Id" ON public."ScheduleDivisions" USING btree ("Team1Id");


--
-- Name: IX_ScheduleDivisions_Team2Id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleDivisions_Team2Id" ON public."ScheduleDivisions" USING btree ("Team2Id");


--
-- Name: IX_ScheduleDivisions_TemplateId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleDivisions_TemplateId" ON public."ScheduleDivisions" USING btree ("TemplateId");


--
-- Name: IX_ScheduleTemplateMatches_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleTemplateMatches_CourtId" ON public."ScheduleTemplateMatches" USING btree ("CourtId");


--
-- Name: IX_ScheduleTemplateMatches_TemplateWeekId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleTemplateMatches_TemplateWeekId" ON public."ScheduleTemplateMatches" USING btree ("TemplateWeekId");


--
-- Name: IX_ScheduleTemplateWeeks_TemplateId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleTemplateWeeks_TemplateId" ON public."ScheduleTemplateWeeks" USING btree ("TemplateId");


--
-- Name: IX_ScheduleWeeks_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_ScheduleWeeks_DivisionId" ON public."ScheduleWeeks" USING btree ("DivisionId");


--
-- Name: IX_SeasonCourts_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SeasonCourts_CourtId" ON public."SeasonCourts" USING btree ("CourtId");


--
-- Name: IX_SeasonCourts_SeasonId_CourtId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SeasonCourts_SeasonId_CourtId" ON public."SeasonCourts" USING btree ("SeasonId", "CourtId");


--
-- Name: IX_SeasonDaySlots_DaySlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SeasonDaySlots_DaySlotId" ON public."SeasonDaySlots" USING btree ("DaySlotId");


--
-- Name: IX_SeasonDaySlots_SeasonId_DaySlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SeasonDaySlots_SeasonId_DaySlotId" ON public."SeasonDaySlots" USING btree ("SeasonId", "DaySlotId");


--
-- Name: IX_SeasonFees_PlayerId_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SeasonFees_PlayerId_SeasonId" ON public."SeasonFees" USING btree ("PlayerId", "SeasonId");


--
-- Name: IX_SeasonFees_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SeasonFees_SeasonId" ON public."SeasonFees" USING btree ("SeasonId");


--
-- Name: IX_SeasonParameters_SeasonId_Key; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SeasonParameters_SeasonId_Key" ON public."SeasonParameters" USING btree ("SeasonId", "Key");


--
-- Name: IX_SeasonTimeSlots_SeasonId_TimeSlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SeasonTimeSlots_SeasonId_TimeSlotId" ON public."SeasonTimeSlots" USING btree ("SeasonId", "TimeSlotId");


--
-- Name: IX_SeasonTimeSlots_TimeSlotId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SeasonTimeSlots_TimeSlotId" ON public."SeasonTimeSlots" USING btree ("TimeSlotId");


--
-- Name: IX_Seasons_LeagueId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Seasons_LeagueId" ON public."Seasons" USING btree ("LeagueId");


--
-- Name: IX_SpareLists_LeagueId_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_SpareLists_LeagueId_PlayerId" ON public."SpareLists" USING btree ("LeagueId", "PlayerId");


--
-- Name: IX_SpareLists_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_SpareLists_PlayerId" ON public."SpareLists" USING btree ("PlayerId");


--
-- Name: IX_TeamApplicantMembers_CreatedPlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicantMembers_CreatedPlayerId" ON public."TeamApplicantMembers" USING btree ("CreatedPlayerId");


--
-- Name: IX_TeamApplicantMembers_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicantMembers_PlayerId" ON public."TeamApplicantMembers" USING btree ("PlayerId");


--
-- Name: IX_TeamApplicantMembers_TeamApplicantId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicantMembers_TeamApplicantId" ON public."TeamApplicantMembers" USING btree ("TeamApplicantId");


--
-- Name: IX_TeamApplicants_LeagueId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicants_LeagueId" ON public."TeamApplicants" USING btree ("LeagueId");


--
-- Name: IX_TeamApplicants_PlacedTeamId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicants_PlacedTeamId" ON public."TeamApplicants" USING btree ("PlacedTeamId");


--
-- Name: IX_TeamApplicants_PreferredDivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicants_PreferredDivisionId" ON public."TeamApplicants" USING btree ("PreferredDivisionId");


--
-- Name: IX_TeamApplicants_SeasonId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamApplicants_SeasonId" ON public."TeamApplicants" USING btree ("SeasonId");


--
-- Name: IX_TeamPlayers_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamPlayers_PlayerId" ON public."TeamPlayers" USING btree ("PlayerId");


--
-- Name: IX_TeamPlayers_TeamId_PlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_TeamPlayers_TeamId_PlayerId" ON public."TeamPlayers" USING btree ("TeamId", "PlayerId");


--
-- Name: IX_TeamStandings_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_TeamStandings_DivisionId" ON public."TeamStandings" USING btree ("DivisionId");


--
-- Name: IX_TeamStandings_TeamId_DivisionId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_TeamStandings_TeamId_DivisionId" ON public."TeamStandings" USING btree ("TeamId", "DivisionId");


--
-- Name: IX_Teams_CaptainPlayerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Teams_CaptainPlayerId" ON public."Teams" USING btree ("CaptainPlayerId");


--
-- Name: IX_Teams_DivisionId_TeamLetter; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Teams_DivisionId_TeamLetter" ON public."Teams" USING btree ("DivisionId", "TeamLetter");


--
-- Name: Announcements FK_Announcements_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Announcements"
    ADD CONSTRAINT "FK_Announcements_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id");


--
-- Name: Divisions FK_Divisions_DaySlots_DaySlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Divisions"
    ADD CONSTRAINT "FK_Divisions_DaySlots_DaySlotId" FOREIGN KEY ("DaySlotId") REFERENCES public."DaySlots"("Id");


--
-- Name: Divisions FK_Divisions_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Divisions"
    ADD CONSTRAINT "FK_Divisions_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: Divisions FK_Divisions_TimeSlots_TimeSlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Divisions"
    ADD CONSTRAINT "FK_Divisions_TimeSlots_TimeSlotId" FOREIGN KEY ("TimeSlotId") REFERENCES public."TimeSlots"("Id");


--
-- Name: EmailListMembers FK_EmailListMembers_EmailLists_EmailListId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailListMembers"
    ADD CONSTRAINT "FK_EmailListMembers_EmailLists_EmailListId" FOREIGN KEY ("EmailListId") REFERENCES public."EmailLists"("Id") ON DELETE CASCADE;


--
-- Name: EmailListMembers FK_EmailListMembers_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailListMembers"
    ADD CONSTRAINT "FK_EmailListMembers_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE CASCADE;


--
-- Name: EmailLists FK_EmailLists_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailLists"
    ADD CONSTRAINT "FK_EmailLists_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: EmailLogs FK_EmailLogs_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."EmailLogs"
    ADD CONSTRAINT "FK_EmailLogs_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id");


--
-- Name: Games FK_Games_Matches_MatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Games"
    ADD CONSTRAINT "FK_Games_Matches_MatchId" FOREIGN KEY ("MatchId") REFERENCES public."Matches"("Id") ON DELETE CASCADE;


--
-- Name: InitiationFees FK_InitiationFees_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."InitiationFees"
    ADD CONSTRAINT "FK_InitiationFees_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE CASCADE;


--
-- Name: LeagueParameters FK_LeagueParameters_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LeagueParameters"
    ADD CONSTRAINT "FK_LeagueParameters_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamDivisions FK_LookingForTeamDivisions_Divisions_DivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamDivisions"
    ADD CONSTRAINT "FK_LookingForTeamDivisions_Divisions_DivisionId" FOREIGN KEY ("DivisionId") REFERENCES public."Divisions"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamDivisions FK_LookingForTeamDivisions_LookingForTeams_LookingForTeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamDivisions"
    ADD CONSTRAINT "FK_LookingForTeamDivisions_LookingForTeams_LookingForTeamId" FOREIGN KEY ("LookingForTeamId") REFERENCES public."LookingForTeams"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamGroups FK_LookingForTeamGroups_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamGroups"
    ADD CONSTRAINT "FK_LookingForTeamGroups_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE RESTRICT;


--
-- Name: LookingForTeamGroups FK_LookingForTeamGroups_LookingForTeams_GroupLeaderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamGroups"
    ADD CONSTRAINT "FK_LookingForTeamGroups_LookingForTeams_GroupLeaderId" FOREIGN KEY ("GroupLeaderId") REFERENCES public."LookingForTeams"("Id") ON DELETE SET NULL;


--
-- Name: LookingForTeamGroups FK_LookingForTeamGroups_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamGroups"
    ADD CONSTRAINT "FK_LookingForTeamGroups_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamPreferredDays FK_LookingForTeamPreferredDays_DaySlots_DaySlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredDays"
    ADD CONSTRAINT "FK_LookingForTeamPreferredDays_DaySlots_DaySlotId" FOREIGN KEY ("DaySlotId") REFERENCES public."DaySlots"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamPreferredDays FK_LookingForTeamPreferredDays_LookingForTeams_LookingForTeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredDays"
    ADD CONSTRAINT "FK_LookingForTeamPreferredDays_LookingForTeams_LookingForTeamId" FOREIGN KEY ("LookingForTeamId") REFERENCES public."LookingForTeams"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamPreferredTimes FK_LookingForTeamPreferredTimes_LookingForTeams_LookingForTeam~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredTimes"
    ADD CONSTRAINT "FK_LookingForTeamPreferredTimes_LookingForTeams_LookingForTeam~" FOREIGN KEY ("LookingForTeamId") REFERENCES public."LookingForTeams"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeamPreferredTimes FK_LookingForTeamPreferredTimes_TimeSlots_TimeSlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeamPreferredTimes"
    ADD CONSTRAINT "FK_LookingForTeamPreferredTimes_TimeSlots_TimeSlotId" FOREIGN KEY ("TimeSlotId") REFERENCES public."TimeSlots"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeams FK_LookingForTeams_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeams FK_LookingForTeams_LookingForTeamGroups_LookingForTeamGroupId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_LookingForTeamGroups_LookingForTeamGroupId" FOREIGN KEY ("LookingForTeamGroupId") REFERENCES public."LookingForTeamGroups"("Id") ON DELETE SET NULL;


--
-- Name: LookingForTeams FK_LookingForTeams_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE RESTRICT;


--
-- Name: LookingForTeams FK_LookingForTeams_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: LookingForTeams FK_LookingForTeams_Teams_PreferredTeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_Teams_PreferredTeamId" FOREIGN KEY ("PreferredTeamId") REFERENCES public."Teams"("Id") ON DELETE SET NULL;


--
-- Name: LookingForTeams FK_LookingForTeams_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."LookingForTeams"
    ADD CONSTRAINT "FK_LookingForTeams_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE SET NULL;


--
-- Name: MatchTeamResults FK_MatchTeamResults_Matches_MatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MatchTeamResults"
    ADD CONSTRAINT "FK_MatchTeamResults_Matches_MatchId" FOREIGN KEY ("MatchId") REFERENCES public."Matches"("Id") ON DELETE CASCADE;


--
-- Name: MatchTeamResults FK_MatchTeamResults_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."MatchTeamResults"
    ADD CONSTRAINT "FK_MatchTeamResults_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: Matches FK_Matches_Courts_CourtId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Matches"
    ADD CONSTRAINT "FK_Matches_Courts_CourtId" FOREIGN KEY ("CourtId") REFERENCES public."Courts"("Id");


--
-- Name: Matches FK_Matches_ScheduleWeeks_ScheduleWeekId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Matches"
    ADD CONSTRAINT "FK_Matches_ScheduleWeeks_ScheduleWeekId" FOREIGN KEY ("ScheduleWeekId") REFERENCES public."ScheduleWeeks"("Id") ON DELETE CASCADE;


--
-- Name: Matches FK_Matches_Teams_Team1Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Matches"
    ADD CONSTRAINT "FK_Matches_Teams_Team1Id" FOREIGN KEY ("Team1Id") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: Matches FK_Matches_Teams_Team2Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Matches"
    ADD CONSTRAINT "FK_Matches_Teams_Team2Id" FOREIGN KEY ("Team2Id") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: Players FK_Players_Players_PartnerPlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Players"
    ADD CONSTRAINT "FK_Players_Players_PartnerPlayerId" FOREIGN KEY ("PartnerPlayerId") REFERENCES public."Players"("Id") ON DELETE SET NULL;


--
-- Name: PlayoffConfigs FK_PlayoffConfigs_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffConfigs"
    ADD CONSTRAINT "FK_PlayoffConfigs_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffDayParams FK_PlayoffDayParams_PlayoffConfigs_PlayoffConfigId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffDayParams"
    ADD CONSTRAINT "FK_PlayoffDayParams_PlayoffConfigs_PlayoffConfigId" FOREIGN KEY ("PlayoffConfigId") REFERENCES public."PlayoffConfigs"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffGames FK_PlayoffGames_PlayoffMatches_PlayoffMatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffGames"
    ADD CONSTRAINT "FK_PlayoffGames_PlayoffMatches_PlayoffMatchId" FOREIGN KEY ("PlayoffMatchId") REFERENCES public."PlayoffMatches"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffMatches FK_PlayoffMatches_Courts_CourtId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_Courts_CourtId" FOREIGN KEY ("CourtId") REFERENCES public."Courts"("Id");


--
-- Name: PlayoffMatches FK_PlayoffMatches_PlayoffMatches_NextMatchId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_PlayoffMatches_NextMatchId" FOREIGN KEY ("NextMatchId") REFERENCES public."PlayoffMatches"("Id") ON DELETE SET NULL;


--
-- Name: PlayoffMatches FK_PlayoffMatches_PlayoffRounds_PlayoffRoundId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_PlayoffRounds_PlayoffRoundId" FOREIGN KEY ("PlayoffRoundId") REFERENCES public."PlayoffRounds"("Id");


--
-- Name: PlayoffMatches FK_PlayoffMatches_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffMatches FK_PlayoffMatches_Teams_Team1Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_Teams_Team1Id" FOREIGN KEY ("Team1Id") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: PlayoffMatches FK_PlayoffMatches_Teams_Team2Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_Teams_Team2Id" FOREIGN KEY ("Team2Id") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: PlayoffMatches FK_PlayoffMatches_Teams_WinnerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffMatches"
    ADD CONSTRAINT "FK_PlayoffMatches_Teams_WinnerId" FOREIGN KEY ("WinnerId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: PlayoffRounds FK_PlayoffRounds_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffRounds"
    ADD CONSTRAINT "FK_PlayoffRounds_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffSeedings FK_PlayoffSeedings_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffSeedings"
    ADD CONSTRAINT "FK_PlayoffSeedings_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: PlayoffSeedings FK_PlayoffSeedings_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."PlayoffSeedings"
    ADD CONSTRAINT "FK_PlayoffSeedings_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE RESTRICT;


--
-- Name: ReportParameters FK_ReportParameters_Reports_ReportId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ReportParameters"
    ADD CONSTRAINT "FK_ReportParameters_Reports_ReportId" FOREIGN KEY ("ReportId") REFERENCES public."Reports"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleDivisions FK_ScheduleDivisions_Courts_CourtId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "FK_ScheduleDivisions_Courts_CourtId" FOREIGN KEY ("CourtId") REFERENCES public."Courts"("Id");


--
-- Name: ScheduleDivisions FK_ScheduleDivisions_Divisions_DivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "FK_ScheduleDivisions_Divisions_DivisionId" FOREIGN KEY ("DivisionId") REFERENCES public."Divisions"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleDivisions FK_ScheduleDivisions_ScheduleTemplates_TemplateId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "FK_ScheduleDivisions_ScheduleTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES public."ScheduleTemplates"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleDivisions FK_ScheduleDivisions_Teams_Team1Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "FK_ScheduleDivisions_Teams_Team1Id" FOREIGN KEY ("Team1Id") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleDivisions FK_ScheduleDivisions_Teams_Team2Id; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleDivisions"
    ADD CONSTRAINT "FK_ScheduleDivisions_Teams_Team2Id" FOREIGN KEY ("Team2Id") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleWeeks FK_ScheduleWeeks_Divisions_DivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleWeeks"
    ADD CONSTRAINT "FK_ScheduleWeeks_Divisions_DivisionId" FOREIGN KEY ("DivisionId") REFERENCES public."Divisions"("Id") ON DELETE CASCADE;


--
-- Name: SeasonCourts FK_SeasonCourts_Courts_CourtId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonCourts"
    ADD CONSTRAINT "FK_SeasonCourts_Courts_CourtId" FOREIGN KEY ("CourtId") REFERENCES public."Courts"("Id") ON DELETE CASCADE;


--
-- Name: SeasonCourts FK_SeasonCourts_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonCourts"
    ADD CONSTRAINT "FK_SeasonCourts_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: SeasonDaySlots FK_SeasonDaySlots_DaySlots_DaySlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonDaySlots"
    ADD CONSTRAINT "FK_SeasonDaySlots_DaySlots_DaySlotId" FOREIGN KEY ("DaySlotId") REFERENCES public."DaySlots"("Id") ON DELETE CASCADE;


--
-- Name: SeasonDaySlots FK_SeasonDaySlots_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonDaySlots"
    ADD CONSTRAINT "FK_SeasonDaySlots_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: SeasonFees FK_SeasonFees_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonFees"
    ADD CONSTRAINT "FK_SeasonFees_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE CASCADE;


--
-- Name: SeasonFees FK_SeasonFees_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonFees"
    ADD CONSTRAINT "FK_SeasonFees_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: SeasonParameters FK_SeasonParameters_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonParameters"
    ADD CONSTRAINT "FK_SeasonParameters_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: SeasonTimeSlots FK_SeasonTimeSlots_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonTimeSlots"
    ADD CONSTRAINT "FK_SeasonTimeSlots_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: SeasonTimeSlots FK_SeasonTimeSlots_TimeSlots_TimeSlotId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SeasonTimeSlots"
    ADD CONSTRAINT "FK_SeasonTimeSlots_TimeSlots_TimeSlotId" FOREIGN KEY ("TimeSlotId") REFERENCES public."TimeSlots"("Id") ON DELETE CASCADE;


--
-- Name: Seasons FK_Seasons_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Seasons"
    ADD CONSTRAINT "FK_Seasons_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: SpareLists FK_SpareLists_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SpareLists"
    ADD CONSTRAINT "FK_SpareLists_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE CASCADE;


--
-- Name: SpareLists FK_SpareLists_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."SpareLists"
    ADD CONSTRAINT "FK_SpareLists_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE RESTRICT;


--
-- Name: TeamApplicantMembers FK_TeamApplicantMembers_Players_CreatedPlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicantMembers"
    ADD CONSTRAINT "FK_TeamApplicantMembers_Players_CreatedPlayerId" FOREIGN KEY ("CreatedPlayerId") REFERENCES public."Players"("Id") ON DELETE SET NULL;


--
-- Name: TeamApplicantMembers FK_TeamApplicantMembers_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicantMembers"
    ADD CONSTRAINT "FK_TeamApplicantMembers_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE RESTRICT;


--
-- Name: TeamApplicantMembers FK_TeamApplicantMembers_TeamApplicants_TeamApplicantId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicantMembers"
    ADD CONSTRAINT "FK_TeamApplicantMembers_TeamApplicants_TeamApplicantId" FOREIGN KEY ("TeamApplicantId") REFERENCES public."TeamApplicants"("Id") ON DELETE CASCADE;


--
-- Name: TeamApplicants FK_TeamApplicants_Divisions_PreferredDivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicants"
    ADD CONSTRAINT "FK_TeamApplicants_Divisions_PreferredDivisionId" FOREIGN KEY ("PreferredDivisionId") REFERENCES public."Divisions"("Id") ON DELETE SET NULL;


--
-- Name: TeamApplicants FK_TeamApplicants_Leagues_LeagueId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicants"
    ADD CONSTRAINT "FK_TeamApplicants_Leagues_LeagueId" FOREIGN KEY ("LeagueId") REFERENCES public."Leagues"("Id") ON DELETE RESTRICT;


--
-- Name: TeamApplicants FK_TeamApplicants_Seasons_SeasonId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicants"
    ADD CONSTRAINT "FK_TeamApplicants_Seasons_SeasonId" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- Name: TeamApplicants FK_TeamApplicants_Teams_PlacedTeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamApplicants"
    ADD CONSTRAINT "FK_TeamApplicants_Teams_PlacedTeamId" FOREIGN KEY ("PlacedTeamId") REFERENCES public."Teams"("Id") ON DELETE SET NULL;


--
-- Name: TeamPlayers FK_TeamPlayers_Players_PlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamPlayers"
    ADD CONSTRAINT "FK_TeamPlayers_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES public."Players"("Id") ON DELETE CASCADE;


--
-- Name: TeamPlayers FK_TeamPlayers_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamPlayers"
    ADD CONSTRAINT "FK_TeamPlayers_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: TeamStandings FK_TeamStandings_Divisions_DivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamStandings"
    ADD CONSTRAINT "FK_TeamStandings_Divisions_DivisionId" FOREIGN KEY ("DivisionId") REFERENCES public."Divisions"("Id") ON DELETE CASCADE;


--
-- Name: TeamStandings FK_TeamStandings_Teams_TeamId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."TeamStandings"
    ADD CONSTRAINT "FK_TeamStandings_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES public."Teams"("Id") ON DELETE CASCADE;


--
-- Name: Teams FK_Teams_Divisions_DivisionId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "FK_Teams_Divisions_DivisionId" FOREIGN KEY ("DivisionId") REFERENCES public."Divisions"("Id") ON DELETE CASCADE;


--
-- Name: Teams FK_Teams_Players_CaptainPlayerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Teams"
    ADD CONSTRAINT "FK_Teams_Players_CaptainPlayerId" FOREIGN KEY ("CaptainPlayerId") REFERENCES public."Players"("Id") ON DELETE RESTRICT;


--
-- Name: ScheduleTemplateMatches ScheduleTemplateMatches_CourtId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateMatches"
    ADD CONSTRAINT "ScheduleTemplateMatches_CourtId_fkey" FOREIGN KEY ("CourtId") REFERENCES public."Courts"("Id") ON DELETE RESTRICT;


--
-- Name: ScheduleTemplateMatches ScheduleTemplateMatches_TemplateWeekId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateMatches"
    ADD CONSTRAINT "ScheduleTemplateMatches_TemplateWeekId_fkey" FOREIGN KEY ("TemplateWeekId") REFERENCES public."ScheduleTemplateWeeks"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleTemplateWeeks ScheduleTemplateWeeks_TemplateId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplateWeeks"
    ADD CONSTRAINT "ScheduleTemplateWeeks_TemplateId_fkey" FOREIGN KEY ("TemplateId") REFERENCES public."ScheduleTemplates"("Id") ON DELETE CASCADE;


--
-- Name: ScheduleTemplates ScheduleTemplates_SeasonId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."ScheduleTemplates"
    ADD CONSTRAINT "ScheduleTemplates_SeasonId_fkey" FOREIGN KEY ("SeasonId") REFERENCES public."Seasons"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict 4yMCRZinp27Q9jB1wNF5swessz0YvJwuPlHo38HtzcyVXLUG0hodF2qlfaQ4UCD

