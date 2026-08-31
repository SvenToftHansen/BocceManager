using BocceManager.Data;
using BocceManager.Data.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Services;

// Compares a Team Listing "Full Roster" Excel export (TeamsPrintService.ExportToExcel,
// ListingMode.FullRoster) that an admin has edited and returned, against the current
// database roster for the same season. Matches teams by their displayed identifier and
// players by their slot (PlayerNumber: 1 = Captain, 2+ = other players / Team Spare).
public static class RosterCompareService
{
    public enum DiffKind { PlayerAdded, PlayerRemoved, PlayerChanged, PhoneChanged }

    public record RosterDiff(
        int TeamId, string TeamLabel, int PlayerNumber, string SlotLabel,
        DiffKind Kind, string OldValue, string NewValue,
        int? OldPlayerId, int? NewPlayerId, bool Resolvable);

    public record RosterCompareResult(List<RosterDiff> Diffs, List<string> UnmatchedTeams);

    private sealed record SlotInfo(int PlayerId, string Name);

    private sealed class DbTeamInfo
    {
        public int TeamId;
        public string Label = "";
        public Dictionary<int, SlotInfo> Slots = new();
        public string Phone = "";
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public static RosterCompareResult Compare(int seasonId, string filePath)
    {
        using var db = new BocceDbContext();
        var dbTeams = LoadCurrentRoster(db, seasonId);
        var allPlayers = db.Players.AsNoTracking().ToList();

        var diffs = new List<RosterDiff>();
        var unmatchedTeams = new List<string>();

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        var colToPlayerNumber = new Dictionary<int, int?>();
        int phoneCol = -1;
        bool haveHeader = false;

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        for (int r = 1; r <= lastRow; r++)
        {
            var cell1 = ws.Cell(r, 1);
            var merged = cell1.MergedRange();
            if (merged != null && merged.ColumnCount() > 2)
                continue; // doc/time-slot/day label row spanning the full width

            string c1 = cell1.GetString().Trim();
            string c2 = ws.Cell(r, 2).GetString().Trim();

            if (c1.Equals("Team", StringComparison.OrdinalIgnoreCase) &&
                c2.Equals("Captain", StringComparison.OrdinalIgnoreCase))
            {
                colToPlayerNumber.Clear();
                phoneCol = -1;
                int c = 3;
                while (true)
                {
                    string h = ws.Cell(r, c).GetString().Trim();
                    if (string.IsNullOrEmpty(h)) break;
                    if (h.Equals("Contact #", StringComparison.OrdinalIgnoreCase)) { phoneCol = c; break; }
                    colToPlayerNumber[c] = HeaderToPlayerNumber(h);
                    c++;
                }
                haveHeader = true;
                continue;
            }

            if (!haveHeader || string.IsNullOrEmpty(c1)) continue;

            string teamText = c1;
            string teamKey = (teamText.EndsWith(" *") ? teamText[..^2] : teamText).Trim();

            if (!dbTeams.TryGetValue(teamKey, out var teamInfo))
            {
                if (!unmatchedTeams.Contains(teamText)) unmatchedTeams.Add(teamText);
                continue;
            }

            var newSlots = new Dictionary<int, string> { [1] = c2 };
            foreach (var (col, pn) in colToPlayerNumber)
            {
                if (pn == null) continue;
                newSlots[pn.Value] = ws.Cell(r, col).GetString().Trim();
            }
            CompareTeamRoster(teamInfo, newSlots, allPlayers, diffs);

            if (phoneCol > 0)
            {
                string newPhone = ws.Cell(r, phoneCol).GetString().Trim();
                if (!string.Equals(newPhone, teamInfo.Phone, StringComparison.OrdinalIgnoreCase))
                    diffs.Add(new RosterDiff(teamInfo.TeamId, teamInfo.Label, 1, "Phone",
                        DiffKind.PhoneChanged, teamInfo.Phone, newPhone, null, null, true));
            }
        }

        return new RosterCompareResult(diffs, unmatchedTeams);
    }

    public static (int Applied, List<string> Errors) Apply(IEnumerable<RosterDiff> selected)
    {
        var list = selected.ToList();
        var errors = new List<string>();
        int applied = 0;

        using var db = new BocceDbContext();
        using var tx = db.Database.BeginTransaction();
        var teamsTouched = new HashSet<int>();

        foreach (var d in list.Where(x => x.Kind != DiffKind.PhoneChanged))
        {
            try
            {
                switch (d.Kind)
                {
                    case DiffKind.PlayerRemoved:
                    {
                        var tp = db.TeamPlayers.FirstOrDefault(x => x.TeamId == d.TeamId && x.PlayerNumber == d.PlayerNumber);
                        if (tp != null) db.TeamPlayers.Remove(tp);
                        break;
                    }
                    case DiffKind.PlayerAdded:
                    case DiffKind.PlayerChanged:
                    {
                        if (!d.NewPlayerId.HasValue)
                        {
                            errors.Add($"{d.TeamLabel} — {d.SlotLabel}: \"{d.NewValue}\" not found in Players, skipped.");
                            continue;
                        }
                        var tp = db.TeamPlayers.FirstOrDefault(x => x.TeamId == d.TeamId && x.PlayerNumber == d.PlayerNumber);
                        if (tp == null)
                            db.TeamPlayers.Add(new TeamPlayer
                            {
                                TeamId = d.TeamId, PlayerId = d.NewPlayerId.Value, PlayerNumber = d.PlayerNumber,
                                Role = d.PlayerNumber == 1 ? "captain" : "player"
                            });
                        else
                            tp.PlayerId = d.NewPlayerId.Value;
                        break;
                    }
                }
                db.SaveChanges();
                applied++;
                teamsTouched.Add(d.TeamId);
            }
            catch (Exception ex)
            {
                errors.Add($"{d.TeamLabel} — {d.SlotLabel}: {ex.Message}");
            }
        }

        foreach (var d in list.Where(x => x.Kind == DiffKind.PhoneChanged))
        {
            try
            {
                var slot1 = db.TeamPlayers.Include(x => x.Player).FirstOrDefault(x => x.TeamId == d.TeamId && x.PlayerNumber == 1);
                if (slot1?.Player == null)
                {
                    errors.Add($"{d.TeamLabel} — Phone: no captain found to update, skipped.");
                    continue;
                }
                slot1.Player.Phone = d.NewValue;
                db.SaveChanges();
                applied++;
                teamsTouched.Add(d.TeamId);
            }
            catch (Exception ex)
            {
                errors.Add($"{d.TeamLabel} — Phone: {ex.Message}");
            }
        }

        foreach (var teamId in teamsTouched)
            TeamRosterService.SyncCaptain(db, teamId);

        tx.Commit();
        return (applied, errors);
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private static int? HeaderToPlayerNumber(string header)
    {
        header = header.Trim();
        if (header.Equals("Captain", StringComparison.OrdinalIgnoreCase)) return 1;
        if (header.Equals("Team Spare", StringComparison.OrdinalIgnoreCase)) return 5;
        if (header.StartsWith("Player ", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(header.AsSpan(7), out int n)) return n;
        return null;
    }

    private static string SlotLabel(int playerNumber) => playerNumber switch
    {
        1 => "Captain",
        5 => "Team Spare",
        _ => $"Player {playerNumber}"
    };

    private static Dictionary<string, DbTeamInfo> LoadCurrentRoster(BocceDbContext db, int seasonId)
    {
        var divisionIds = db.Divisions.Where(d => d.SeasonId == seasonId).Select(d => d.Id).ToList();
        var teams = db.Teams.Where(t => divisionIds.Contains(t.DivisionId)).AsNoTracking().ToList();
        var teamIds = teams.Select(t => t.Id).ToList();
        var teamPlayers = db.TeamPlayers.Where(tp => teamIds.Contains(tp.TeamId))
            .Include(tp => tp.Player).AsNoTracking().ToList();

        var result = new Dictionary<string, DbTeamInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in teams)
        {
            string label = !string.IsNullOrEmpty(team.DisplayName) ? team.DisplayName : team.SystemName;
            string key = label.Trim();
            var info = new DbTeamInfo { TeamId = team.Id, Label = label + (team.IsActive ? "" : " *") };

            foreach (var tp in teamPlayers.Where(x => x.TeamId == team.Id))
                info.Slots[tp.PlayerNumber] = new SlotInfo(tp.PlayerId, $"{tp.Player.FirstName} {tp.Player.LastName}".Trim());

            if (info.Slots.TryGetValue(1, out var captain))
                info.Phone = teamPlayers.First(x => x.TeamId == team.Id && x.PlayerNumber == 1).Player.Phone ?? "";

            result[key] = info;
        }
        return result;
    }

    // Compares a whole team's row against the DB. Captain (slot 1) is a distinct role, so it's
    // always compared directly by slot. The other seats (Player 2+, Team Spare) are treated as
    // an interchangeable pool: what matters is whether a player is still on the team at all, not
    // which specific column they land in — otherwise the same roster returned with Player 3 and
    // Player 4 swapped reads as two false "changes" instead of zero.
    private static void CompareTeamRoster(DbTeamInfo team, Dictionary<int, string> newSlots, List<Player> allPlayers, List<RosterDiff> diffs)
    {
        newSlots.TryGetValue(1, out var captainName);
        CompareSlot(team, 1, captainName ?? "", allPlayers, diffs);

        var newResolved = new Dictionary<int, (string Name, int? PlayerId)>();
        foreach (var (pn, raw) in newSlots)
        {
            if (pn == 1) continue;
            string name = (raw ?? "").Trim();
            if (name.Length > 0) newResolved[pn] = (name, ResolvePlayer(name, allPlayers));
        }

        var pendingOldPns = new HashSet<int>(team.Slots.Keys.Where(pn => pn != 1));
        var pendingNewPns = new HashSet<int>(newResolved.Keys);

        // Seats whose text didn't change need no further work.
        foreach (var pn in pendingOldPns.ToList())
        {
            if (newResolved.TryGetValue(pn, out var nv) &&
                string.Equals(team.Slots[pn].Name, nv.Name, StringComparison.OrdinalIgnoreCase))
            {
                pendingOldPns.Remove(pn);
                pendingNewPns.Remove(pn);
            }
        }

        // Players who are still on the team, just under a different column, aren't a change.
        var oldIdToPn = pendingOldPns
            .GroupBy(pn => team.Slots[pn].PlayerId)
            .ToDictionary(g => g.Key, g => g.First());
        foreach (var pn in pendingNewPns.ToList())
        {
            var nv = newResolved[pn];
            if (nv.PlayerId.HasValue && oldIdToPn.TryGetValue(nv.PlayerId.Value, out var oldPn) && pendingOldPns.Contains(oldPn))
            {
                pendingOldPns.Remove(oldPn);
                pendingNewPns.Remove(pn);
            }
        }

        // What's left are genuine additions, removals, or substitutions.
        foreach (var pn in pendingOldPns.Union(pendingNewPns).Distinct().OrderBy(x => x))
        {
            bool hasOld = pendingOldPns.Contains(pn);
            bool hasNew = pendingNewPns.Contains(pn);
            string slotLabel = SlotLabel(pn);

            if (hasOld && hasNew)
            {
                var old = team.Slots[pn];
                var nv = newResolved[pn];
                diffs.Add(new RosterDiff(team.TeamId, team.Label, pn, slotLabel,
                    DiffKind.PlayerChanged, old.Name, nv.Name, old.PlayerId, nv.PlayerId, nv.PlayerId.HasValue));
            }
            else if (hasOld)
            {
                var old = team.Slots[pn];
                diffs.Add(new RosterDiff(team.TeamId, team.Label, pn, slotLabel,
                    DiffKind.PlayerRemoved, old.Name, "", old.PlayerId, null, true));
            }
            else
            {
                var nv = newResolved[pn];
                diffs.Add(new RosterDiff(team.TeamId, team.Label, pn, slotLabel,
                    DiffKind.PlayerAdded, "", nv.Name, null, nv.PlayerId, nv.PlayerId.HasValue));
            }
        }
    }

    private static void CompareSlot(DbTeamInfo team, int playerNumber, string newName, List<Player> allPlayers, List<RosterDiff> diffs)
    {
        team.Slots.TryGetValue(playerNumber, out var old);
        string oldName = old?.Name ?? "";
        newName = (newName ?? "").Trim();

        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

        string slotLabel = SlotLabel(playerNumber);

        if (oldName.Length == 0 && newName.Length > 0)
        {
            var resolved = ResolvePlayer(newName, allPlayers);
            diffs.Add(new RosterDiff(team.TeamId, team.Label, playerNumber, slotLabel,
                DiffKind.PlayerAdded, "", newName, null, resolved, resolved.HasValue));
        }
        else if (oldName.Length > 0 && newName.Length == 0)
        {
            diffs.Add(new RosterDiff(team.TeamId, team.Label, playerNumber, slotLabel,
                DiffKind.PlayerRemoved, oldName, "", old?.PlayerId, null, true));
        }
        else
        {
            var resolved = ResolvePlayer(newName, allPlayers);
            diffs.Add(new RosterDiff(team.TeamId, team.Label, playerNumber, slotLabel,
                DiffKind.PlayerChanged, oldName, newName, old?.PlayerId, resolved, resolved.HasValue));
        }
    }

    private static int? ResolvePlayer(string fullName, List<Player> allPlayers)
    {
        var matches = allPlayers
            .Where(p => string.Equals($"{p.FirstName} {p.LastName}".Trim(), fullName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return matches.Count == 1 ? matches[0].Id : null;
    }
}
