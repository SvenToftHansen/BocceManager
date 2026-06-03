using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public static class AppParameterService
{
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
}
