using System.Data.Common;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var db = new BocceDbContext();
        db.Database.EnsureCreated();
        ApplySchemaPatches(db);
        SeedReferenceData(db);
    }

    // Adds columns that were introduced after the initial schema was created.
    // Safe to run on every startup — AddColumnIfMissing is a no-op when the column exists.
    private static void ApplySchemaPatches(BocceDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        // Leagues
        AddColumnIfMissing(conn, "Leagues", "RulesText",             "TEXT");
        AddColumnIfMissing(conn, "Leagues", "PlayersPerTeamMinimum", "INTEGER");
        AddColumnIfMissing(conn, "Leagues", "PlayersPerTeamMaximum", "INTEGER");

        // Seasons
        AddColumnIfMissing(conn, "Seasons", "PlayersPerTeamMinimum", "INTEGER");
        AddColumnIfMissing(conn, "Seasons", "PlayersPerTeamMaximum", "INTEGER");
        AddColumnIfMissing(conn, "Seasons", "GamesPerSeason",        "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "GameInterval",          "TEXT NOT NULL DEFAULT 'weekly'");
        AddColumnIfMissing(conn, "Seasons", "TimeslotDriven",        "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing(conn, "Seasons", "PointsForWin",          "INTEGER NOT NULL DEFAULT 2");
        AddColumnIfMissing(conn, "Seasons", "PointsForTie",          "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing(conn, "Seasons", "PointsForLoss",         "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "PointsForNoShow",       "INTEGER NOT NULL DEFAULT -1");
        AddColumnIfMissing(conn, "Seasons", "PointsToWinGame",       "INTEGER NOT NULL DEFAULT 12");
        AddColumnIfMissing(conn, "Seasons", "GamesPerMatch",         "INTEGER NOT NULL DEFAULT 2");
        AddColumnIfMissing(conn, "Seasons", "ScoringMode",           "TEXT NOT NULL DEFAULT 'games_mode'");
        AddColumnIfMissing(conn, "Seasons", "TeamsInPlayoffs",       "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "FirstPlaceGuaranteed",  "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing(conn, "Seasons", "PlayoffType",           "TEXT NOT NULL DEFAULT 'ladder'");
        AddColumnIfMissing(conn, "Seasons", "PlayoffGamesPerMatch",  "INTEGER NOT NULL DEFAULT 2");
        AddColumnIfMissing(conn, "Seasons", "PlayoffScoringMode",    "TEXT NOT NULL DEFAULT 'match_play'");
        AddColumnIfMissing(conn, "Seasons", "PlayoffTiebreakerEnd",  "INTEGER NOT NULL DEFAULT 1");

        // Divisions
        AddColumnIfMissing(conn, "Divisions", "PlayersPerTeamMinimum", "INTEGER");
        AddColumnIfMissing(conn, "Divisions", "PlayersPerTeamMaximum", "INTEGER");
    }

    private static void AddColumnIfMissing(DbConnection conn, string table, string column, string sqlType)
    {
        using var pragma = conn.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({table})";
        using var reader = pragma.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return; // already exists
        }

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {sqlType}";
        alter.ExecuteNonQuery();
    }

    private static void SeedReferenceData(BocceDbContext db)
    {
        if (!db.DaySlots.Any())
        {
            db.DaySlots.AddRange(
                new DaySlot { DayName = "Monday",    DayAbbr = "MON", DayNbr = 1 },
                new DaySlot { DayName = "Tuesday",   DayAbbr = "TUE", DayNbr = 2 },
                new DaySlot { DayName = "Wednesday", DayAbbr = "WED", DayNbr = 3 },
                new DaySlot { DayName = "Thursday",  DayAbbr = "THU", DayNbr = 4 },
                new DaySlot { DayName = "Friday",    DayAbbr = "FRI", DayNbr = 5 },
                new DaySlot { DayName = "Saturday",  DayAbbr = "SAT", DayNbr = 6 },
                new DaySlot { DayName = "Sunday",    DayAbbr = "SUN", DayNbr = 7 }
            );
        }

        if (!db.TimeSlots.Any())
        {
            (string h12, string h24, int order)[] slots =
            [
                ("8:00 AM",  "0800",  1),  ("8:30 AM",  "0830",  2),
                ("9:00 AM",  "0900",  3),  ("9:30 AM",  "0930",  4),
                ("10:00 AM", "1000",  5),  ("10:30 AM", "1030",  6),
                ("11:00 AM", "1100",  7),  ("11:30 AM", "1130",  8),
                ("12:00 PM", "1200",  9),  ("12:30 PM", "1230", 10),
                ("1:00 PM",  "1300", 11),  ("1:30 PM",  "1330", 12),
                ("2:00 PM",  "1400", 13),  ("2:30 PM",  "1430", 14),
                ("3:00 PM",  "1500", 15),  ("3:30 PM",  "1530", 16),
                ("4:00 PM",  "1600", 17),  ("4:30 PM",  "1630", 18),
                ("5:00 PM",  "1700", 19),  ("5:30 PM",  "1730", 20),
                ("6:00 PM",  "1800", 21),  ("6:30 PM",  "1830", 22),
                ("7:00 PM",  "1900", 23),
            ];
            foreach (var (h12, h24, order) in slots)
                db.TimeSlots.Add(new TimeSlot { Timeslot12h = h12, Timeslot24h = h24, SortOrder = order });
        }

        if (!db.AppParameters.Any())
        {
            db.AppParameters.AddRange(
                new AppParameter { Key = "ClubName",            Value = "Golden Vista Bocce Club", Description = "Full name of the bocce club" },
                new AppParameter { Key = "LeagueCaptainName",   Value = "",                        Description = "Name of the league captain" },
                new AppParameter { Key = "LeagueCaptainEmail",  Value = "",                        Description = "Email address of the league captain" }
            );
        }

        db.SaveChanges();
    }
}
