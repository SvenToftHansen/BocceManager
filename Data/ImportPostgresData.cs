using BocceManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BocceManager.Data;

/// <summary>
/// Imports player data, spare lists, and looking-for-team records from PostgreSQL backup (dump_inserts.sql).
/// Parses SQL INSERT statements directly instead of hardcoding data.
/// Run via: var importer = new ImportPostgresData("path/to/dump_inserts.sql"); importer.Execute();
/// </summary>
public class ImportPostgresData
{
    private readonly BocceDbContext _db;
    private readonly string _sqlBackupPath;

    public ImportPostgresData(string sqlBackupPath = null)
    {
        _db = new BocceDbContext();
        _sqlBackupPath = sqlBackupPath ?? GetDefaultBackupPath();
    }

    private string GetDefaultBackupPath()
    {
        var defaultPath = @"C:\Users\svenh\Documents\BocceDocs\dump_inserts.sql";
        if (File.Exists(defaultPath))
            return defaultPath;

        // Try alternative paths
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var altPath = Path.Combine(Path.GetDirectoryName(baseDir), "..", "..", "..", "..", "dump_inserts.sql");
        altPath = Path.GetFullPath(altPath);

        return altPath;
    }

    public void Execute()
    {
        try
        {
            Console.WriteLine("Starting PostgreSQL data import...\n");

            if (!File.Exists(_sqlBackupPath))
            {
                Console.WriteLine($"ERROR: Backup file not found at: {_sqlBackupPath}");
                return;
            }

            var sqlContent = File.ReadAllText(_sqlBackupPath);

            // Step 1: Create Spring League
            var league = CreateOrGetSpringLeague();
            Console.WriteLine($"✓ Spring League ready (ID: {league.Id})");

            // Step 2: Create Spring Season
            var season = CreateOrGetSpringseason(league);
            Console.WriteLine($"✓ Spring Season ready (ID: {season.Id})");

            // Step 3: Parse and import all players
            var playerMap = ParseAndImportPlayers(sqlContent);
            Console.WriteLine($"✓ Imported {playerMap.Count} players");

            // Step 4: Parse and populate LookingForTeams
            var lookingCount = ParseAndPopulateLookingForTeams(sqlContent, league, season, playerMap);
            Console.WriteLine($"✓ Created {lookingCount} LookingForTeams entries");

            // Step 5: Parse and populate SpareLists
            var spareCount = ParseAndPopulateSpareLists(sqlContent, league, playerMap);
            Console.WriteLine($"✓ Created SpareList with {spareCount} players");

            Console.WriteLine($"\n✓ Import complete!");
            Console.WriteLine($"  Total players: {_db.Players.Count()}");
            Console.WriteLine($"  Total LookingForTeams: {_db.LookingForTeams.Count()}");
            Console.WriteLine($"  Total SpareLists: {_db.SpareLists.Count()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    private League CreateOrGetSpringLeague()
    {
        var existing = _db.Leagues.FirstOrDefault(l => l.Name == "Spring League");
        if (existing != null) return existing;

        var league = new League
        {
            Name = "Spring League",
            Description = "Spring season league imported from PostgreSQL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Leagues.Add(league);
        _db.SaveChanges();
        return league;
    }

    private Season CreateOrGetSpringseason(League league)
    {
        var existing = _db.Seasons.FirstOrDefault(s => s.Name == "Spring Season" && s.LeagueId == league.Id);
        if (existing != null) return existing;

        var season = new Season
        {
            LeagueId = league.Id,
            Name = "Spring Season",
            Status = "building",
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Seasons.Add(season);
        _db.SaveChanges();
        return season;
    }

    /// <summary>
    /// Parses the players INSERT statement from SQL and imports all players.
    /// Returns a mapping of PostgreSQL player ID -> SQLite Player object.
    /// Handles SQL-escaped quotes ('' for apostrophes in names).
    /// </summary>
    private Dictionary<int, Player> ParseAndImportPlayers(string sqlContent)
    {
        var playerMap = new Dictionary<int, Player>();

        // Find the players INSERT statement
        var playerPattern = @"INSERT INTO public\.players\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;)";
        var match = Regex.Match(sqlContent, playerPattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No players data found in backup");
            return playerMap;
        }

        var valuesBlock = match.Groups[1].Value;

        // Split by ),\n\t( to get each record (handles the formatting with tabs)
        var records = Regex.Split(valuesBlock, @"\),\s*\(");

        int importedCount = 0;

        foreach (var record in records)
        {
            try
            {
                // Clean up the record (remove leading/trailing parens if still there)
                var cleanRecord = record.Trim('(', ')').Trim();
                if (string.IsNullOrWhiteSpace(cleanRecord))
                    continue;

                // Split by comma, but we need to be smart about commas inside quoted strings
                var fields = SplitSqlFields(cleanRecord);

                if (fields.Count < 9)
                    continue;

                int postgresId = int.Parse(fields[0].Trim());
                string firstName = UnquoteSql(fields[2].Trim());
                string lastName = UnquoteSql(fields[3].Trim());
                string email = UnquoteSql(fields[4].Trim());
                string phone = UnquoteSql(fields[5].Trim());
                string lotNumber = UnquoteSql(fields[6].Trim());

                // Skip if already exists
                if (_db.Players.Any(p => p.FirstName == firstName && p.LastName == lastName))
                    continue;

                var player = new Player
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber,
                    IsActive = true,
                    LookingForTeam = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Players.Add(player);
                playerMap[postgresId] = player;
                importedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse player record: {ex.Message}");
            }
        }

        _db.SaveChanges();
        return playerMap;
    }

    /// <summary>
    /// Splits a SQL record line by commas while respecting quoted strings.
    /// Handles SQL-escaped quotes ('') inside string values.
    /// </summary>
    private List<string> SplitSqlFields(string record)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < record.Length; i++)
        {
            char c = record[i];

            if (c == '\'' && i + 1 < record.Length && record[i + 1] == '\'')
            {
                // SQL-escaped quote: ''
                currentField.Append("''");
                i++;
            }
            else if (c == '\'')
            {
                inQuotes = !inQuotes;
                currentField.Append(c);
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        if (currentField.Length > 0)
            fields.Add(currentField.ToString());

        return fields;
    }

    /// <summary>
    /// Parses the looking_for_team table from SQL and creates LookingForTeams entries.
    /// </summary>
    private int ParseAndPopulateLookingForTeams(string sqlContent, League league, Season season, Dictionary<int, Player> playerMap)
    {
        // Find the looking_for_team INSERT statement
        var pattern = @"INSERT INTO public\.looking_for_team\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;)";
        var match = Regex.Match(sqlContent, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No looking_for_team data found in backup");
            return 0;
        }

        var valuesBlock = match.Groups[1].Value;
        int createdCount = 0;

        // Split by ),\n\t( to get each record
        var records = Regex.Split(valuesBlock, @"\),\s*\(");

        foreach (var record in records)
        {
            try
            {
                var cleanRecord = record.Trim('(', ')').Trim();
                if (string.IsNullOrWhiteSpace(cleanRecord))
                    continue;

                var fields = SplitSqlFields(cleanRecord);
                if (fields.Count < 2)
                    continue;

                int postgresPlayerId = int.Parse(fields[1].Trim());

                if (!playerMap.TryGetValue(postgresPlayerId, out var player))
                    continue;

                // Skip if already exists
                if (_db.LookingForTeams.Any(l => l.PlayerId == player.Id && l.LeagueId == league.Id))
                    continue;

                var lookingForTeam = new LookingForTeam
                {
                    LeagueId = league.Id,
                    PlayerId = player.Id,
                    TeamId = null
                };

                _db.LookingForTeams.Add(lookingForTeam);
                createdCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse looking_for_team record: {ex.Message}");
            }
        }

        _db.SaveChanges();
        return createdCount;
    }

    /// <summary>
    /// Parses the spare_list_players table from SQL and populates SpareLists.
    /// </summary>
    private int ParseAndPopulateSpareLists(string sqlContent, League league, Dictionary<int, Player> playerMap)
    {
        // Find the spare_list_players INSERT statement
        var pattern = @"INSERT INTO public\.spare_list_players\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;)";
        var match = Regex.Match(sqlContent, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No spare_list_players data found in backup");
            return 0;
        }

        var valuesBlock = match.Groups[1].Value;
        int addedCount = 0;

        // Split by ),\n\t( to get each record
        var records = Regex.Split(valuesBlock, @"\),\s*\(");

        foreach (var record in records)
        {
            try
            {
                var cleanRecord = record.Trim('(', ')').Trim();
                if (string.IsNullOrWhiteSpace(cleanRecord))
                    continue;

                var fields = SplitSqlFields(cleanRecord);
                if (fields.Count < 3)
                    continue;

                int postgresPlayerId = int.Parse(fields[2].Trim());

                if (!playerMap.TryGetValue(postgresPlayerId, out var player))
                    continue;

                // Skip if already in the spare list
                if (_db.SpareLists.Any(sl => sl.PlayerId == player.Id && sl.LeagueId == league.Id))
                    continue;

                var spareListEntry = new SpareList
                {
                    LeagueId = league.Id,
                    PlayerId = player.Id,
                    IsActive = true
                };

                _db.SpareLists.Add(spareListEntry);
                addedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse spare_list_players record: {ex.Message}");
            }
        }

        _db.SaveChanges();
        return addedCount;
    }

    /// <summary>
    /// Removes SQL quotes and handles NULL values.
    /// Also unescapes SQL-escaped quotes ('').
    /// </summary>
    private string UnquoteSql(string value)
    {
        if (value == null || value == "NULL")
            return null;

        if (value.StartsWith("'") && value.EndsWith("'"))
        {
            var unquoted = value.Substring(1, value.Length - 2);
            // Unescape SQL-escaped quotes: '' -> '
            return unquoted.Replace("''", "'");
        }

        return value;
    }
}
