namespace BocceManager.Services;

public enum AttachableReport { SpareList, UnpaidFees, AllPlayers, TeamListing, Schedule, Standings, SeasonSeed, PlayoffSchedule }

public record AttachableReportOption(AttachableReport Report, string Name)
{
    public override string ToString() => Name;
}

// Lets Compose Email insert one of the app's existing reports into the message body as themed
// HTML (colors/headers mirroring the report's own PrintDocument), built by ReportHtmlService.
// This is content-only — there is no PDF-attach path here; to email a report as a PDF, generate
// it from the report's own panel (Print Preview → Save as PDF, which saves into the Documents
// folder and registers it there) and attach it via Compose's "Attach Document" button.
public static class ReportAttachmentService
{
    public static List<AttachableReportOption> GetOptions() =>
    [
        new(AttachableReport.SpareList, "Spare List"),
        new(AttachableReport.UnpaidFees, "Unpaid Fees"),
        new(AttachableReport.AllPlayers, "All Players"),
        new(AttachableReport.TeamListing, "Team Listing"),
        new(AttachableReport.Schedule, "Schedule"),
        new(AttachableReport.Standings, "Standings"),
        new(AttachableReport.SeasonSeed, "Season Seed"),
        new(AttachableReport.PlayoffSchedule, "Playoff Schedule"),
    ];

    public static string BuildHtmlContent(AttachableReport report, int leagueId, int? seasonId) => report switch
    {
        AttachableReport.SpareList => ReportHtmlService.BuildSpareListHtml(leagueId, seasonId),
        AttachableReport.UnpaidFees => ReportHtmlService.BuildUnpaidFeesHtml(leagueId, seasonId),
        AttachableReport.AllPlayers => ReportHtmlService.BuildAllPlayersHtml(leagueId, seasonId),
        AttachableReport.TeamListing => RequireSeason(seasonId, "Team Listing", ReportHtmlService.BuildTeamListingHtml),
        AttachableReport.Schedule => RequireSeason(seasonId, "Schedule", ReportHtmlService.BuildScheduleHtml),
        AttachableReport.Standings => RequireSeason(seasonId, "Standings", s => ReportHtmlService.BuildStandingsHtml(s)),
        AttachableReport.SeasonSeed => RequireSeason(seasonId, "Season Seed", s => ReportHtmlService.BuildSeasonSeedHtml(s)),
        AttachableReport.PlayoffSchedule => RequireSeason(seasonId, "Playoff Schedule", ReportHtmlService.BuildPlayoffScheduleHtml),
        _ => throw new ArgumentOutOfRangeException(nameof(report))
    };

    private static string RequireSeason(int? seasonId, string reportName, Func<int, string> build) =>
        seasonId.HasValue ? build(seasonId.Value)
            : throw new InvalidOperationException($"Select a default season before inserting the {reportName} report.");
}
