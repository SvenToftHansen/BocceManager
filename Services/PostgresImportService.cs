using Npgsql;
using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public class PostgresImportService
{
    private const string PostgresConnString = "Host=localhost;Port=5432;Database=bocce_league;Username=postgres;Password=7720";

    public async Task<List<PostgresPlayerPreview>> PreviewPlayersAsync()
    {
        var players = new List<PostgresPlayerPreview>();
        var sparePlayerIds = new HashSet<int>();
        var lookingPlayerIds = new HashSet<int>();

        try
        {
            using (var conn = new NpgsqlConnection(PostgresConnString))
            {
                await conn.OpenAsync();

                // Get spare list players
                var spareCmd = conn.CreateCommand();
                spareCmd.CommandText = "SELECT \"playerId\" FROM public.spare_list_players";
                using (var reader = await spareCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        sparePlayerIds.Add(reader.GetInt32(0));
                    }
                }

                // Get looking for team players
                var lftCmd = conn.CreateCommand();
                lftCmd.CommandText = "SELECT \"playerId\" FROM public.looking_for_team";
                using (var reader = await lftCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lookingPlayerIds.Add(reader.GetInt32(0));
                    }
                }

                // Get all players
                var playerCmd = conn.CreateCommand();
                playerCmd.CommandText = @"
                    SELECT id, ""firstName"", ""lastName"", email, phone, ""lotNumber"", ""isActive""
                    FROM public.players
                    ORDER BY ""lastName"", ""firstName""";

                using (var reader = await playerCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var playerId = reader.GetInt32(0);
                        players.Add(new PostgresPlayerPreview
                        {
                            Id = playerId,
                            FirstName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            LastName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                            LotNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsSpare = sparePlayerIds.Contains(playerId),
                            LookingForTeam = lookingPlayerIds.Contains(playerId),
                            IsActive = reader.IsDBNull(6) ? true : reader.GetBoolean(6)
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to preview players from PostgreSQL: {ex.Message}", ex);
        }

        return players;
    }

    public async Task<(int playersImported, int spareListAdded, int lookingForTeamAdded)> ImportPlayersAsync(
        BocceDbContext sqliteDb, int? targetLeagueId = null)
    {
        // If no league specified, auto-find Spring League
        if (!targetLeagueId.HasValue)
        {
            var springLeague = sqliteDb.Leagues.FirstOrDefault(l => l.Name == "Spring League");
            if (springLeague == null)
                throw new Exception("Spring League not found in database. Please create it first or specify a targetLeagueId");
            targetLeagueId = springLeague.Id;
        }

        var postgresPlayers = await PreviewPlayersAsync();
        int playersImported = 0;
        int spareListAdded = 0;
        int lookingForTeamAdded = 0;

        foreach (var pgPlayer in postgresPlayers)
        {
            // Check if player already exists
            var existing = sqliteDb.Players.FirstOrDefault(p =>
                p.FirstName == pgPlayer.FirstName && p.LastName == pgPlayer.LastName);

            Player sqlitePlayer;
            if (existing == null)
            {
                // Create new player
                sqlitePlayer = new Player
                {
                    FirstName = pgPlayer.FirstName,
                    LastName = pgPlayer.LastName,
                    Email = pgPlayer.Email,
                    Phone = pgPlayer.Phone,
                    LotNumber = pgPlayer.LotNumber,
                    IsActive = pgPlayer.IsActive
                };
                sqliteDb.Players.Add(sqlitePlayer);
                await sqliteDb.SaveChangesAsync();
                playersImported++;
            }
            else
            {
                sqlitePlayer = existing;
            }

            // Add to SpareList if marked as spare
            if (pgPlayer.IsSpare)
            {
                var existing_spare = sqliteDb.SpareLists.FirstOrDefault(s =>
                    s.LeagueId == targetLeagueId && s.PlayerId == sqlitePlayer.Id);
                if (existing_spare == null)
                {
                    sqliteDb.SpareLists.Add(new SpareList
                    {
                        LeagueId = targetLeagueId.Value,
                        PlayerId = sqlitePlayer.Id,
                        IsActive = true
                    });
                    spareListAdded++;
                }
            }

            // Add to LookingForTeam if marked as looking_for_team
            if (pgPlayer.LookingForTeam)
            {
                var existing_lft = sqliteDb.LookingForTeams.FirstOrDefault(l =>
                    l.LeagueId == targetLeagueId && l.PlayerId == sqlitePlayer.Id);
                if (existing_lft == null)
                {
                    sqliteDb.LookingForTeams.Add(new LookingForTeam
                    {
                        LeagueId = targetLeagueId.Value,
                        PlayerId = sqlitePlayer.Id
                    });
                    lookingForTeamAdded++;
                }
            }
        }

        // Save all changes at once
        await sqliteDb.SaveChangesAsync();

        return (playersImported, spareListAdded, lookingForTeamAdded);
    }
}

public class PostgresPlayerPreview
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LotNumber { get; set; }
    public bool IsSpare { get; set; }
    public bool LookingForTeam { get; set; }
    public bool IsActive { get; set; }
}
