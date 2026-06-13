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

## Data Import

- [x] Import prior-season players + team history from local PostgreSQL DB into SQLite
