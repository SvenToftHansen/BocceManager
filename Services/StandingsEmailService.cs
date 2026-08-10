using BocceManager.Data;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

public record PlayerStandingsSendResult(string TeamLabel, string FirstName, string Email, bool Success, string? Error);

public record StandingsEmailSummary(
    int PlayersSent, int TeamsSent, int TeamsSkipped,
    List<string> SkippedTeams, List<PlayerStandingsSendResult> FailedSends);

// Who would receive a standings email and what team/division it's for — no report HTML built
// yet, so this is cheap to compute just to populate a confirm dialog (counts, preview picker).
public record StandingsEmailRosterEntry(int? PlayerId, string FirstName, string Email, string TeamLabel, int DivisionId, string DivisionName);

public record StandingsEmailRoster(string ClubName, int? LeagueId, List<StandingsEmailRosterEntry> Recipients, List<string> SkippedTeams);

// One recipient's fully personalized subject/body — built up front (no network calls) so the
// panel can preview exact send content before committing to SendAsync.
public record StandingsEmailRecipientPlan(
    int? PlayerId, string FirstName, string Email, string TeamLabel, int DivisionId, string Subject, string Body);

public record StandingsEmailPlan(string ClubName, int? LeagueId, List<StandingsEmailRecipientPlan> Recipients, List<string> SkippedTeams);

// Emails every active player their team's division standings plus the season-wide seed report,
// with a personalized greeting — one email per player (not one per team) since the greeting
// needs each recipient's own first name. BuildRoster (who gets one, no report content) and
// BuildPreviewEmail (one recipient's full content, for the confirm dialog's Preview button) are
// both cheap/synchronous; BuildPlan does the same work as BuildPreviewEmail but for everyone at
// once, cached per division, right before SendAsync fires off the actual Gmail calls.
public static class StandingsEmailService
{
    // One blank line between greeting paragraphs, two before the first report, three between
    // the two reports.
    private const string ParaSpacer = """<div style="height:1em;">&nbsp;</div>""";
    private const string GreetingSpacer = """
        <div style="height:1.4em;">&nbsp;</div><div style="height:1.4em;">&nbsp;</div>
        """;
    private const string ReportSpacer = """
        <div style="height:1.4em;">&nbsp;</div><div style="height:1.4em;">&nbsp;</div><div style="height:1.4em;">&nbsp;</div>
        """;

    private static string Enc(string s) => System.Net.WebUtility.HtmlEncode(s);
    private static string Para(string html) => $"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;\">{html}</div>";

    private static string BuildGreeting(string firstName, string teamLabel, string divisionName, string? extraMessage)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(Para($"Hello {Enc(firstName)}. Here are the standings for your Division ({Enc(divisionName)}) and for your team {Enc(teamLabel)}. " +
                       "Included is the current seeding table for season end tournament."));

        if (!string.IsNullOrWhiteSpace(extraMessage))
        {
            sb.Append(ParaSpacer);
            var encoded = Enc(extraMessage.Trim()).Replace("\r\n", "\n").Replace("\n", "<br>");
            sb.Append(Para(encoded));
        }

        sb.Append(ParaSpacer);
        sb.Append(Para("Thanks for participating, and we hope you enjoy the league."));
        sb.Append(ParaSpacer);
        sb.Append(Para("GVBB Executive."));

        return sb.ToString();
    }

    public static StandingsEmailRoster BuildRoster(int seasonId, int? leagueId, bool uniqueEmailOnly = true)
    {
        using var db = new BocceDbContext();
        var clubName = AppParameterService.GetAppParameter(db, "ClubName") ?? "Bocce League";
        var teams = db.Teams
            .Include(t => t.Division)
            .Where(t => t.Division.SeasonId == seasonId && t.IsActive)
            .OrderBy(t => t.Division.SortName).ThenBy(t => t.SortOrder)
            .ToList();

        var recipients = new List<StandingsEmailRosterEntry>();
        var skippedTeams = new List<string>();

        foreach (var t in teams)
        {
            var teamRecipients = EmailRecipientService.GetRecipients(db, EmailRecipientScope.Team, [t.Id], leagueId, seasonId, uniqueEmailOnly);
            if (teamRecipients.Count == 0) { skippedTeams.Add(t.EffectiveDisplayName); continue; }

            foreach (var r in teamRecipients)
                recipients.Add(new StandingsEmailRosterEntry(r.PlayerId, r.FirstName, r.Email, t.EffectiveDisplayName, t.DivisionId, t.Division.Name));
        }

        return new StandingsEmailRoster(clubName, leagueId, recipients, skippedTeams);
    }

    // Builds exactly one recipient's subject/body — used by the confirm dialog's Preview button
    // so it always reflects whatever extra message is currently typed in, without having to
    // rebuild every other recipient's email too.
    public static (string Subject, string Body) BuildPreviewEmail(
        int seasonId, int fromWeek, int toWeek, string clubName, StandingsEmailRosterEntry recipient, string? extraMessage)
    {
        var divisionHtml = ReportHtmlService.BuildStandingsHtml(seasonId, recipient.DivisionId, fromWeek, toWeek);
        var seasonSeedHtml = ReportHtmlService.BuildSeasonSeedHtml(seasonId, fromWeek, toWeek);
        var greeting = BuildGreeting(recipient.FirstName, recipient.TeamLabel, recipient.DivisionName, extraMessage);
        var body = greeting + GreetingSpacer + divisionHtml + ReportSpacer + seasonSeedHtml;
        var subject = $"{clubName} — {recipient.TeamLabel}: Division Standings & Season Seed";
        return (subject, body);
    }

    public static StandingsEmailPlan BuildPlan(int seasonId, int? leagueId, int fromWeek, int toWeek, string? extraMessage, bool uniqueEmailOnly = true)
    {
        var roster = BuildRoster(seasonId, leagueId, uniqueEmailOnly);
        if (roster.Recipients.Count == 0)
            return new StandingsEmailPlan(roster.ClubName, leagueId, [], roster.SkippedTeams);

        var seasonSeedHtml = ReportHtmlService.BuildSeasonSeedHtml(seasonId, fromWeek, toWeek);
        var divisionHtmlCache = new Dictionary<int, string>();
        var plans = new List<StandingsEmailRecipientPlan>();

        foreach (var r in roster.Recipients)
        {
            if (!divisionHtmlCache.TryGetValue(r.DivisionId, out var divisionHtml))
            {
                divisionHtml = ReportHtmlService.BuildStandingsHtml(seasonId, r.DivisionId, fromWeek, toWeek);
                divisionHtmlCache[r.DivisionId] = divisionHtml;
            }

            var greeting = BuildGreeting(r.FirstName, r.TeamLabel, r.DivisionName, extraMessage);
            var body = greeting + GreetingSpacer + divisionHtml + ReportSpacer + seasonSeedHtml;
            var subject = $"{roster.ClubName} — {r.TeamLabel}: Division Standings & Season Seed";
            plans.Add(new StandingsEmailRecipientPlan(r.PlayerId, r.FirstName, r.Email, r.TeamLabel, r.DivisionId, subject, body));
        }

        return new StandingsEmailPlan(roster.ClubName, leagueId, plans, roster.SkippedTeams);
    }

    // testMode sends exactly one email — the first recipient's content, addressed only to
    // testEmail with a "[TEST] " subject prefix — and doesn't touch EmailLog, so test sends
    // don't clutter real send history.
    public static async Task<StandingsEmailSummary> SendAsync(StandingsEmailPlan plan, bool testMode, string? testEmail,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (plan.Recipients.Count == 0)
            return new StandingsEmailSummary(0, 0, plan.SkippedTeams.Count, plan.SkippedTeams, []);
        if (testMode && string.IsNullOrWhiteSpace(testEmail))
            throw new InvalidOperationException("Enter a test email address.");

        var credential = await GmailAuthService.GetCredentialAsync(ct);
        var fromEmail = await GmailAuthService.GetConnectedAccountAsync(credential, ct);

        var toSend = testMode ? plan.Recipients.Take(1).ToList() : plan.Recipients;

        var failed = new List<PlayerStandingsSendResult>();
        var logRows = new List<(int? PlayerId, string Email, bool Success, string? Error)>();
        int playersSent = 0;
        var teamsSentTo = new HashSet<string>();

        foreach (var r in toSend)
        {
            ct.ThrowIfCancellationRequested();

            var toEmail = testMode ? testEmail!.Trim() : r.Email;
            var subject = testMode ? "[TEST] " + r.Subject : r.Subject;

            progress?.Report($"Emailing {r.FirstName} ({r.TeamLabel})...");

            try
            {
                await GmailSendService.SendAsync(credential, fromEmail, toEmail, subject, r.Body, null, ct);
                playersSent++;
                teamsSentTo.Add(r.TeamLabel);
                logRows.Add((r.PlayerId, toEmail, true, null));
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed to email standings to player {PlayerId}", r.PlayerId);
                failed.Add(new PlayerStandingsSendResult(r.TeamLabel, r.FirstName, r.Email, false, ex.Message));
                logRows.Add((r.PlayerId, toEmail, false, ex.Message));
            }

            await Task.Delay(250, ct);
        }

        if (!testMode)
        {
            using var db = new BocceDbContext();
            var log = new Data.Entities.EmailLog
            {
                LeagueId = plan.LeagueId,
                Subject = $"{plan.ClubName} — Division Standings & Season Seed",
                RecipientCount = playersSent,
                SentBy = Environment.UserName,
                SentAt = DateTime.UtcNow
            };
            db.EmailLogs.Add(log);
            db.SaveChanges();

            foreach (var (playerId, email, success, error) in logRows)
            {
                db.EmailLogRecipients.Add(new Data.Entities.EmailLogRecipient
                {
                    EmailLogId = log.Id,
                    PlayerId = playerId,
                    Email = email,
                    Status = success ? "Sent" : "Failed",
                    ErrorMessage = error,
                    SentAt = DateTime.UtcNow
                });
            }
            db.SaveChanges();
        }

        return new StandingsEmailSummary(playersSent, teamsSentTo.Count, plan.SkippedTeams.Count, plan.SkippedTeams, failed);
    }
}
