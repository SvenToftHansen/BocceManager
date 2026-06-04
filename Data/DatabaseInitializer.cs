using System.Data.Common;
using BocceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var db = new BocceDbContext();

        // For PostgreSQL, use migrations to ensure schema is created
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            // If migrations fail, try EnsureCreated as fallback
            Console.WriteLine($"Migration failed ({ex.Message}), attempting EnsureCreated...");
            db.Database.EnsureCreated();

            // Only apply schema patches if using EnsureCreated (not migrations)
            ApplySchemaPatches(db);
        }

        // Seed reference data (safe to run even if schema already exists)
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
        AddColumnIfMissing(conn, "Leagues", "MaxTeamsInDivision",    "INTEGER NOT NULL DEFAULT 0");

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

        // Seasons — new fields
        AddColumnIfMissing(conn, "Seasons", "IsCurrent",           "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "WeeksInSeason",       "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "MaxTeamsInDivision",  "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(conn, "Seasons", "Status",              "TEXT NOT NULL DEFAULT 'building'");
        AddColumnIfMissing(conn, "Seasons", "PlayoffStartDate",    "TEXT");
        AddColumnIfMissing(conn, "Seasons", "PlayoffEndDate",      "TEXT");

        // DaySlots / TimeSlots — IsActive may be missing on older databases
        AddColumnIfMissing(conn, "DaySlots",  "IsActive", "INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing(conn, "TimeSlots", "IsActive", "INTEGER NOT NULL DEFAULT 1");

        // Divisions
        AddColumnIfMissing(conn, "Divisions", "PlayersPerTeamMinimum", "INTEGER");
        AddColumnIfMissing(conn, "Divisions", "PlayersPerTeamMaximum", "INTEGER");
        AddColumnIfMissing(conn, "Divisions", "ShortName", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(conn, "Divisions", "SortName",  "TEXT NOT NULL DEFAULT ''");

        // Fix ShortName format: "Mo0900" → "Mo-0900" (insert dash before the 4-digit time suffix)
        using (var fix = conn.CreateCommand())
        {
            fix.CommandText = """
                UPDATE "Divisions"
                SET "ShortName" = SUBSTRING("ShortName", 1, LENGTH("ShortName") - 4) || '-' || SUBSTRING("ShortName", LENGTH("ShortName") - 3)
                WHERE LENGTH("ShortName") > 4 AND "ShortName" NOT LIKE '%-%'
                """;
            fix.ExecuteNonQuery();
        };

        // Documents
        CreateTableIfMissing(conn, "ClubDocuments", """
            CREATE TABLE "ClubDocuments" (
                "Id"           INTEGER NOT NULL CONSTRAINT "PK_ClubDocuments" PRIMARY KEY AUTOINCREMENT,
                "Title"        TEXT    NOT NULL DEFAULT '',
                "FileName"     TEXT    NOT NULL DEFAULT '',
                "DocType"      TEXT    NOT NULL DEFAULT '',
                "GoogleDocsUrl" TEXT,
                "LeagueId"     INTEGER,
                "UploadedAt"   TEXT    NOT NULL DEFAULT '',
                "Notes"        TEXT
            )
            """);

        // Finance — GL accounts and journal entries
        CreateTableIfMissing(conn, "GlAccounts", """
            CREATE TABLE "GlAccounts" (
                "Id"             INTEGER NOT NULL CONSTRAINT "PK_GlAccounts" PRIMARY KEY AUTOINCREMENT,
                "Code"           TEXT    NOT NULL DEFAULT '',
                "Name"           TEXT    NOT NULL DEFAULT '',
                "AccountType"    TEXT    NOT NULL DEFAULT '',
                "OpeningBalance" TEXT    NOT NULL DEFAULT '0',
                "IsActive"       INTEGER NOT NULL DEFAULT 1,
                "CreatedAt"      TEXT    NOT NULL DEFAULT ''
            )
            """);

        CreateTableIfMissing(conn, "JournalEntries", """
            CREATE TABLE "JournalEntries" (
                "Id"              INTEGER NOT NULL CONSTRAINT "PK_JournalEntries" PRIMARY KEY AUTOINCREMENT,
                "EntryDate"       TEXT    NOT NULL DEFAULT '',
                "Amount"          TEXT    NOT NULL DEFAULT '0',
                "Description"     TEXT    NOT NULL DEFAULT '',
                "DebitAccountId"  INTEGER NOT NULL,
                "CreditAccountId" INTEGER NOT NULL,
                "Reference"       TEXT,
                "Notes"           TEXT,
                "EnteredBy"       TEXT,
                "CreatedAt"       TEXT    NOT NULL DEFAULT ''
            )
            """);

        // SpareLists — migrate from old schema (Name/Description) to new (PlayerId/IsActive)
        // If the old table exists (detected by presence of "Name" column), rename and recreate.
        if (TableHasColumn(conn, "SpareLists", "Name"))
        {
            using var rename = conn.CreateCommand();
            rename.CommandText = "ALTER TABLE \"SpareLists\" RENAME TO \"SpareLists_legacy\"";
            rename.ExecuteNonQuery();
        }
        CreateTableIfMissing(conn, "SpareLists", """
            CREATE TABLE "SpareLists" (
                "Id"       INTEGER NOT NULL CONSTRAINT "PK_SpareLists" PRIMARY KEY AUTOINCREMENT,
                "LeagueId" INTEGER NOT NULL,
                "PlayerId" INTEGER NOT NULL,
                "IsActive" INTEGER NOT NULL DEFAULT 1
            )
            """);

        // LookingForTeam — one row per player seeking a team in a league
        CreateTableIfMissing(conn, "LookingForTeams", """
            CREATE TABLE "LookingForTeams" (
                "Id"       INTEGER NOT NULL CONSTRAINT "PK_LookingForTeams" PRIMARY KEY AUTOINCREMENT,
                "LeagueId" INTEGER NOT NULL,
                "PlayerId" INTEGER NOT NULL
            )
            """);

        // Ensure Documents folder exists alongside the database
        Directory.CreateDirectory(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents"));
    }

    private static bool TableHasColumn(DbConnection conn, string table, string column)
    {
        using var check = conn.CreateCommand();
        // PostgreSQL query to check if table exists
        check.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{table.ToLower()}'";
        if (Convert.ToInt32(check.ExecuteScalar()) == 0) return false;

        using var pragma = conn.CreateCommand();
        // PostgreSQL query to check if column exists
        pragma.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{table.ToLower()}' AND column_name = '{column.ToLower()}'";
        return Convert.ToInt32(pragma.ExecuteScalar()) > 0;
    }

    private static void CreateTableIfMissing(DbConnection conn, string table, string createSql)
    {
        using var check = conn.CreateCommand();
        // PostgreSQL query to check if table exists
        check.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{table.ToLower()}'";
        if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;

        // Convert SQLite CREATE TABLE syntax to PostgreSQL syntax
        string postgresSql = ConvertSqliteToPostgresSql(createSql);
        using var create = conn.CreateCommand();
        create.CommandText = postgresSql;
        create.ExecuteNonQuery();
    }

    private static void AddColumnIfMissing(DbConnection conn, string table, string column, string sqlType)
    {
        using var check = conn.CreateCommand();
        // PostgreSQL query to check if column exists
        check.CommandText = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{table.ToLower()}' AND column_name = '{column.ToLower()}'";
        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            return; // already exists

        // Convert SQLite types to PostgreSQL types
        string pgType = ConvertSqliteTypeToPostgresType(sqlType);
        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {pgType}";
        alter.ExecuteNonQuery();
    }

    private static string ConvertSqliteTypeToPostgresType(string sqliteType)
    {
        // Map SQLite types to PostgreSQL equivalents
        return sqliteType.ToUpper() switch
        {
            "TEXT" => "TEXT",
            "INTEGER" => "INTEGER",
            "INTEGER NOT NULL DEFAULT 0" => "INTEGER NOT NULL DEFAULT 0",
            "INTEGER NOT NULL DEFAULT 1" => "INTEGER NOT NULL DEFAULT 1",
            _ when sqliteType.ToUpper().StartsWith("INTEGER NOT NULL DEFAULT") => sqliteType.ToUpper(),
            _ when sqliteType.ToUpper().StartsWith("TEXT NOT NULL DEFAULT") => sqliteType.ToUpper(),
            _ => sqliteType
        };
    }

    private static string ConvertSqliteToPostgresSql(string sqliteSql)
    {
        // Replace SQLite-specific syntax with PostgreSQL equivalents
        string result = sqliteSql
            .Replace("AUTOINCREMENT", "")
            .Replace("CONSTRAINT \"PK_", "CONSTRAINT PK_")
            .Replace("PRIMARY KEY AUTOINCREMENT", "PRIMARY KEY GENERATED ALWAYS AS IDENTITY")
            .Replace("PRIMARY KEY", "PRIMARY KEY GENERATED ALWAYS AS IDENTITY");

        // Replace INTEGER with SERIAL for auto-increment IDs (but only if they had AUTOINCREMENT)
        if (result.Contains("GENERATED ALWAYS AS IDENTITY"))
        {
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @"""Id""\s+INTEGER\s+NOT NULL\s+CONSTRAINT",
                "\"Id\" SERIAL NOT NULL CONSTRAINT"
            );
        }

        return result;
    }

    private static void SeedReferenceData(BocceDbContext db)
    {
        // Upsert day slots so any missing days are added even if others already exist
        (string Name, string Abbr, int Nbr)[] days =
        [
            ("Monday", "MON", 1), ("Tuesday", "TUE", 2), ("Wednesday", "WED", 3),
            ("Thursday", "THU", 4), ("Friday", "FRI", 5), ("Saturday", "SAT", 6), ("Sunday", "SUN", 7)
        ];
        var existingDayNbrs = db.DaySlots.Select(d => d.DayNbr).ToHashSet();
        foreach (var (name, abbr, nbr) in days)
            if (!existingDayNbrs.Contains(nbr))
                db.DaySlots.Add(new DaySlot { DayName = name, DayAbbr = abbr, DayNbr = nbr });
        // Ensure all day slots are active
        foreach (var d in db.DaySlots.Where(d => !d.IsActive).ToList())
            d.IsActive = true;

        // Upsert time slots — 0800–2000; new times can be added without recreating the table
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
            ("7:00 PM",  "1900", 23),  ("7:30 PM",  "1930", 24),
            ("8:00 PM",  "2000", 25),
        ];
        var existingTimes = db.TimeSlots.Select(t => t.Timeslot24h).ToHashSet();
        foreach (var (h12, h24, order) in slots)
            if (!existingTimes.Contains(h24))
                db.TimeSlots.Add(new TimeSlot { Timeslot12h = h12, Timeslot24h = h24, SortOrder = order });
        // Ensure all time slots are active
        foreach (var t in db.TimeSlots.Where(t => !t.IsActive).ToList())
            t.IsActive = true;

        if (!db.AppParameters.Any())
        {
            db.AppParameters.AddRange(
                new AppParameter { Key = "ClubName",            Value = "Golden Vista Bocce Club", Description = "Full name of the bocce club" },
                new AppParameter { Key = "LeagueCaptainName",   Value = "",                        Description = "Name of the league captain" },
                new AppParameter { Key = "LeagueCaptainEmail",  Value = "",                        Description = "Email address of the league captain" }
            );
        }

        // Seed chart of accounts if empty
        if (!db.GlAccounts.Any())
        {
            db.GlAccounts.AddRange(
                new GlAccount { Code = "1000", Name = "Chequing",              AccountType = "asset" },
                new GlAccount { Code = "1100", Name = "Deposits Outstanding",  AccountType = "asset" },
                new GlAccount { Code = "4100", Name = "Initiation Fees",       AccountType = "income" },
                new GlAccount { Code = "4200", Name = "Season Fees",           AccountType = "income" },
                new GlAccount { Code = "4300", Name = "Event Revenue",         AccountType = "income" },
                new GlAccount { Code = "4400", Name = "Donations",             AccountType = "income" },
                new GlAccount { Code = "4500", Name = "Other Income",          AccountType = "income" },
                new GlAccount { Code = "5100", Name = "Website Hosting",       AccountType = "expense" },
                new GlAccount { Code = "5200", Name = "Court Maintenance",     AccountType = "expense" },
                new GlAccount { Code = "5300", Name = "Event Expenses",        AccountType = "expense" },
                new GlAccount { Code = "5400", Name = "Other Expenses",        AccountType = "expense" }
            );
        }

        db.SaveChanges();
    }
}
