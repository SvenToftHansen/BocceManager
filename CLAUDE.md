# BocceManager Architecture & Development Guide

## Project Overview
- **Framework**: .NET 9.0-windows (WinForms)
- **Database**: PostgreSQL (production), SQLite (testing)
- **Purpose**: Desktop admin and score-entry app for bocce league management

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
- Print services (legacy): `Services/TeamsPrintService.cs`, `Services/SchedulePrintService.cs`
