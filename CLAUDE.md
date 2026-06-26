# BocceManager Architecture & Development Guide

## Project Overview
- **Framework**: .NET 9.0-windows (WinForms)
- **Database**: PostgreSQL (production), SQLite (testing)
- **Purpose**: Desktop admin and score-entry app for bocce league management

## Working Habits

### Git Safety Checkpoint
**Rule**: Before starting any multi-file change or significant task, always `git commit` the current clean state first.

**Why**: If API credits run out mid-task, the request fails immediately with no warning. Any partially-edited files are left in a broken state with no automatic rollback. A checkpoint commit ensures there is always a known-good state to `git reset --hard` back to.

**How to apply**:
- At the start of each session, if there are uncommitted changes that build cleanly, commit them before starting new work.
- After each independently working logical chunk (e.g. a feature or panel), commit before moving to the next.
- Use descriptive commit messages so it's clear what was completed vs what was interrupted.

---

## Architectural Decisions

### Reporting System (Updated 2026-06-13)

**Decision**: Use **FastReport.OpenSource** instead of Microsoft ReportViewer for report generation and editing.

**Rationale**:
- Microsoft ReportViewer has compatibility issues with .NET 9 (BinaryFormatter was removed in .NET 9 for security reasons)
- ReportViewer is designed for .NET Framework and older .NET Core versions
- FastReport.OpenSource is already included in the project and fully supports .NET 9
- FastReport has a visual designer allowing WYSIWYG report editing
- FastReport supports multiple export formats (PDF, Excel, etc.)

**Implementation**:
- Reports are defined in FastReport format (not RDLC)
- Users can edit reports visually using FastReport Designer
- ReportViewerPanel uses FastReport's report viewer control
- Load reports with: `var report = new Report(); report.Load("path/to/report.frx");`
- More details: See ReportViewerPanel.cs and ReportService.cs

**Related Files**:
- `Services/ReportService.cs` - Report metadata and app parameters
- `Panels/ReportViewerPanel.cs` - Unified report viewer UI
- `Reports/` - FastReport definition files (.frx)

---

### Print Preview System (Updated 2026-06-23)

**Decision**: Use **PrintPreviewService** for all print preview dialogs, replacing duplicate implementations.

**Rationale**:
- Eliminates duplicate toolbar and printer logic across multiple services
- Provides consistent UI/UX for all reports and printouts
- Unified export support (PDF, Excel, CSV) available to all reports
- Easier to maintain and extend print functionality

**Implementation**:
- `Services/PrintPreviewService.cs` - Single entry point for all print previews
- Provides toolbar with Print, PDF, Excel, CSV, Web (placeholder), and Prev/Next navigation
- Supports optional export via `string[]` headers and `List<string[]>` rows
- All services delegate their `ShowPrintPreview()` calls to this unified service

**Using Print Preview**:
```csharp
// Create PrintDocument with content
var doc = new PrintDocument { DocumentName = "My Report" };
doc.PrintPage += (_, e) => {
    // Draw content to e.Graphics
};

// Show preview with optional export support
var headers = new[] { "Col1", "Col2", "Col3" };
var rows = new List<string[]> { /* data */ };
PrintPreviewService.ShowPrintPreview(this, doc, headers, rows);

// Without export:
PrintPreviewService.ShowPrintPreview(this, doc);
```

**Related Files**:
- `Services/PrintPreviewService.cs` - Unified print preview implementation
- `Services/TeamsPrintService.cs` - Delegates to PrintPreviewService
- `Services/SchedulePrintService.cs` - Delegates to PrintPreviewService
- `Services/SpareListReportService.cs` - Delegates to PrintPreviewService
- `Services/ReportExportService.cs` - Handles Excel/CSV export

---

### Logging (Added 2026-06-24)

**Decision**: Use **Serilog** with a rolling file sink for all application logging.

**Implementation**:
- Configured in `Program.cs` at startup — no other setup needed
- Log files written to `%AppData%\BocceManager\logs\bocce-YYYYMMDD.log`
- 30-day retention, rolls over daily
- `Services/AppLogger.cs` — thin static wrapper; use this everywhere instead of calling Serilog directly

**Usage**:
```csharp
AppLogger.Info("Season {SeasonId} loaded", season.Id);
AppLogger.Warn("No divisions found for season {SeasonId}", id);
AppLogger.Error(ex, "Failed to save score for game {GameId}", gameId);
```

**Related Files**:
- `Services/AppLogger.cs` - Static wrapper (Info, Warn, Error, Debug)
- `Program.cs` - Logger configuration and initialization

---

### Excel Export (Updated 2026-06-24)

**Decision**: Use **ClosedXML** for all Excel exports — produces real `.xlsx` files.

**Rationale**: The previous implementation wrote CSV content with a `.csv` extension (not a real spreadsheet). ClosedXML produces properly formatted `.xlsx` files with styled headers, auto-fitted columns, and frozen header rows.

**Implementation**:
- `ReportExportService.ExportToExcel()` now writes real XLSX via ClosedXML
- Headers get bold white text on a blue background
- Columns auto-fit to content; header row is frozen

**Usage** (no change to callers — same signature):
```csharp
ReportExportService.ExportToExcel(this, "filename", headers, rows);
```

**Related Files**:
- `Services/ReportExportService.cs` - ExportToExcel (ClosedXML) and ExportToCsv (StreamWriter)

---

### HTTP Resilience (Added 2026-06-24)

**Decision**: Use **Polly** for retry logic on all external HTTP calls (Brevo email API, website API).

**Implementation**:
- `Services/PollyPolicies.cs` — shared pipeline definitions; add new policies here as needed
- `PollyPolicies.HttpRetry` — 3 retries, exponential back-off (2s → 4s → 8s), logs each retry via AppLogger

**Usage**:
```csharp
await PollyPolicies.HttpRetry.ExecuteAsync(async ct =>
{
    var response = await httpClient.PostAsync(url, content, ct);
    response.EnsureSuccessStatusCode();
}, cancellationToken);
```

**Related Files**:
- `Services/PollyPolicies.cs` - Shared Polly resilience pipelines

---

## Database

### AppParameters Table
Stores application-wide configuration values as key-value pairs:
- `ReportPdfLocation` - Default folder for PDF exports
- `WebsiteApiUrl` - URL for web report upload (placeholder)
- `WebsiteApiKey` - API key for website integration (placeholder)

Query via: `ReportService.GetAppParameter(db, "key")`

### Reports & ReportParameters Tables
Metadata-driven report discovery:
- **Reports** - Available reports (Name, ReportPath, Description, DisplayOrder, IsActive)
- **ReportParameters** - Parameters each report accepts (ReportId, ParameterName, DefaultSource)

Dynamic report loading allows adding new reports without code changes.

---

## Common Development Tasks

### Adding a New Report
1. Create `.frx` file in `/Reports` folder using FastReport Designer
2. Add row to `Reports` table via migration or directly in PostgreSQL:
   ```sql
   INSERT INTO "Reports" ("Name", "ReportPath", "Description", "DisplayOrder", "IsActive", "CreatedDate", "ModifiedDate")
   VALUES ('Report Name', 'Reports/ReportName.frx', 'Description', 3, true, NOW(), NOW());
   ```
3. If report needs parameters, add rows to `ReportParameters` table
4. Rebuild and run app

### Editing an Existing Report
1. Open `/Reports/ReportName.frx` with FastReport Designer
2. Edit layout, add/remove fields, change formatting
3. Save the file
4. Rebuild the app (RDLC-like editing, but FastReport format)

### Exporting Report to PDF
- Button click calls `ReportService.GetDefaultReportPdfLocation(db)`
- Uses configured save location (AppParameter: "ReportPdfLocation")
- Default: `Environment.SpecialFolder.MyDocuments`

---

## UI/Theme Constants

See `UI/Theme/AppTheme.cs` for available colors and fonts:
- Navigation: `NavBackground`, `NavText`, `NavSelected`, `NavHover`
- Content: `ContentBackground`, `Surface`, `Separator`
- Text: `TextPrimary`, `TextSecondary`, `TextMuted`
- Feedback: `ButtonSuccess`, `ButtonDanger`
- Fonts: `FontDefault`, `FontDefaultBold`, `FontSmall`, etc.

---

## Navigation Structure

Main menu (sidebar) organized in groups:
- **WORKSPACE**: Dashboard, Leagues, Seasons, Divisions
- **ROSTER**: Players, Teams
- **OPERATIONS**: Score Entry, Schedule
- **REPORTS**: Standings, Playoffs, Reports (unified viewer)
- **ADMINISTRATION**: Spare Lists, Announcements, Fees, Email, Documents, Parameters, Courts, Utilities, Theme

Navigation is rebuilt when app starts; visibility depends on whether a default season is selected.

---

## Testing

- Test database: SQLite at path set in `BocceDbContext.DbPath`
- Production database: PostgreSQL at `localhost:5432/bocce_league`
- Migrations applied via: `dotnet ef database update`
- Seed data: `DatabaseInitializer.SeedReferenceData()`

---

## References
- **Master backlog**: `TODOLIST.md` in project root — all pending features, bugs, and tasks. At session start, pick 3–5 items and load into TodoWrite. Update TODOLIST.md when items are completed.
- Memory system: `/memory/` directory (auto-memory, persists across sessions)
- Database migrations: `/Data/Migrations/`
- Print preview: Use `PrintPreviewService.ShowPrintPreview()` for all new and existing printouts
