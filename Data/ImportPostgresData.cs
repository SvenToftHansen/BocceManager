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
    /// </summary>
    private Dictionary<int, Player> ParseAndImportPlayers(string sqlContent)
    {
        var playerMap = new Dictionary<int, Player>();

        // Find the players INSERT statement
        var playerPattern = @"INSERT INTO public\.players\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;|\n\n)";
        var match = Regex.Match(sqlContent, playerPattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No players data found in backup");
            return playerMap;
        }

        var valuesBlock = match.Groups[1].Value;

        // Parse individual player records: (id, parkId, firstName, lastName, email, phone, lotNumber, isActive, createdAt)
        var recordPattern = @"\(\s*(\d+),\s*(\d+),\s*'([^']*)',\s*'([^']*)',\s*(NULL|'[^']*'),\s*(NULL|'[^']*'),\s*(NULL|'[^']*'),\s*(true|false),\s*'([^']+)'\s*\)";
        var recordMatches = Regex.Matches(valuesBlock, recordPattern);

        int importedCount = 0;

        foreach (Match recordMatch in recordMatches)
        {
            try
            {
                int postgresId = int.Parse(recordMatch.Groups[1].Value);
                string firstName = UnquoteSql(recordMatch.Groups[3].Value);
                string lastName = UnquoteSql(recordMatch.Groups[4].Value);
                string email = UnquoteSql(recordMatch.Groups[5].Value);
                string phone = UnquoteSql(recordMatch.Groups[6].Value);
                string lotNumber = UnquoteSql(recordMatch.Groups[7].Value);

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
    /// Parses the looking_for_team table from SQL and creates LookingForTeams entries.
    /// </summary>
    private int ParseAndPopulateLookingForTeams(string sqlContent, League league, Season season, Dictionary<int, Player> playerMap)
    {
        // Find the looking_for_team INSERT statement
        var pattern = @"INSERT INTO public\.looking_for_team\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;|\n\n)";
        var match = Regex.Match(sqlContent, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No looking_for_team data found in backup");
            return 0;
        }

        var valuesBlock = match.Groups[1].Value;
        int createdCount = 0;

        // Parse individual records: (id, playerId, leagueId, seasonId, addedAt, notes)
        var recordPattern = @"\(\s*\d+,\s*(\d+),\s*\d+,\s*NULL,\s*'[^']+',\s*(NULL|'[^']*')\s*\)";
        var recordMatches = Regex.Matches(valuesBlock, recordPattern);

        foreach (Match recordMatch in recordMatches)
        {
            try
            {
                int postgresPlayerId = int.Parse(recordMatch.Groups[1].Value);

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
        var pattern = @"INSERT INTO public\.spare_list_players\s*\([^)]+\)\s*VALUES\s*([\s\S]+?)(?:;|\n\n)";
        var match = Regex.Match(sqlContent, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Console.WriteLine("Warning: No spare_list_players data found in backup");
            return 0;
        }

        var valuesBlock = match.Groups[1].Value;
        int addedCount = 0;

        // Parse individual records: (id, spareListId, playerId, isActive, addedDate, notes)
        var recordPattern = @"\(\s*\d+,\s*\d+,\s*(\d+),\s*(true|false),\s*'[^']+',\s*(NULL|'[^']*')\s*\)";
        var recordMatches = Regex.Matches(valuesBlock, recordPattern);

        foreach (Match recordMatch in recordMatches)
        {
            try
            {
                int postgresPlayerId = int.Parse(recordMatch.Groups[1].Value);

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
    /// </summary>
    private string UnquoteSql(string value)
    {
        if (value == null || value == "NULL")
            return null;

        if (value.StartsWith("'") && value.EndsWith("'"))
            return value.Substring(1, value.Length - 2);

        return value;
    }
}
