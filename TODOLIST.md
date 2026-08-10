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
  - Consider replacing week-range NumericUpDown spinners with plain TextBox fields (~20 lines, see StandingsPanel.cs:106–142)

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

- [x] **Players Panel** — Full CRUD editor for player records *(reworked 2026-07-04, see Fixes / Polish)*
  - Fields: first/last name, email, phone, lot#, active flag, role, spouse/partner
  - Manual Save/Clear/Delete workflow with filters (Season/Division/Team/Team Status/Spare/Looking For Team)

- [x] **Team Applicants Panel** — Group applicant registration and placement *(built 2026-06-24)*
  - LEAGUE nav group, below Teams
  - Left: list filtered by Pending/All; Right: Group tab + Members tab + LFT Pool tab
  - Group tab: GroupName, email, phone, preferred division, notes
  - Members tab: add existing players (picker) or new people (name/email/phone dialog); remove
  - LFT Pool tab: all unplaced LFT players for season; Assign to Team button
  - "Place Group..." button: confirm dialog → creates team, players, TeamPlayer records in one transaction
  - DivisionPanel: "Place Applicant..." button on teams toolbar; shows picker filtered to prefer current division

- [x] **Looking For Team Panel** — Standalone panel in ROSTER nav group *(built 2026-06-25)*
  - Add player with filter (all / exclude team members / exclude spare list members)
  - Edit preferred division, registered date, notes per entry
  - Assign to team picker with player count display ("A - Hansen (4)") and room filters
  - LFT Pool tab removed from Team Applicants panel; Players/Teams/Team Applicants/LFT moved to ROSTER nav group

- [x] **Looking For Team Panel — Enhancements** *(done 2026-06-26)*
  - [x] Multiple selection on "Add Player to LFT" — select multiple players, dialog offers "Add as Group" or "Add as Solos"
  - [x] Show all group members including selected player (marked with ◆)
  - [x] Auto-name groups by creator's last name (e.g., "Hansen's Group")
  - [x] No default selection on panel load; detail panel empty until clicked
  - [x] Show team day/time on preferred team display and team picker (MON-0900 format)
  - [ ] Create new team option in "Assign to Team" dialog — if selected division has room, allow creating new team
  - [ ] Add Member button improvement — investigate intermittent failure (appears to work reliably now)

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

## Reporting

Reports are hand-rolled `PrintDocument` GDI+ services (see CLAUDE.md's "Reporting System"), not a report-designer framework — an earlier FastReport.OpenSource plan was scrapped since it was never actually implemented (dead package reference, unused DB tables) while the schedule/teams/standings reports below were built directly.

- [x] **Schedule Report** — `SchedulePrintService` / `ReportSchedulePanel`
- [x] **Teams Report** — `TeamsPrintService` / `ReportTeamListingPanel`
- [x] **Standings Report** — `StandingsPrintService` / `StandingsPanel`
- [ ] **Document Creator** — combine multiple reports into one PDF for distribution
  - User selects which reports to include
  - Merge each report's PDF (via `PdfRenderService`) into one file, output to the configured PDF folder

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

- [x] **In-App Email Client + Gmail Integration** — full email compose panel
  - Recipient picker: all active players, players on teams, division, team, players on spare list, unpaid players, email lists, individual(s) (multi-select)
  - Send via Gmail API (OAuth2 desktop flow); personal Gmail accounts cap at ~500/day, comfortably covers 240–500/week
  - Gmail Client ID/Secret and refresh token stored DPAPI-encrypted in AppParameters (Parameters panel "Gmail Integration" section)
  - Per-recipient send history log (EmailLog/EmailLogRecipient)
  - Insert Report... — inserts a themed HTML version (colors/headers mirroring the print report) of Spare List, Unpaid Fees, All Players, Team Listing, Schedule, Standings, Season Seed, or Playoff Schedule at the cursor, via `ReportAttachmentService`/`ReportHtmlService`. Content-only, no PDF-attach path.
  - Attach Document... — attach any PDF/Word file from the Documents folder (`ClubDocuments`) to the email
  - Clear Content — wipes the body editor back to empty
  - Manual Email Address(es) scope — type semicolon/comma-separated addresses directly (not tied to a Player row) for one-off recipients not in the roster
  - Preview — renders subject/body (merged against a sample or real recipient) before sending
  - Replies tab — lists the connected inbox (`GmailReadService`), matches senders against Players by email, shows the message body on selection (auto-marks read), and can Archive. Uses the `gmail.modify` OAuth scope (superset of send/read/label-write; replaced the old `gmail.send`-only scope, which was also too narrow for the "Connected as ..." profile lookup)
  - "Unique email addresses only" checkbox (default on) next to the recipient count — toggles `EmailRecipientService`'s email dedup on/off per send, rather than always forcing it
  - Related: Email Lists Panel

- [x] **Automated Standings Email per Team** *(built 2026-08-06, updated 2026-08-07)* — email each player their team's division standings + overall season seed report, with a personalized greeting
  - `Services/StandingsEmailService.cs` — one email per active player (switched from one-per-team once a personalized "Hello {FirstName}" greeting was requested — the greeting differs per recipient, so the send has to too). Division/season-seed HTML is still built once per division and reused across all its players' emails, not rebuilt per send
  - Body = greeting ("Hello {FirstName}. Here are the standings for your Division ({DivisionName}) and for your team {TeamName}. Included is the current seeding table for season end tournament." + optional admin-typed extra message paragraph + "Thanks for participating, and we hope you enjoy the league." + "GVBB Executive.") + 2-blank-line spacer + `ReportHtmlService.BuildStandingsHtml(seasonId, divisionId, fromWeek, toWeek)` + 3-blank-line spacer + `ReportHtmlService.BuildSeasonSeedHtml(seasonId, fromWeek, toWeek)`. Week-ending date via `ReportHtmlService.GetStandingsWeekEndDate`
  - Confirm dialog has a multi-line "Additional message" text box (e.g. for upcoming-event announcements) inserted into the greeting; `StandingsEmailService.BuildRoster`/`BuildPreviewEmail`/`BuildPlan` are split so the Preview button can rebuild just one recipient's email live as that text changes, without rebuilding everyone's
  - Respects the Standings panel's current week-range slider — if it's showing "as of Week 1" while 3 weeks are scored, the email (and its "Weekending" date) reflects that same historical cutoff, not always the latest. Both reports pull from the shared `Services/StandingsAggregationService.cs` (extracted from `StandingsPanel`'s week-range aggregation, used by both the on-screen panel and the emailed reports so they never disagree)
  - Recipients per team via `EmailRecipientService.GetRecipients(..., EmailRecipientScope.Team, ...)`; teams with no active/emailable players are skipped and reported back, not silently dropped. `EmailRecipientService` optionally dedupes by (email, team name) — not email alone, since the same person can legitimately be on more than one team and needs a separate, team-specific send for each; a plain-email key would have wrongly merged those. Toggle lives in the confirm dialog as "Unique email addresses only (per team)" (default on) and rebuilds the roster/preview list live when switched
  - When dedup merges two people sharing one email/team (e.g. spouses), `CombineFirstNames` joins their first names ("John & Jane") into the single surviving `EmailRecipient.FirstName` instead of silently dropping one person's name — applies to the `{FirstName}` merge field everywhere `EmailRecipientService` is used (Compose and Standings), not just the Standings greeting
  - Triggered from Standings panel's "Email Standings..." button (now under the REPORTS nav group, moved from LEAGUE); logs to EmailLog/EmailLogRecipient like Compose sends (test sends are not logged)
  - Confirm dialog includes a per-player Preview (WebView2, exact subject/body), the current week range, and a Test Mode checkbox + text box — test mode sends exactly one email (the first recipient's content) to whatever address is typed in (defaults to the Parameters "League Captain Email"), instead of fanning out to everyone
  - Not yet done: trigger from the Email panel too (currently Standings panel only)

- [ ] **Daily Send Count Indicator in Compose** — show today's Gmail send count against the ~500/day account limit
  - Derive from EmailLogRecipient rows (Status = "Sent", SentAt = today) — no external API call needed
  - Helps the user judge whether a big blast should be split across two days
  - Add as a small label near the Send button in Compose

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

- [x] **Show Team Count in Brackets** *(done 2026-06-26, extended 2026-07-04)* — Add team count display across the app
  - [x] SeasonPanel division list: "Division A (12)"
  - [x] Division Lookup list (DivisionPanel) — "Division A (12 teams)"
  - [ ] Other panels (LeaguePanel, etc.) — future enhancement
  - Uses Division.TeamCount for efficiency

- [ ] **Player Adding Bug** — when adding players to a team that already has players, previously selected players should be greyed out / unavailable in the available list
- [ ] **Leagues Panel Boolean Bug** — FormatException when displaying IsActive column in DataGridView (may be fixed — verify in Leagues panel)

- [x] **Season current-flag toggle bug** *(fixed 2026-07-04)* — a league with only one season could not have its "Current" flag turned off; the editor force-checked it and re-flipped it on at save. Now a league can have zero current seasons; turning one ON still clears the flag on others, but turning OFF no longer hunts for a replacement.

- [x] **Sparelist notes rework** *(done 2026-07-04)* — note shown inline (truncated 40 chars) next to the spare's name; double-click opens a small popup to view/edit the note; removed the now-redundant bottom Notes textbox panel.

- [x] **Players Panel rework** *(done 2026-07-04)* — see Core Panels Not Yet Built entry below, now substantially complete:
  - [x] No default lookup selection on panel load
  - [x] Editor fields hidden until New Player or a lookup row is selected
  - [x] Removed autosave; added explicit Save / Clear / Delete buttons
  - [x] Save, Clear, and Delete all return to the empty (hidden fields, no selection) state
  - [x] Added Season / Division / Team / Team Status (on-off) / Spare List / Looking For Team filters above the lookup, defaulting to All; filters persist across save/clear/delete
  - [x] Removed Created-date field
  - [x] Spouse/Partner picker no longer narrows to the search-filtered subset; always lists full roster sorted by last name then first name

- [ ] **Team Applicants Panel removal — deferred** — user asked to remove the Roster > Team Applicants panel, but DivisionPanel's "Place Applicant Group" button reads pending TeamApplicants records to auto-create teams, so the panel/table/service aren't isolated. Decided to leave everything as-is for now; revisit with a decision on whether to also retire the DivisionPanel placement feature and the underlying table.

- [ ] **Roster — Looking For Team / Looking For Placement tabs** — user flagged both tabs need more planning/thinking before any changes; no concrete requirements yet.

- [ ] **Roster — Teams: new "all players × all teams" tab** — big planned feature, needs design before implementation:
  - Scrollable grid: 5 day columns, each showing team members + open slots
  - Vertical per-team sections (System Name header, Player 1..N lines)
  - Player list with advanced search + No Team/All and Looking-For-Team/All filters
  - Drag-and-drop a player onto a team slot; blank slot = direct move, occupied slot = confirm replacement (show displaced player's name)
  - Add a "Confirmed" field per team (captain has confirmed day/time + roster)
  - User will provide an Excel mockup + query example before implementation starts

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
- [x] Season-level court selection — SeasonCourts now has SortOrder (priority); Season Panel Courts tab manages selection/order/display style; league schedule generation and playoff bracket generation both consult it (playoffs strictly inherit, no separate PlayoffCourt selection). Removed duplicate/inconsistent letter-display settings (global AppParameter + per-league) in favor of one setting on Season.CourtDisplayStyle.
