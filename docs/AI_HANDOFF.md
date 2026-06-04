# AI Handoff (Claude <-> Copilot)

Purpose: keep both assistants aligned on business rules, implementation approach, and current state.

Fast workflow:
1. At start of session: read this file top to bottom.
2. During session: follow Non-Negotiables and Decisions.
3. At end of session: update "Latest Handoff" and move previous one to "History".

When switching assistants, paste the "Copy/Paste Handoff Block" at the bottom.

---

## Latest Handoff

**Date:** 2026-06-04
**Owner:** Sven Hansen (svenhansen@shaw.ca)
**Assistant:** Claude (Haiku 4.5 → Sonnet 4.6 this session)
**Branch:** master

---

### 1) Goal Now

BocceManager is a desktop admin/score-entry app for Golden Vista Bocce League.
Immediate priorities for next session:
- Test the newly-fixed buttons (Delete League, Delete Season, Delete Division)
- Test the Copy Season flow (now copies divisions + teams + players + LFT)
- Verify the Utilities → Backup/Restore works end-to-end
- Continue building out remaining placeholder panels (Score Entry, Schedule, Standings, etc.)

---

### 2) Business Rules (Non-Negotiable)

- **Stack:** .NET 9 WinForms, PostgreSQL (Npgsql 9.0.1), Entity Framework Core 9
- **Database:** PostgreSQL local instance — `bocce_league` on localhost:5432, user=postgres, pass=7720
- **Connection string lives in:** `Data/BocceDbContext.cs` line ~9
- **No SQLite** — migration to PostgreSQL is complete; do not re-introduce SQLite
- **Schema owned by EF Core migrations** — never call `ApplySchemaPatches()` on PostgreSQL; that was SQLite-only legacy code
- **All DateTime fields use `DateTime.UtcNow`** — PostgreSQL timestamp with time zone requires UTC
- **Player data source:** 239 players imported from `dump_inserts.sql` (PostgreSQL backup); original file lives at `C:\Users\svenh\Documents\BocceDocs\dump_inserts.sql`
- **Backups stored in:** `{app exe dir}\Backups\` — plain SQL format via pg_dump
- **pg_dump / psql found via:** `Services/BackupService.FindPostgresqlBin()` — scans `C:\Program Files\PostgreSQL\{version}\bin\`
- **Navigation is locked during restore** — `MainForm.LockNavigation()` / `UnlockNavigation()` disables only the nav sidebar, not the content panel
- **Restore must clear Npgsql connection pool first** — `NpgsqlConnection.ClearAllPools()` before terminating backend connections; otherwise EF Core reconnects immediately and blocks the DROP

---

### 3) Decisions Already Made

**Chosen:**
- PostgreSQL over SQLite — single source of truth for both desktop app and future website
- EF Core migrations (`Database.Migrate()`) for schema — not manual SQL patches
- `ImportPostgresData.cs` parses `dump_inserts.sql` to seed players — uses `SplitSqlFields()` for SQL-escaped quotes (O''Connor style)
- Restore uses `--single-transaction` flag on psql for speed
- Restore runs on `Task.Run` (background thread) so UI stays responsive
- Nav sidebar locked (not whole form) during restore so log panel stays readable
- `Application.Restart()` called automatically after successful restore
- Dark-themed `RichTextBox` log panel in Utilities with timestamps and elapsed times
- Copy Season copies: divisions + teams + TeamPlayers + LookingForTeam TeamId updates
- `SetEditModeUI()` must set both `.Visible` AND `.Enabled` on Delete buttons

**Rejected:**
- SQLite for production (too fragile for multi-client / website sharing)
- `ApplySchemaPatches()` on PostgreSQL startup (causes "column already exists" after restore)
- `mainForm.Enabled = false` during restore (whites out log panel — unreadable)
- Hardcoded player list in importer (missed ~40 players with apostrophes in names)
- Regex-based SQL field splitting (broke on `O''Connor` — replaced with char-by-char `SplitSqlFields()`)

---

### 4) Current State

**Done this session:**
- Fixed app not loading (Form1.cs → MainForm.cs rename)
- Fixed league Save button not appearing for new leagues
- Migrated database from SQLite to PostgreSQL (Npgsql 9.0.1, .NET 9)
- Created EF Core initial migration (`InitialPostgresSchema`)
- Imported 239 players + 20 LookingForTeam + 54 SpareLists from PostgreSQL dump
- Fixed `DateTime.Now` → `DateTime.UtcNow` for document uploads
- Built Utilities panel with backup/restore/preview (UTILITIES nav group)
- Fixed restore: DROP DATABASE transaction error → separate psql calls
- Fixed restore speed: `--single-transaction` + `NpgsqlConnection.ClearAllPools()`
- Fixed restore blocking: connection checker loop logs who is holding the DB open
- Fixed restore UI: dark log panel with timestamps, nav lock, auto-restart
- Fixed "column already exists" startup error after restore (removed `ApplySchemaPatches` from startup)
- Fixed Delete Season button (never enabled + missing FK child cleanup)
- Fixed Delete League button (never enabled)
- Fixed Delete Division button (never enabled + missing FK child cleanup)
- Fixed Copy Season missing play days and play times
- Expanded Copy Season to copy teams, players, and LookingForTeam entries

**In progress / needs verification:**
- Restore live table progress (uses `--echo-queries` + async stdout read — not yet confirmed working)
- Restore speed (ClearAllPools fix applied — needs real-world test with pgAdmin closed)

**Blocked / known issues:**
- BocceManager.Tests project broken (references removed `BocceDbContext.DbPath` — SQLite leftover; tests need rewriting for PostgreSQL)
- Many panels still show PlaceholderPanel: Score Entry, Schedule, Standings, Playoffs, Spare Lists, Announcements, Fees, Email Lists

---

### 5) Key Files

| File | Purpose |
|------|---------|
| `Data/BocceDbContext.cs` | DB context — PostgreSQL connection string here |
| `Data/DatabaseInitializer.cs` | Startup schema init — calls `Migrate()` then `SeedReferenceData()` only |
| `Data/Migrations/` | EF Core migrations — do not edit manually |
| `Data/ImportPostgresData.cs` | One-shot player import from dump_inserts.sql |
| `Services/BackupService.cs` | pg_dump backup, psql restore, PreviewBackup, connection checker |
| `Panels/UtilitiesPanel.cs` | Backup/Restore UI with dark log panel |
| `Panels/SeasonPanel.cs` | Season CRUD + CopySeasonData() for full season clone |
| `Panels/LeaguePanel.cs` | League CRUD |
| `Panels/DivisionPanel.cs` | Division CRUD + team/player management |
| `MainForm.cs` | Navigation, LockNavigation() / UnlockNavigation() |

**Files that must not change without care:**
- `Data/Migrations/` — edit only via `dotnet ef migrations add`
- `Data/BocceDbContext.cs` — connection string is live production credentials

---

### 6) Validation

**Build:** `dotnet build BocceManager.csproj` — passes clean (warnings only, no errors)
**Tests:** BocceManager.Tests is broken (SQLite leftover) — ignore for now
**Manual checks still needed:**
- [ ] Delete League with seasons/divisions/teams — confirm no FK error
- [ ] Delete Season — confirm no FK error
- [ ] Delete Division with schedule/standings — confirm no FK error
- [ ] Copy Season — confirm divisions + teams + players + LFT all appear in new season
- [ ] Backup → Restore → app restarts and shows correct data
- [ ] Restore with pgAdmin open — confirm log shows "Database still open: pgAdmin 4(idle)"
- [ ] Document upload (was fixed for UTC timestamp)

---

### 7) Risks / Unknowns

- **Restore with pgAdmin open** will still block on DROP DATABASE even after ClearAllPools — user must close pgAdmin first. The log now clearly shows which app is holding the connection.
- **pg_dump path** found by scanning `C:\Program Files\PostgreSQL\{version}\bin\` — if PostgreSQL is installed elsewhere this will fail with a clear error message.
- **BocceManager.Tests** references `BocceDbContext.DbPath` which no longer exists — tests will not compile until rewritten for PostgreSQL (not urgent).
- **TeamsInDivision counter** on Division is not auto-updated when teams are copied — may show 0 after CopySeasonData. Should be recalculated after copy.
- **LookingForTeam has no SeasonId** — it is league-scoped. Unplaced players carry forward automatically. Placed players have their TeamId updated to the new team. This is the current schema; adding SeasonId would be a migration.

---

### 8) Next Actions (Ordered)

1. Test all three Delete buttons with real data — confirm no FK constraint errors
2. Test Copy Season end-to-end — verify teams and players appear in new season
3. Test Backup → Restore → auto-restart cycle with pgAdmin closed
4. Fix `TeamsInDivision` counter on Division after CopySeasonData
5. Implement Score Entry panel (next major feature)
6. Implement Schedule panel
7. Implement Standings panel
8. Fix BocceManager.Tests for PostgreSQL (low priority)

---

## Copy/Paste Handoff Block

Paste this when switching assistants:

```
Handing off BocceManager desktop app (Golden Vista Bocce League).
Stack: .NET 9 WinForms, PostgreSQL localhost:5432/bocce_league (postgres/7720), EF Core 9, Npgsql 9.0.1.
Repo: C:\Users\svenh\Documents\BocceDocs\BocceManager, branch: master

Key rules:
- No SQLite. PostgreSQL only.
- Schema owned by EF Core migrations in Data/Migrations/ — never call ApplySchemaPatches() on startup.
- All DateTime → DateTime.UtcNow (PostgreSQL timestamptz).
- During restore: NpgsqlConnection.ClearAllPools() before DROP DATABASE, run on Task.Run background thread, lock nav sidebar only (not whole form).
- SetEditModeUI() must set both .Visible AND .Enabled on delete buttons.

Current state: see docs/AI_HANDOFF.md → Latest Handoff → Section 4.
Next actions: see docs/AI_HANDOFF.md → Latest Handoff → Section 8.
Build: dotnet build BocceManager.csproj — clean.
```

---

## History

### Handoff 2026-06-04 (session start — blank template)
- Template created by CoPilot, not yet filled.
