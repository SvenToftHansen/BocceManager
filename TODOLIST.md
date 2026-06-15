# BocceManager — Master Todo List
_Generated 2026-06-15 from TODO.md, memory files, and app state analysis_

---

## In Progress / Partially Done

- [ ] **Schedule Panel — Division Schedules tab** — Tab 1 (Templates) is complete; Tab 2 (Division Schedules viewer) is a "coming soon" stub
- [ ] **Schedule Templates Part 2** — Part 1 (circle-method round-robin + court balancing) committed; Part 2 (actual generation from template into live schedule) not started
- [ ] **IsLocked Enforcement** — Implemented in DivisionPanel and PlayerPanel only; remaining panels need it (see Season Lifecycle section)

---

## Core Panels Not Yet Built

- [ ] **Score Entry Panel** — Enter match results per division/week
  - Supports `games_mode` and `match_play` scoring modes
  - On save: recalculate MatchTeamResult → update TeamStanding → re-rank division
  - Block entry when season is locked (IsLocked enforcement)

- [ ] **Standings Panel** — Calculated standings per division
  - Sort: StandingsPoints DESC → PlusMinus DESC → Wins DESC
  - Show DivisionRank and PlayoffSeed

- [ ] **Playoffs Panel** — Bracket/ladder management and results
  - Playoff types: ladder or round_robin
  - Scoring mode: PlayoffScoringMode (default match_play)
  - Seeds from TeamStanding.PlayoffSeed
  - Lock down when season is Completed

- [ ] **Spare Lists Panel** — Players available as spares per division
  - Players opt-in; show contact info for coordinators
  - Linked to SpareListPlayer entity

- [ ] **Announcements Panel** — Create and manage club announcements

- [ ] **Fees Panel** — Track initiation and season fees per player
  - InitiationFee: one-time, fields AmountOwing / AmountPaid / PaidDate
  - SeasonFee: unique per (PlayerId, SeasonId); only created when payment recorded

- [ ] **Email Lists Panel** — Manage distribution groups used by the email client

- [ ] **Players Panel** — Full CRUD editor for player records
  - Fields: first/last name, email, phone, lot#, active flag, looking-for-team flag
  - Assign players to teams from this panel
  - Mark LFT players with ◆ in picker

---

## Season Management

- [ ] **Clone Season Wizard** — when creating a new season, offer option to copy structure from a previous Locked or Completed season
  - Dropdown to select the source season (filtered to Locked/Completed only)
  - Checkboxes/option buttons for what to copy:
    - Teams (roster structure, not scores)
    - Parameters (division settings, court assignments, schedule config, etc.)
  - Copying is optional — user can create a blank season if preferred

- [ ] **Season Status Transition Validation** — Prevent backward status transitions (e.g., Playoff Play → League Play not allowed)
  - Show confirmation dialog before any status change
  - Enforce: Setup → League Play → Playoff Play → Completed (one-way only)
  - Document destructive backward transitions (deletes schedule, scores, brackets)

- [ ] **IsLocked Enforcement — Remaining Panels**
  - Score Entry panel — block score entry when season is locked
  - Schedule panel — block schedule creation/modification when locked
  - Standings panel — read-only when season is Completed
  - Playoffs panel — lock structure once released; full lock on Completed

- [ ] **IsLocked Manual Testing** (checklist)
  - [ ] Add division when season locked → blocked with message
  - [ ] Add team when season locked → blocked with message
  - [ ] Assign player when season locked → blocked with message
  - [ ] Mark Looking for Team in locked season → error shown
  - [ ] Create new player when season locked → should succeed (not assigned to team)

---

## Schedule

- [ ] **Division Schedules tab** — view generated schedule per division/week (inside SchedulePanel)
- [ ] **Schedule Templates Part 2** — apply template to generate live weekly schedule
- [ ] **Court Assignment Integration** — add court selection to match/schedule creation
- [ ] **Court Validation** — validate court assignments when building schedules (no double-booking)
- [ ] **Display Courts on Schedule** — show assigned courts on schedule/match views

---

## Reporting (FastReport + WebView2 + PostgreSQL)

- [ ] **Install NuGet packages** — FastReport.Core (free), Microsoft.Web.WebView2
- [ ] **Reports table** — PostgreSQL table: id, name, category, description, frx_content (text), is_active, updated_at
- [ ] **Report Viewer Panel** — unified viewer using WebView2 to display PDF rendered from .frx
- [ ] **Dynamic report menu** — builds sidebar/menu items from DB contents at startup
- [ ] **Import/Export UI** — panel to import .frx files into DB and export for external editing in FastReport Designer
- [ ] **Schedule Report (.frx)** — first-draft FastReport definition for schedule by week/division
- [ ] **Teams Report (.frx)** — first-draft FastReport definition for team roster by division
- [ ] **Standings Report (.frx)** — first-draft for standings by division
- [ ] **Document Creator** — combine multiple reports into one PDF for distribution
  - User selects which reports to include
  - Merge and output to configured PDF folder

---

## Data Import / Export

- [ ] **Import Player Records** — bulk import from prior PostgreSQL season or TSV file
  - Map old PostgreSQL schema → new entities
  - TSV format: FirstName, LastName, Email, PhoneNumber, LotNumber, IsActivePlayer, lookingForTeam, onsublist, InitiationFeePaid
  - Handle LookingForTeam and SpareList linkage on import

- [ ] **Export Tool** — export key datasets to Excel (.xlsx)
  - Candidates: Players list, Team rosters, Standings, Schedule, Fees

---

## Courts

- [ ] **Auto-default court number/letter** — when adding a new court, pre-fill next sequential values
- [ ] **Courts on Reports** — display assigned courts on schedule reports and match views

---

## Communications

- [ ] **In-App Email Client + Brevo Integration** — full email compose panel
  - Recipient picker: individuals, teams, divisions, all members, email lists
  - Send via Brevo REST API (free tier: 300 emails/day, covers 240–500/week)
  - Brevo API key stored in AppParameters
  - Send history log
  - Related: Email Lists Panel

---

## Admin / Utilities

- [ ] **Folder Location Configuration** — configurable paths for key directories
  - PDF output folder, document uploads, report exports
  - Stored in AppParameters; browsable via folder picker
  - Used by Documents panel and all PDF export features

- [ ] **Database Backup / Restore** — one-click backup of bocce.db to user-selected location
  - Restore from backup with confirmation
  - Useful before major data operations (import, season clone)

- [ ] **In-App SQL Panel** — SELECT / INSERT / UPDATE / DELETE only (no DDL)
  - For admin troubleshooting and data corrections
  - Build after core data entry is working

- [ ] **Utilities Panel** — home for one-off admin operations:
  - Clear LookingForTeam entries for a season
  - Resequence team letters in a division
  - Recalculate all standings for a season
  - Force-unlock a season (with confirmation)

---

## Website Integration (Future)

- [ ] **Website Push Tool** — push stats from BocceManager to GVBOCCE.com
  - Site on Vercel; DB is PostgreSQL at Neon.tech
  - Push: standings, schedules, documents, news
  - Needs: WebsiteApiUrl and WebsiteApiKey in AppParameters (placeholders exist)

- [ ] **GVBOCCE.com Website** — public-facing website (separate project)
  - Hosted on Vercel; backend at Neon.tech PostgreSQL
  - Ingests content pushed from BocceManager

---

## Accounting / GL (Lower Priority)

- [ ] **GL / Journal Entry UI** — schema already in place (GlAccount, JournalEntry)
  - Simple double-entry view: debit account, credit account, amount, date, memo
  - Accounts: Chequing, Initiation Fees, Season Fees, Event Revenue, Expenses
  - Tie to Fees Panel for automatic journal entries on fee payment

---

## Fixes / Polish

- [ ] **Player Adding Bug** — when adding players to a team that already has players, previously selected players should be greyed out / unavailable in the available list
- [ ] **Leagues Panel Boolean Bug** — FormatException when displaying IsActive column in DataGridView (may be fixed — verify in Leagues panel)

---

## Documentation

- [ ] **Split CLAUDE.md** into focused files:
  - ARCHITECTURE.md — reporting system, design decisions
  - DATABASE.md — table definitions, AppParameters reference
  - DEVELOPMENT.md — common tasks (adding reports, editing, exporting)
  - UI_THEME.md — theme constants, colors, fonts
  - NAVIGATION.md — menu structure and visibility rules
  - CLAUDE.md → high-level overview with links to above

---

## Completed ✓

- [x] Dashboard Panel
- [x] League Panel — CRUD, rules text, parameters
- [x] Season Panel — CRUD, scoring params, playoff settings, divisions tab, slots tab, courts tab
- [x] Division Panel — CRUD, teams tab, players sub-panel
- [x] Team Editor Panel — full CRUD, roster management, captain assignment, LFT integration
- [x] Courts Panel — CRUD, auto-sequencing, delete validation, resequencing
- [x] Players Panel (partial) — IsLocked enforcement, LFT flag management
- [x] Document Manager — upload/link PDF, DOCX, Google Docs
- [x] Parameters Panel — app-level key/value settings
- [x] Theme selection
- [x] Default League & Season selection — Dashboard controls + event system
- [x] Schedule Templates Part 1 — circle-method round-robin + court balancing
- [x] IsLocked Enforcement — DivisionPanel and PlayerPanel
- [x] Schedule Panel — Tab 1 (Templates)
- [x] Navigation — sidebar with groups, indentation, white dot prefix
- [x] Schema — Season.Status, GlAccount, JournalEntry, Finance entities, SeasonCourts
- [x] Bug fix — Leagues panel boolean FormatException
