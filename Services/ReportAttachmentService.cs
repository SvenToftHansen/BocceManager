using System.Drawing.Printing;
using System.Text;
using BocceManager.Data;

namespace BocceManager.Services;

public enum AttachableReport { SpareList, UnpaidFees, AllPlayers, TeamListing }

public record AttachableReportOption(AttachableReport Report, string Name, bool SupportsContent);

// Lets Compose attach (as PDF) or embed (as an HTML table) one of the app's existing
// hand-rolled PrintDocument reports, using each report's own existing data/document builders.
public static class ReportAttachmentService
{
    public static List<AttachableReportOption> GetOptions() =>
    [
        new(AttachableReport.SpareList, "Spare List", true),
        new(AttachableReport.UnpaidFees, "Unpaid Fees", true),
        new(AttachableReport.AllPlayers, "All Players", true),
        new(AttachableReport.TeamListing, "Team Listing", false)
    ];

    public static (string FileName, byte[] Bytes) BuildPdfAttachment(AttachableReport report, int leagueId, int? seasonId)
    {
        using var db = new BocceDbContext();
        var (doc, title) = BuildDocument(db, report, leagueId, seasonId);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{title}_{Guid.NewGuid():N}.pdf");
        if (!PdfRenderService.TryRenderToPdf(doc, tempPath, out var error))
            throw new InvalidOperationException(error ?? "PDF export failed.");

        try
        {
            return ($"{title}.pdf", File.ReadAllBytes(tempPath));
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
        }
    }

    public static string BuildHtmlContent(AttachableReport report, int leagueId, int? seasonId)
    {
        using var db = new BocceDbContext();
        var (headers, rows) = BuildFlatData(db, report, leagueId, seasonId);
        return BuildHtmlTable(headers, rows);
    }

    private static (PrintDocument Doc, string Title) BuildDocument(BocceDbContext db, AttachableReport report, int leagueId, int? seasonId)
    {
        var (clubName, leagueName, seasonName) = GetHeaderContext(db, leagueId, seasonId);

        switch (report)
        {
            case AttachableReport.SpareList:
            {
                var data = SpareListReportService.BuildSpareListData(leagueId);
                return (SpareListReportService.BuildDocument(clubName, leagueName, seasonName, data), "SpareList");
            }
            case AttachableReport.UnpaidFees:
            {
                var docHeader = $"{clubName}  –  {leagueName}  –  {seasonName} - Unpaid Fees";
                var sections = UnpaidFeesReportService.BuildSections(seasonId);
                return (UnpaidFeesReportService.BuildDocument(docHeader, sections), "UnpaidFees");
            }
            case AttachableReport.AllPlayers:
            {
                var filter = new AllPlayersReportService.ReportFilter(false, false, false, false, false, false, false, false, false);
                var data = AllPlayersReportService.BuildData(leagueId, seasonId, filter);
                var docHeader = $"{clubName} – {leagueName} – {seasonName} - ALL PLAYERS";
                return (AllPlayersReportService.BuildDocument(docHeader, filter.Describe(), data), "AllPlayers");
            }
            case AttachableReport.TeamListing:
            {
                if (!seasonId.HasValue) throw new InvalidOperationException("Select a default season before attaching the Team Listing report.");
                var sections = TeamsPrintService.BuildSections(seasonId.Value);
                return (TeamsPrintService.BuildDocument(sections), "TeamListing");
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(report));
        }
    }

    private static (string[] Headers, List<string[]> Rows) BuildFlatData(BocceDbContext db, AttachableReport report, int leagueId, int? seasonId)
    {
        switch (report)
        {
            case AttachableReport.SpareList:
            {
                var data = SpareListReportService.BuildSpareListData(leagueId);
                var headers = new[] { "Name", "Phone", "Email", "Lot Number", "Notes" };
                var rows = data.Select(r => new[] { r.Name, r.Phone, r.Email, r.LotNumber, r.Notes }).ToList();
                return (headers, rows);
            }
            case AttachableReport.UnpaidFees:
            {
                var sections = UnpaidFeesReportService.BuildSections(seasonId);
                return UnpaidFeesReportService.BuildFlatExportData(sections);
            }
            case AttachableReport.AllPlayers:
            {
                var filter = new AllPlayersReportService.ReportFilter(false, false, false, false, false, false, false, false, false);
                var data = AllPlayersReportService.BuildData(leagueId, seasonId, filter);
                return AllPlayersReportService.BuildFlatExportData(data);
            }
            default:
                throw new InvalidOperationException($"{report} does not support inline content.");
        }
    }

    private static (string ClubName, string LeagueName, string SeasonName) GetHeaderContext(BocceDbContext db, int leagueId, int? seasonId)
    {
        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        var leagueName = db.Leagues.Where(l => l.Id == leagueId).Select(l => l.Name).FirstOrDefault() ?? "";
        var seasonName = seasonId.HasValue
            ? db.Seasons.Where(s => s.Id == seasonId.Value).Select(s => s.Name).FirstOrDefault() ?? "N/A"
            : "N/A";
        return (clubName, leagueName, seasonName);
    }

    private static string BuildHtmlTable(string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<table style=\"border-collapse:collapse;font-family:Segoe UI,sans-serif;font-size:13px;\">");
        sb.Append("<tr>");
        foreach (var h in headers)
            sb.Append("<th style=\"border:1px solid #999;padding:4px 8px;background:#f3f4f6;text-align:left;\">")
              .Append(System.Net.WebUtility.HtmlEncode(h)).Append("</th>");
        sb.Append("</tr>");

        foreach (var row in rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row)
                sb.Append("<td style=\"border:1px solid #ccc;padding:4px 8px;\">")
                  .Append(System.Net.WebUtility.HtmlEncode(cell)).Append("</td>");
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
}
