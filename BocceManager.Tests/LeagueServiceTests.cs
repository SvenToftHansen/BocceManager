using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using Microsoft.Data.Sqlite;

namespace BocceManager.Tests;

/// <summary>
/// Tests for LeagueService.ComputeDeleteImpact and ExecuteCascadeDelete.
/// Each test gets its own isolated SQLite file through the TestDb helper.
/// Tests in this class run sequentially (xUnit default for a single class).
/// </summary>
public class LeagueServiceTests
{
    // ── ComputeDeleteImpact ───────────────────────────────────────────────────

    [Fact]
    public void ComputeDeleteImpact_LeagueWithNoSeasons_ReturnsAllZeros()
    {
        using var tdb = new TestDb();
        var league = tdb.AddLeague("Empty League");

        var impact = LeagueService.ComputeDeleteImpact(tdb.Db, league.Id);

        Assert.Equal("Empty League", impact.LeagueName);
        Assert.Empty(impact.Seasons);
        Assert.Equal(0, impact.TotalDivisions);
        Assert.Equal(0, impact.TotalTeams);
        Assert.Equal(0, impact.SpareListCount);
        Assert.Equal(0, impact.OfficialCount);
        Assert.Equal(0, impact.AnnouncementCount);
        Assert.Equal(0, impact.EmailListCount);
        Assert.Equal(0, impact.ParameterCount);
    }

    [Fact]
    public void ComputeDeleteImpact_TwoSeasonsWithDivisionsAndTeams_CountsCorrectly()
    {
        using var tdb = new TestDb();
        var league = tdb.AddLeague("Test League");

        // Season 1: 2 divisions × 3 teams each
        var s1 = tdb.AddSeason(league.Id, "2023");
        var d1 = tdb.AddDivision(s1.Id, "Monday AM");
        var d2 = tdb.AddDivision(s1.Id, "Monday PM");
        tdb.AddTeams(d1.Id, 3);
        tdb.AddTeams(d2.Id, 3);

        // Season 2: 1 division × 4 teams
        var s2 = tdb.AddSeason(league.Id, "2024");
        var d3 = tdb.AddDivision(s2.Id, "Tuesday");
        tdb.AddTeams(d3.Id, 4);

        var impact = LeagueService.ComputeDeleteImpact(tdb.Db, league.Id);

        Assert.Equal(2, impact.Seasons.Count);
        Assert.Equal(3, impact.TotalDivisions);
        Assert.Equal(10, impact.TotalTeams);

        var si2023 = impact.Seasons.Single(s => s.Name == "2023");
        Assert.Equal(2, si2023.Divisions);
        Assert.Equal(6, si2023.Teams);

        var si2024 = impact.Seasons.Single(s => s.Name == "2024");
        Assert.Equal(1, si2024.Divisions);
        Assert.Equal(4, si2024.Teams);
    }

    [Fact]
    public void ComputeDeleteImpact_SupportingEntities_CountedCorrectly()
    {
        using var tdb = new TestDb();
        var league = tdb.AddLeague("League With Stuff");

        var player = tdb.AddPlayer("Captain", "Jack");
        var player2 = tdb.AddPlayer("Second", "Spare");
        tdb.Db.SpareLists.Add(new SpareList { LeagueId = league.Id, PlayerId = player.Id });
        tdb.Db.SpareLists.Add(new SpareList { LeagueId = league.Id, PlayerId = player2.Id });
        tdb.Db.LeagueOfficials.Add(new LeagueOfficial
            { LeagueId = league.Id, PlayerId = player.Id, RoleTitle = "Director" });

        tdb.Db.Announcements.Add(new Announcement
            { LeagueId = league.Id, Title = "News", Body = "..." });

        tdb.Db.EmailLists.Add(new EmailList { LeagueId = league.Id, Name = "Members" });

        tdb.Db.LeagueParameters.Add(new LeagueParameter
            { LeagueId = league.Id, Key = "fee", Value = "50" });
        tdb.Db.LeagueParameters.Add(new LeagueParameter
            { LeagueId = league.Id, Key = "max_teams", Value = "16" });

        tdb.Db.SaveChanges();

        var impact = LeagueService.ComputeDeleteImpact(tdb.Db, league.Id);

        Assert.Equal(2, impact.SpareListCount);
        Assert.Equal(1, impact.OfficialCount);
        Assert.Equal(1, impact.AnnouncementCount);
        Assert.Equal(1, impact.EmailListCount);
        Assert.Equal(2, impact.ParameterCount);
    }

    [Fact]
    public void ComputeDeleteImpact_UnknownLeagueId_Throws()
    {
        using var tdb = new TestDb();

        Assert.Throws<InvalidOperationException>(
            () => LeagueService.ComputeDeleteImpact(tdb.Db, 999));
    }

    // ── ExecuteCascadeDelete ──────────────────────────────────────────────────

    [Fact]
    public void ExecuteCascadeDelete_EmptyLeague_RemovesLeague()
    {
        using var tdb = new TestDb();
        var league = tdb.AddLeague("Gone League");

        LeagueService.ExecuteCascadeDelete(tdb.Db, league.Id);

        Assert.Empty(tdb.Db.Leagues);
    }

    [Fact]
    public void ExecuteCascadeDelete_LeagueWithFullHierarchy_RemovesAllData()
    {
        using var tdb = new TestDb();
        var league   = tdb.AddLeague("Full League");
        var season   = tdb.AddSeason(league.Id, "2024");
        var division = tdb.AddDivision(season.Id, "Div A");
        tdb.AddTeams(division.Id, 2);

        tdb.Db.SeasonParameters.Add(new SeasonParameter
            { SeasonId = season.Id, Key = "x", Value = "1" });
        tdb.Db.DivisionParameters.Add(new DivisionParameter
            { DivisionId = division.Id, Key = "y", Value = "2" });
        tdb.Db.LeagueParameters.Add(new LeagueParameter
            { LeagueId = league.Id, Key = "z", Value = "3" });
        tdb.Db.SaveChanges();

        LeagueService.ExecuteCascadeDelete(tdb.Db, league.Id);

        Assert.Empty(tdb.Db.Leagues);
        Assert.Empty(tdb.Db.Seasons);
        Assert.Empty(tdb.Db.Divisions);
        Assert.Empty(tdb.Db.Teams);
        Assert.Empty(tdb.Db.SeasonParameters);
        Assert.Empty(tdb.Db.DivisionParameters);
        Assert.Empty(tdb.Db.LeagueParameters);
    }

    [Fact]
    public void ExecuteCascadeDelete_PlayersNotDeleted()
    {
        using var tdb = new TestDb();
        var league   = tdb.AddLeague("League");
        var season   = tdb.AddSeason(league.Id, "2024");
        var division = tdb.AddDivision(season.Id, "Div A");
        var player   = tdb.AddPlayer("Alice", "Smith");

        var team = new Team
        {
            DivisionId = division.Id,
            TeamLetter = "A",
            SystemName = "A-DIV",
            CaptainPlayerId = player.Id
        };
        tdb.Db.Teams.Add(team);
        tdb.Db.SaveChanges();

        tdb.Db.TeamPlayers.Add(new TeamPlayer
            { TeamId = team.Id, PlayerId = player.Id, Role = "captain" });
        tdb.Db.SaveChanges();

        LeagueService.ExecuteCascadeDelete(tdb.Db, league.Id);

        // League and team gone — player stays
        Assert.Empty(tdb.Db.Leagues);
        Assert.Empty(tdb.Db.Teams);
        Assert.Single(tdb.Db.Players);
    }

    [Fact]
    public void ExecuteCascadeDelete_CaptainFkClearedBeforeTeamDelete_NoException()
    {
        // This test verifies the Restrict FK on Team.CaptainPlayerId is handled.
        // If the service forgets to null it out first, EF will throw.
        using var tdb = new TestDb();
        var league   = tdb.AddLeague("League");
        var season   = tdb.AddSeason(league.Id, "2024");
        var division = tdb.AddDivision(season.Id, "Div");
        var player   = tdb.AddPlayer("Bob", "Jones");

        var team = new Team
        {
            DivisionId = division.Id,
            TeamLetter = "A",
            SystemName = "A-DIV",
            CaptainPlayerId = player.Id
        };
        tdb.Db.Teams.Add(team);
        tdb.Db.SaveChanges();

        // Should not throw
        var ex = Record.Exception(
            () => LeagueService.ExecuteCascadeDelete(tdb.Db, league.Id));

        Assert.Null(ex);
    }

    [Fact]
    public void ExecuteCascadeDelete_OtherLeagueUnaffected()
    {
        using var tdb = new TestDb();
        var leagueA = tdb.AddLeague("League A");
        var leagueB = tdb.AddLeague("League B");

        tdb.AddSeason(leagueA.Id, "2024 A");
        tdb.AddSeason(leagueB.Id, "2024 B");

        LeagueService.ExecuteCascadeDelete(tdb.Db, leagueA.Id);

        Assert.Single(tdb.Db.Leagues);
        Assert.Equal("League B", tdb.Db.Leagues.Single().Name);
        Assert.Single(tdb.Db.Seasons);
        Assert.Equal("2024 B", tdb.Db.Seasons.Single().Name);
    }

    [Fact]
    public void ExecuteCascadeDelete_SupportingEntitiesRemoved()
    {
        using var tdb = new TestDb();
        var league = tdb.AddLeague("League");
        var player = tdb.AddPlayer("Sam", "Lee");

        tdb.Db.SpareLists.Add(new SpareList { LeagueId = league.Id, PlayerId = player.Id });
        tdb.Db.Announcements.Add(new Announcement
            { LeagueId = league.Id, Title = "Hi", Body = "." });
        tdb.Db.EmailLists.Add(new EmailList { LeagueId = league.Id, Name = "EL" });
        tdb.Db.LeagueOfficials.Add(new LeagueOfficial
            { LeagueId = league.Id, PlayerId = player.Id, RoleTitle = "Captain" });
        tdb.Db.LeagueParameters.Add(new LeagueParameter
            { LeagueId = league.Id, Key = "k", Value = "v" });
        tdb.Db.SaveChanges();

        LeagueService.ExecuteCascadeDelete(tdb.Db, league.Id);

        Assert.Empty(tdb.Db.SpareLists);
        Assert.Empty(tdb.Db.Announcements);
        Assert.Empty(tdb.Db.EmailLists);
        Assert.Empty(tdb.Db.LeagueOfficials);
        Assert.Empty(tdb.Db.LeagueParameters);
        // Player still present
        Assert.Single(tdb.Db.Players);
    }

    // ── Test Helper ───────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fresh isolated SQLite database for one test, seeds it via
    /// EnsureCreated, and deletes the file on Dispose.
    /// </summary>
    private sealed class TestDb : IDisposable
    {
        private readonly string _path;
        public BocceDbContext Db { get; }

        public TestDb()
        {
            _path = Path.Combine(Path.GetTempPath(), $"bocce_test_{Guid.NewGuid():N}.db");
            BocceDbContext.DbPath = _path;
            Db = new BocceDbContext();
            Db.Database.EnsureCreated();
        }

        public League AddLeague(string name)
        {
            var l = new League { Name = name };
            Db.Leagues.Add(l);
            Db.SaveChanges();
            return l;
        }

        public Season AddSeason(int leagueId, string name)
        {
            var s = new Season { LeagueId = leagueId, Name = name };
            Db.Seasons.Add(s);
            Db.SaveChanges();
            return s;
        }

        public Division AddDivision(int seasonId, string name)
        {
            var d = new Division { SeasonId = seasonId, Name = name };
            Db.Divisions.Add(d);
            Db.SaveChanges();
            return d;
        }

        public void AddTeams(int divisionId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var letter = ((char)('A' + i)).ToString();
                Db.Teams.Add(new Team
                {
                    DivisionId = divisionId,
                    TeamLetter = letter,
                    SystemName = $"{letter}-DIV"
                });
            }
            Db.SaveChanges();
        }

        public Player AddPlayer(string first, string last)
        {
            var p = new Player { FirstName = first, LastName = last };
            Db.Players.Add(p);
            Db.SaveChanges();
            return p;
        }

        public void Dispose()
        {
            Db.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_path); } catch { /* best effort */ }
        }
    }
}
