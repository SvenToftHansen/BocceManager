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

### Reporting System (Updated 2026-08-03)

**Decision**: Reports are hand-rolled `System.Drawing.Printing.PrintDocument` GDI+ drawing code, not a report-designer framework.

**Background**: An earlier plan to adopt FastReport.OpenSource (in place of Microsoft ReportViewer, which doesn't work on .NET 9) was never actually implemented — the package reference, a `Reports`/`ReportParameters` metadata schema, and a `ReportService.cs` were added, but no code ever loaded a `.frx` file or rendered through FastReport. That dead scaffolding (package reference, entities, service, orphaned `.rdlc` files) has been removed to stop it from implying report generation works a way it doesn't.

**How reporting actually works**:
- Each report has a dedicated service (e.g. `SpareListReportService`, `UnpaidFeesReportService`, `AllPlayersReportService`, `TeamsPrintService`, `SchedulePrintService`, `StandingsPrintService`) that builds a `PrintDocument` via GDI+ (`Graphics.DrawString`/`DrawRectangle`/etc.) from data pulled straight out of the DB — no template files.
- Most of these services also expose flat `(string[] Headers, List<string[]> Rows)` export data for Excel/CSV.
- The resulting `PrintDocument` is shown via `PrintPreviewService.ShowPrintPreview(...)`, which provides the shared preview window, Print/PDF/Excel/CSV toolbar.
- PDF export drives the OS's "Microsoft Print to PDF" virtual printer (`Services/PdfRenderService.cs` for headless use, e.g. email attachments; `PrintPreviewService.SendToPdf` for the interactive "Save as PDF" button).

**Adding a new report**: write a new `Services/<Name>ReportService.cs` following the existing pattern (data builder → `PrintDocument` builder → optional flat export), and a panel that calls `PrintPreviewService.ShowPrintPreview`. No DB metadata rows or template files needed.

**Related Files**:
- `Services/PrintPreviewService.cs` - Shared preview/print/export UI
- `Services/PdfRenderService.cs` - Headless PrintDocument → PDF file
- `Services/*ReportService.cs`, `Services/*PrintService.cs` - Per-report data + document builders

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
- `DocumentsFolder` - Default folder the "Save as PDF" dialog opens to for all reports
- `ReportPdfLocation` - Legacy/unused; superseded by `DocumentsFolder` for PDF export defaulting
- `WebsiteApiUrl` - URL for web report upload (placeholder)
- `WebsiteApiKey` - API key for website integration (placeholder)

Query via: `AppParameterService.GetAppParameter(db, "key")`

---

## Common Development Tasks

### Adding a New Report
1. Write `Services/<Name>ReportService.cs` following the existing pattern: a data builder (DB → DTOs), a `BuildDocument(...)` that draws the `PrintDocument` via GDI+, and optionally a flat `(headers, rows)` export for Excel/CSV.
2. Add a panel (or a button on an existing panel) that builds the document and calls `PrintPreviewService.ShowPrintPreview(this, doc, headers, rows)`.
3. No DB rows or template files needed — reports aren't metadata-driven.

### Exporting Report to PDF
- Interactive: the Print Preview toolbar's "Save as PDF" button (`PrintPreviewService.SendToPdf`) drives the OS's "Microsoft Print to PDF" virtual printer.
- If the `DocumentsFolder` AppParameter (Parameters panel's "Documents Folder" field) is set and exists, it saves straight there with no dialog — filename is `<ReportName>_<MMM_dd_yyyy>.pdf` (e.g. `SpareList_Jan_01_2026.pdf`), overwriting silently if a file with the same name already exists.
- If `DocumentsFolder` isn't configured, it falls back to a `SaveFileDialog` (initial folder `Environment.SpecialFolder.MyDocuments`) with the same dated filename pre-filled.
- Headless (e.g. for email attachments): `Services/PdfRenderService.TryRenderToPdf(doc, outputPath, out error)` does the same rendering without a dialog.

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
