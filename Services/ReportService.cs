using BocceManager.Data;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public static class ReportService
{
    public static List<Report> GetActiveReports(BocceDbContext db)
        => db.Reports
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ToList();

    public static Report? GetReport(BocceDbContext db, int reportId)
        => db.Reports.FirstOrDefault(r => r.Id == reportId);

    public static List<ReportParameter> GetReportParameters(BocceDbContext db, int reportId)
        => db.ReportParameters
            .Where(p => p.ReportId == reportId)
            .OrderBy(p => p.DisplayOrder)
            .ToList();

    public static void InitializeDefaultReports(BocceDbContext db)
    {
        if (db.Reports.Any()) return;

        var reports = new[]
        {
            new Report
            {
                Name = "Team Listing",
                ReportPath = "Reports/TeamListing.rdlc",
                Description = "Lists all teams and rosters for the current season, organized by division and time slot.",
                DisplayOrder = 1,
                IsActive = true
            },
            new Report
            {
                Name = "Schedules - Generic",
                ReportPath = "Reports/SchedulesGeneric.rdlc",
                Description = "Prints all schedule templates for the current season, showing weekly court assignments.",
                DisplayOrder = 2,
                IsActive = true
            }
        };

        foreach (var report in reports)
            db.Reports.Add(report);

        db.SaveChanges();
    }

    public static string? GetAppParameter(BocceDbContext db, string key)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == key);
        return param?.Value;
    }

    public static void SetAppParameter(BocceDbContext db, string key, string value, string? description = null)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == key);
        if (param == null)
        {
            db.AppParameters.Add(new AppParameter
            {
                Key = key,
                Value = value,
                Description = description,
                IsActive = true
            });
        }
        else
        {
            param.Value = value;
            if (description != null) param.Description = description;
        }
        db.SaveChanges();
    }

    public static string GetDefaultReportPdfLocation(BocceDbContext db)
    {
        var location = GetAppParameter(db, "ReportPdfLocation");
        return location ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public static string GetWebsiteApiUrl(BocceDbContext db)
    {
        return GetAppParameter(db, "WebsiteApiUrl") ?? "";
    }

    public static string GetWebsiteApiKey(BocceDbContext db)
    {
        return GetAppParameter(db, "WebsiteApiKey") ?? "";
    }
}
