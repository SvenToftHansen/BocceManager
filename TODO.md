# BocceManager TODO

## Panels Not Yet Implemented (Placeholders)

- [ ] **Score Entry** — panel not started
- [ ] **Standings** — panel not started
- [ ] **Playoffs** — panel not started
- [ ] **Spare Lists** — panel not started
- [ ] **Announcements** — panel not started
- [ ] **Fees** — panel not started
- [ ] **Email Lists** — panel not started (Brevo API integration planned)

## Schedule

- [ ] **Division Schedules tab** — "coming soon" stub inside SchedulePanel; Tab 1 (Templates) is done
  - Note: Schedule Editor is for viewing only; don't need dynamic list updates
- [ ] **Schedule Templates Part 2** — Part 1 (circle-method round-robin + court balancing) committed; Part 2 not started

## Teams / Players

- [ ] Fix Player Adding — when adding players to a team that already has players, show previously selected players and make them unavailable in the available players list

## Courts

- [ ] Improve Courts Editor — when adding a new court, auto-default court number and letter to next sequential values
- [ ] Add court selection to match/schedule creation screens
- [ ] Validate court assignments when building schedules
- [ ] Display assigned courts on schedule/match reports

## IsLocked Enforcement

Currently enforced in DivisionPanel and PlayerPanel only.

- [ ] ScoreEntry panel — block score entry when season is locked
- [ ] Schedule panel — block schedule creation/modification when season is locked
- [ ] Standings/Playoffs panels — lock down when season is completed

## Testing (IsLocked)

- [ ] Attempt to add division when season locked → should be blocked with message
- [ ] Attempt to add team when season locked → should be blocked with message
- [ ] Attempt to assign player when season locked → should be blocked with message
- [ ] Attempt to mark Looking for Team in locked season → error should be shown
- [ ] Create new player when season locked → should succeed (not assigned to team)

## Folder Locations / File Paths

- [ ] Add a tool to configure folder locations for key paths — e.g. rules documents, document uploads, PDF output directory
  - Should be stored in app settings/parameters (not hardcoded)
  - Allow user to browse/select folders per category
  - Used by Documents panel uploads and any PDF export features

## Export Tool

- [ ] Add export functionality for key data sets (players, teams, etc.)
  - Target formats: Excel (.xlsx) and/or Google Sheets
  - Candidates: Players list, Teams roster, Standings, Schedule, Fees

## Document Creator

- [ ] Build a tool to combine multiple reports into a single PDF for distribution
  - User selects which reports to include (e.g. Schedule, Standings, Fees, etc.)
  - Generates all reports and merges them into one PDF
  - Output to configured PDF folder

## Season Creation

- [ ] **Clone Season wizard** — when creating a new season, offer option to copy structure from a previous Locked or Completed season
  - Dropdown to select the source season (filtered to Locked/Completed only)
  - Checkboxes/option buttons for what to copy:
    - [ ] Teams (roster structure, not scores)
    - [ ] Parameters (division settings, court assignments, schedule config, etc.)
  - Copying is optional — user can create a blank season if preferred

## Data Import

- [x] Import prior-season players + team history from local PostgreSQL DB into SQLite

## Documentation Refactoring

Organize CLAUDE.md by splitting into focused, modular documentation files:

- [ ] **ARCHITECTURE.md** — Reporting system, database schema, design decisions
- [ ] **DATABASE.md** — Detailed table definitions, AppParameters reference
- [ ] **DEVELOPMENT.md** — Common development tasks (adding reports, editing reports, exporting, etc.)
- [ ] **UI_THEME.md** — Theme constants, available colors/fonts
- [ ] **NAVIGATION.md** — Menu structure and visibility rules
- [ ] **CLAUDE.md** — Reduce to high-level overview with links to other docs
