using Microsoft.EntityFrameworkCore;
using BocceManager.Data.Entities;

namespace BocceManager.Data;

public class BocceDbContext : DbContext
{
    // PostgreSQL connection string
    private const string PostgresConnString = "Host=localhost;Port=5432;Database=bocce_league;Username=postgres;Password=7720";

    // Reference / Lookup
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<DaySlot> DaySlots => Set<DaySlot>();

    // Parameters
    public DbSet<AppParameter> AppParameters => Set<AppParameter>();
    public DbSet<LeagueParameter> LeagueParameters => Set<LeagueParameter>();
    public DbSet<SeasonParameter> SeasonParameters => Set<SeasonParameter>();
    public DbSet<DivisionParameter> DivisionParameters => Set<DivisionParameter>();
    public DbSet<TeamParameter> TeamParameters => Set<TeamParameter>();
    public DbSet<PlayerParameter> PlayerParameters => Set<PlayerParameter>();

    // Players
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PendingPlayer> PendingPlayers => Set<PendingPlayer>();
    public DbSet<InitiationFee> InitiationFees => Set<InitiationFee>();
    public DbSet<SeasonFee> SeasonFees => Set<SeasonFee>();

    // Leagues
    public DbSet<League> Leagues => Set<League>();
    public DbSet<SpareList>      SpareLists      => Set<SpareList>();
    public DbSet<LookingForTeam> LookingForTeams => Set<LookingForTeam>();
    public DbSet<LeagueOfficial> LeagueOfficials => Set<LeagueOfficial>();

    // Team Applications (teams seeking placement in a division)
    public DbSet<TeamApplicant>         TeamApplicants         => Set<TeamApplicant>();
    public DbSet<TeamApplicantPlayer>   TeamApplicantPlayers   => Set<TeamApplicantPlayer>();
    public DbSet<TeamApplicantDaySlot>  TeamApplicantDaySlots  => Set<TeamApplicantDaySlot>();
    public DbSet<TeamApplicantTimeSlot> TeamApplicantTimeSlots => Set<TeamApplicantTimeSlot>();

    // Seasons
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SeasonCourt>    SeasonCourts    => Set<SeasonCourt>();
    public DbSet<SeasonDaySlot>  SeasonDaySlots  => Set<SeasonDaySlot>();
    public DbSet<SeasonTimeSlot> SeasonTimeSlots => Set<SeasonTimeSlot>();

    // Divisions
    public DbSet<Division> Divisions => Set<Division>();

    // Teams
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();

    // Schedule
    public DbSet<ScheduleWeek> ScheduleWeeks => Set<ScheduleWeek>();
    public DbSet<BocceMatch> Matches => Set<BocceMatch>();
    public DbSet<MatchTeamResult> MatchTeamResults => Set<MatchTeamResult>();
    public DbSet<Game> Games => Set<Game>();

    // Standings
    public DbSet<TeamStanding> TeamStandings => Set<TeamStanding>();

    // Other
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<SpareRequest> SpareRequests => Set<SpareRequest>();

    // Email
    public DbSet<EmailList> EmailLists => Set<EmailList>();
    public DbSet<EmailListMember> EmailListMembers => Set<EmailListMember>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    // Playoffs
    public DbSet<PlayoffRound> PlayoffRounds => Set<PlayoffRound>();
    public DbSet<PlayoffMatch> PlayoffMatches => Set<PlayoffMatch>();
    public DbSet<PlayoffGame> PlayoffGames => Set<PlayoffGame>();

    // Documents
    public DbSet<ClubDocument> ClubDocuments => Set<ClubDocument>();

    // Finance
    public DbSet<GlAccount>    GlAccounts    => Set<GlAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql(PostgresConnString);

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Unique indexes
        model.Entity<AppParameter>().HasIndex(e => e.Key).IsUnique();
        model.Entity<LeagueParameter>().HasIndex(e => new { e.LeagueId, e.Key }).IsUnique();
        model.Entity<SeasonParameter>().HasIndex(e => new { e.SeasonId, e.Key }).IsUnique();
        model.Entity<DivisionParameter>().HasIndex(e => new { e.DivisionId, e.Key }).IsUnique();
        model.Entity<TeamParameter>().HasIndex(e => new { e.TeamId, e.Key }).IsUnique();
        model.Entity<PlayerParameter>().HasIndex(e => new { e.PlayerId, e.Key }).IsUnique();
        model.Entity<SeasonCourt>().HasIndex(e => new { e.SeasonId, e.CourtId }).IsUnique();
        model.Entity<SeasonDaySlot>().HasIndex(e => new { e.SeasonId, e.DaySlotId }).IsUnique();
        model.Entity<SeasonTimeSlot>().HasIndex(e => new { e.SeasonId, e.TimeSlotId }).IsUnique();
        model.Entity<SpareList>().HasIndex(e => new { e.LeagueId, e.PlayerId }).IsUnique();
        model.Entity<LookingForTeam>().HasIndex(e => new { e.LeagueId, e.PlayerId }).IsUnique();
        model.Entity<SeasonFee>().HasIndex(e => new { e.PlayerId, e.SeasonId }).IsUnique();
        model.Entity<Team>().HasIndex(e => new { e.DivisionId, e.TeamLetter }).IsUnique();
        model.Entity<TeamPlayer>().HasIndex(e => new { e.TeamId, e.PlayerId }).IsUnique();
        model.Entity<TeamStanding>().HasIndex(e => new { e.TeamId, e.DivisionId }).IsUnique();
        model.Entity<EmailListMember>().HasIndex(e => new { e.EmailListId, e.PlayerId }).IsUnique();
        model.Entity<LeagueOfficial>().HasIndex(e => new { e.LeagueId, e.PlayerId }).IsUnique();

        // GlAccount unique code
        model.Entity<GlAccount>().HasIndex(e => e.Code).IsUnique();

        // JournalEntry: two FKs to GlAccount
        model.Entity<JournalEntry>()
            .HasOne(e => e.DebitAccount).WithMany(a => a.DebitEntries)
            .HasForeignKey(e => e.DebitAccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<JournalEntry>()
            .HasOne(e => e.CreditAccount).WithMany(a => a.CreditEntries)
            .HasForeignKey(e => e.CreditAccountId).OnDelete(DeleteBehavior.Restrict);

        // BocceMatch: multiple FKs to Team
        model.Entity<BocceMatch>()
            .HasOne(e => e.Team1).WithMany()
            .HasForeignKey(e => e.Team1Id).OnDelete(DeleteBehavior.Restrict);
        model.Entity<BocceMatch>()
            .HasOne(e => e.Team2).WithMany()
            .HasForeignKey(e => e.Team2Id).OnDelete(DeleteBehavior.Restrict);

        // Team captain FK
        model.Entity<Team>()
            .HasOne(e => e.Captain).WithMany()
            .HasForeignKey(e => e.CaptainPlayerId).OnDelete(DeleteBehavior.Restrict);

        // SpareList / LookingForTeam: FK to Player
        model.Entity<SpareList>()
            .HasOne(e => e.Player).WithMany()
            .HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<LookingForTeam>()
            .HasOne(e => e.Player).WithMany()
            .HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        // LookingForTeam.TeamId: cleared (not deleted) when the team is deleted
        model.Entity<LookingForTeam>()
            .HasOne(e => e.Team).WithMany()
            .HasForeignKey(e => e.TeamId).OnDelete(DeleteBehavior.SetNull);

        // TeamApplicant unique indexes
        model.Entity<TeamApplicantPlayer>()
            .HasIndex(e => new { e.TeamApplicantId, e.PlayerId }).IsUnique();
        model.Entity<TeamApplicantDaySlot>()
            .HasIndex(e => new { e.TeamApplicantId, e.DaySlotId }).IsUnique();
        model.Entity<TeamApplicantTimeSlot>()
            .HasIndex(e => new { e.TeamApplicantId, e.TimeSlotId }).IsUnique();

        // TeamApplicant FKs
        model.Entity<TeamApplicantPlayer>()
            .HasOne(e => e.Player).WithMany()
            .HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TeamApplicantDaySlot>()
            .HasOne(e => e.DaySlot).WithMany()
            .HasForeignKey(e => e.DaySlotId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TeamApplicantTimeSlot>()
            .HasOne(e => e.TimeSlot).WithMany()
            .HasForeignKey(e => e.TimeSlotId).OnDelete(DeleteBehavior.Restrict);
        // AssignedDivisionId: cleared when the division is deleted
        model.Entity<TeamApplicant>()
            .HasOne(e => e.AssignedDivision).WithMany()
            .HasForeignKey(e => e.AssignedDivisionId).OnDelete(DeleteBehavior.SetNull);

        // SpareRequest: three FKs (two to Player, one to SpareList)
        model.Entity<SpareRequest>()
            .HasOne(e => e.SpareList).WithMany()
            .HasForeignKey(e => e.SpareListId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SpareRequest>()
            .HasOne(e => e.RequestingPlayer).WithMany()
            .HasForeignKey(e => e.RequestingPlayerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SpareRequest>()
            .HasOne(e => e.SparePlayer).WithMany()
            .HasForeignKey(e => e.SparePlayerId).OnDelete(DeleteBehavior.Restrict);

        // PlayoffMatch: three FKs to Team
        model.Entity<PlayoffMatch>()
            .HasOne(e => e.Team1).WithMany()
            .HasForeignKey(e => e.Team1Id).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PlayoffMatch>()
            .HasOne(e => e.Team2).WithMany()
            .HasForeignKey(e => e.Team2Id).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PlayoffMatch>()
            .HasOne(e => e.Winner).WithMany()
            .HasForeignKey(e => e.WinnerId).OnDelete(DeleteBehavior.Restrict);
    }
}
