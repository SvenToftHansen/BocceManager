using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

// Builds a Google-Contacts-importable CSV from active players, grouping them into
// labels ("All Players", "{Year} Players" per season, "Sublist - {League}" per league,
// "Executive") and merging couples who share one email into a single combined contact.
public static class GoogleContactsExportService
{
    private const string OrganizationName = "GV Bocce Club";

    private record PlayerGroups(Player Player, List<string> Labels);

    public static void Export(Control parent)
    {
        using var saveDlg = new SaveFileDialog
        {
            Title = "Export to Google Contacts",
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"GoogleContacts_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDlg.ShowDialog(parent) != DialogResult.OK) return;

        try
        {
            using var db = new BocceDbContext();
            var rows = BuildRows(db);

            using var writer = new StreamWriter(saveDlg.FileName);
            writer.WriteLine(CsvLine(["Name", "Given Name", "Family Name", "E-mail 1 - Value",
                "Phone 1 - Value", "Organization 1 - Name", "Notes", "Group Membership"]));

            foreach (var row in rows)
                writer.WriteLine(CsvLine(row));

            AppLogger.Info("Google Contacts export saved: {Path} ({Count} contacts)", saveDlg.FileName, rows.Count);
            MessageBox.Show($"Exported {rows.Count} contacts to {saveDlg.FileName}", "Export Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Google Contacts export failed");
            MessageBox.Show($"Error exporting contacts: {ex.Message}", "Export Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static List<string[]> BuildRows(BocceDbContext db)
    {
        var players = db.Players.Where(p => p.IsActive).ToList();
        var playerIds = players.Select(p => p.Id).ToHashSet();

        var seasons = db.Seasons.ToList();
        var seasonLabels = seasons.ToDictionary(s => s.Id, s => $"{YearLabel(s)} Players");
        // newest season first, so Group Membership lists recent seasons before older ones
        var seasonOrder = seasons.OrderByDescending(s => s.StartDate ?? DateOnly.MinValue)
            .Select(s => s.Id).ToList();

        var playerSeasons = db.TeamPlayers
            .Where(tp => tp.IsActive && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, SeasonId = tp.Team.Division.SeasonId })
            .ToList()
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SeasonId).ToHashSet());

        var leagues = db.Leagues.ToList();
        var leagueLabels = leagues.ToDictionary(l => l.Id, l => $"Sublist - {l.Name}");

        var playerLeagues = db.SpareLists
            .Where(s => s.IsActive && playerIds.Contains(s.PlayerId))
            .Select(s => new { s.PlayerId, s.LeagueId })
            .ToList()
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.LeagueId).ToHashSet());

        var playerGroups = players.Select(p =>
        {
            var labels = new List<string> { "All Players" };

            if (playerSeasons.TryGetValue(p.Id, out var seasonIds))
                labels.AddRange(seasonOrder.Where(seasonIds.Contains).Select(id => seasonLabels[id]));

            if (playerLeagues.TryGetValue(p.Id, out var leagueIds))
                labels.AddRange(leagues.Where(l => leagueIds.Contains(l.Id)).Select(l => leagueLabels[l.Id]));

            if (p.Role != 0)
                labels.Add("Executive");

            return new PlayerGroups(p, labels);
        }).ToList();

        return playerGroups
            .GroupBy(pg => string.IsNullOrWhiteSpace(pg.Player.Email)
                ? Guid.NewGuid().ToString()
                : pg.Player.Email.Trim().ToLowerInvariant())
            .Select(BuildHouseholdRow)
            .ToList();
    }

    private static string YearLabel(Season season) =>
        season.StartDate?.Year.ToString() ?? season.Name;

    private static string[] BuildHouseholdRow(IGrouping<string, PlayerGroups> household)
    {
        var members = household.ToList();
        var first = members[0].Player;

        string givenName, familyName, fullName;
        if (members.Count == 1)
        {
            givenName = first.FirstName;
            familyName = first.LastName;
            fullName = $"{first.FirstName} {first.LastName}".Trim();
        }
        else if (members.Select(m => m.Player.LastName.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
        {
            givenName = CombineNames(members.Select(m => m.Player.FirstName));
            familyName = first.LastName;
            fullName = $"{givenName} {familyName}".Trim();
        }
        else
        {
            givenName = "";
            familyName = "";
            fullName = CombineNames(members.Select(m => $"{m.Player.FirstName} {m.Player.LastName}".Trim()));
        }

        var email = members.Select(m => m.Player.Email).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)) ?? "";
        var phone = members.Select(m => m.Player.Phone).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? "";
        var lotNumber = members.Select(m => m.Player.LotNumber).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        var notes = lotNumber != null ? $"Lot {lotNumber}" : "";

        var groupMembership = string.Join(" ::: ", members.SelectMany(m => m.Labels).Distinct());

        return [fullName, givenName, familyName, email, phone, OrganizationName, notes, groupMembership];
    }

    private static string CombineNames(IEnumerable<string> names)
    {
        var distinct = names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return distinct.Count switch
        {
            0 => "",
            1 => distinct[0],
            _ => string.Join(", ", distinct.Take(distinct.Count - 1)) + " & " + distinct[^1]
        };
    }

    private static string CsvLine(IEnumerable<string> fields) =>
        string.Join(",", fields.Select(f => $"\"{f.Replace("\"", "\"\"")}\""));
}
