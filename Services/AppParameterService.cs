using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public class DefaultsChangedEventArgs : EventArgs
{
    public int? LeagueId { get; set; }
    public int? SeasonId { get; set; }
}

public static class AppParameterService
{
    public static event EventHandler<DefaultsChangedEventArgs>? DefaultsChanged;
    public static List<AppParameter> Load(BocceDbContext db)
        => [.. db.AppParameters.OrderBy(p => p.Key)];

    public static void Save(BocceDbContext db,
        IEnumerable<(string Key, string Value, string Desc)> rows)
    {
        db.AppParameters.RemoveRange(db.AppParameters);
        foreach (var (key, value, desc) in rows)
            db.AppParameters.Add(new AppParameter
                { Key = key, Value = value, Description = NullIfEmpty(desc) });
        db.SaveChanges();
    }

    public static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;

    // Formats an optional int for display — null renders as an em-dash.
    public static string Fmt(int? v) => v.HasValue ? v.Value.ToString() : "—";

    public static int? GetDefaultLeagueId(BocceDbContext db)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == "DefaultLeagueId");
        return param != null && int.TryParse(param.Value, out var id) ? id : null;
    }

    public static int? GetDefaultSeasonId(BocceDbContext db)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == "DefaultSeasonId");
        return param != null && int.TryParse(param.Value, out var id) ? id : null;
    }

    public static void SetDefaultLeagueId(BocceDbContext db, int? leagueId)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == "DefaultLeagueId");
        if (param == null)
        {
            db.AppParameters.Add(new AppParameter
            {
                Key = "DefaultLeagueId",
                Value = leagueId?.ToString() ?? "",
                Description = "Default league for user context"
            });
        }
        else
        {
            param.Value = leagueId?.ToString() ?? "";
        }
        db.SaveChanges();
        RaiseDefaultsChanged(leagueId, GetDefaultSeasonId(db));
    }

    public static void SetDefaultSeasonId(BocceDbContext db, int? seasonId)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == "DefaultSeasonId");
        if (param == null)
        {
            db.AppParameters.Add(new AppParameter
            {
                Key = "DefaultSeasonId",
                Value = seasonId?.ToString() ?? "",
                Description = "Default season for user context"
            });
        }
        else
        {
            param.Value = seasonId?.ToString() ?? "";
        }
        db.SaveChanges();
        RaiseDefaultsChanged(GetDefaultLeagueId(db), seasonId);
    }

    private static void RaiseDefaultsChanged(int? leagueId, int? seasonId)
    {
        DefaultsChanged?.Invoke(null, new DefaultsChangedEventArgs { LeagueId = leagueId, SeasonId = seasonId });
    }

    public static bool HasDefaults(BocceDbContext db)
    {
        var leagueId = GetDefaultLeagueId(db);
        var seasonId = GetDefaultSeasonId(db);
        return leagueId.HasValue && seasonId.HasValue;
    }

    public static string? GetAppParameter(BocceDbContext db, string key)
    {
        return db.AppParameters.FirstOrDefault(p => p.Key == key)?.Value;
    }

    public static void SetAppParameter(BocceDbContext db, string key, string value)
    {
        var param = db.AppParameters.FirstOrDefault(p => p.Key == key);
        if (param == null)
        {
            db.AppParameters.Add(new AppParameter { Key = key, Value = value });
        }
        else
        {
            param.Value = value;
        }
        db.SaveChanges();
    }
}
