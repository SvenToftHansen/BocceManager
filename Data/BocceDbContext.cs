using Microsoft.EntityFrameworkCore;
using BocceManager.Data.Entities;

namespace BocceManager.Data;

public class BocceDbContext : DbContext
{
    // PostgreSQL connection string
    private const string PostgresConnString = "Host=localhost;Port=5432;Database=bocce_league;Username=postgres;Password=7720";

    // Test mode support
    public static string? DbPath { get; set; }

    // Reference / Lookup
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<DaySlot> DaySlots => Set<DaySlot>();

    // Parameters
    public DbSet<AppParameter>    AppParameters    => Set<AppParameter>();
    public DbSet<LeagueParameter> LeagueParameters => Set<LeagueParameter>();
    public DbSet<SeasonParameter> SeasonParameters => Set<SeasonParameter>();

    // Players
    public DbSet<Player>        Players        => Set<Player>();
    public DbSet<InitiationFee> InitiationFees => Set<InitiationFee>();
    public DbSet<SeasonFee>     SeasonFees     => Set<SeasonFee>();

    // Leagues
    public DbSet<League>                Leagues                => Set<League>();
    public DbSet<SpareList>             SpareLists             => Set<SpareList>();
    public DbSet<LookingForTeam>        LookingForTeams        => Set<LookingForTeam>();
    public DbSet<LookingForTeamGroup>   LookingForTeamGroups   => Set<LookingForTeamGroup>();
    public DbSet<LookingForTeamDivision> LookingForTeamDivisions => Set<LookingForTeamDivision>();
    public DbSet<LookingForTeamPreferredDay> LookingForTeamPreferredDays => Set<LookingForTeamPreferredDay>();
    public DbSet<LookingForTeamPreferredTime> LookingForTeamPreferredTimes => Set<LookingForTeamPreferredTime>();

    // Team Applicants
    public DbSet<TeamApplicant>       TeamApplicants       => Set<TeamApplicant>();
    public DbSet<TeamApplicantMember> TeamApplicantMembers => Set<TeamApplicantMember>();

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

    // Schedule Templates
    public DbSet<ScheduleTemplate>      ScheduleTemplates      => Set<ScheduleTemplate>();
    public DbSet<ScheduleTemplateWeek>  ScheduleTemplateWeeks  => Set<ScheduleTemplateWeek>();
    public DbSet<ScheduleTemplateMatch> ScheduleTemplateMatches => Set<ScheduleTemplateMatch>();
    public DbSet<ScheduleDivision>      ScheduleDivisions      => Set<ScheduleDivision>();

    // Standings
    public DbSet<TeamStanding> TeamStandings => Set<TeamStanding>();

    // Other
    public DbSet<Announcement> Announcements => Set<Announcement>();

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

    // Ideas
    public DbSet<NewIdea> NewIdeas => Set<NewIdea>();

    // Reports
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportParameter> ReportParameters => Set<ReportParameter>();

    // Views
    public DbSet<Stats> Stats => Set<Stats>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!string.IsNullOrEmpty(DbPath))
        {
            // Test mode: use SQLite
            options.UseSqlite($"Data Source={DbPath}");
        }
        else
        {
            // Production: use PostgreSQL
            options.UseNpgsql(PostgresConnString);
        }
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Unique indexes
        model.Entity<AppParameter>().HasIndex(e => e.Key).IsUnique();
        model.Entity<LeagueParameter>().HasIndex(e => new { e.LeagueId, e.Key }).IsUnique();
        model.Entity<SeasonParameter>().HasIndex(e => new { e.SeasonId, e.Key }).IsUnique();
model.Entity<SeasonCourt>().HasIndex(e => new { e.SeasonId, e.CourtId }).IsUnique();
        model.Entity<SeasonDaySlot>().HasIndex(e => new { e.SeasonId, e.DaySlotId }).IsUnique();
        model.Entity<SeasonTimeSlot>().HasIndex(e => new { e.SeasonId, e.TimeSlotId }).IsUnique();
        model.Entity<SpareList>().HasIndex(e => new { e.LeagueId, e.PlayerId }).IsUnique();
        model.Entity<LookingForTeam>().HasIndex(e => new { e.LeagueId, e.PlayerId, e.SeasonId }).IsUnique();
        model.Entity<SeasonFee>().HasIndex(e => new { e.PlayerId, e.SeasonId }).IsUnique();
        model.Entity<Team>().HasIndex(e => new { e.DivisionId, e.TeamLetter }).IsUnique();
        model.Entity<TeamPlayer>().HasIndex(e => new { e.TeamId, e.PlayerId }).IsUnique();
        model.Entity<LookingForTeamGroup>().HasIndex(e => new { e.LeagueId, e.SeasonId, e.Name }).IsUnique();
        model.Entity<TeamStanding>().HasIndex(e => new { e.TeamId, e.DivisionId }).IsUnique();
        model.Entity<ScheduleTemplate>().HasIndex(e => new { e.SeasonId, e.TeamCount }).IsUnique();
        model.Entity<EmailListMember>().HasIndex(e => new { e.EmailListId, e.PlayerId }).IsUnique();

        // Optional spouse/partner link between players (self-reference)
        model.Entity<Player>()
            .HasOne<Player>()
            .WithMany()
            .HasForeignKey(e => e.PartnerPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Report: unique key on Name
        model.Entity<Report>().HasIndex(e => e.Name).IsUnique();
        // ReportParameter: unique on ReportId + ParameterName
        model.Entity<ReportParameter>().HasIndex(e => new { e.ReportId, e.ParameterName }).IsUnique();

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
        model.Entity<LookingForTeam>()
            .HasOne(e => e.Season).WithMany()
            .HasForeignKey(e => e.SeasonId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<LookingForTeam>()
            .HasOne(e => e.Team).WithMany()
            .HasForeignKey(e => e.TeamId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<LookingForTeam>()
            .HasOne(e => e.PreferredTeam).WithMany()
            .HasForeignKey(e => e.PreferredTeamId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<LookingForTeam>()
            .HasOne(e => e.Group).WithMany(e => e.Members)
            .HasForeignKey(e => e.LookingForTeamGroupId).OnDelete(DeleteBehavior.SetNull);

        // LookingForTeamGroup FKs
        model.Entity<LookingForTeamGroup>()
            .HasOne(e => e.League).WithMany()
            .HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<LookingForTeamGroup>()
            .HasOne(e => e.Season).WithMany()
            .HasForeignKey(e => e.SeasonId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<LookingForTeamGroup>()
            .HasOne(e => e.Leader).WithMany()
            .HasForeignKey(e => e.GroupLeaderId).OnDelete(DeleteBehavior.SetNull);

        // LookingForTeamDivision FKs
        model.Entity<LookingForTeamDivision>()
            .HasIndex(e => new { e.LookingForTeamId, e.DivisionId }).IsUnique();
        model.Entity<LookingForTeamDivision>()
            .HasOne(e => e.LookingForTeam).WithMany(e => e.PreferredDivisions)
            .HasForeignKey(e => e.LookingForTeamId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<LookingForTeamDivision>()
            .HasOne(e => e.Division).WithMany()
            .HasForeignKey(e => e.DivisionId).OnDelete(DeleteBehavior.Cascade);

        // LookingForTeamPreferredDay FKs
        model.Entity<LookingForTeamPreferredDay>()
            .HasIndex(e => new { e.LookingForTeamId, e.DaySlotId }).IsUnique();
        model.Entity<LookingForTeamPreferredDay>()
            .HasOne(e => e.LookingForTeam).WithMany(e => e.PreferredDays)
            .HasForeignKey(e => e.LookingForTeamId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<LookingForTeamPreferredDay>()
            .HasOne(e => e.DaySlot).WithMany()
            .HasForeignKey(e => e.DaySlotId).OnDelete(DeleteBehavior.Cascade);

        // LookingForTeamPreferredTime FKs
        model.Entity<LookingForTeamPreferredTime>()
            .HasIndex(e => new { e.LookingForTeamId, e.TimeSlotId }).IsUnique();
        model.Entity<LookingForTeamPreferredTime>()
            .HasOne(e => e.LookingForTeam).WithMany(e => e.PreferredTimes)
            .HasForeignKey(e => e.LookingForTeamId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<LookingForTeamPreferredTime>()
            .HasOne(e => e.TimeSlot).WithMany()
            .HasForeignKey(e => e.TimeSlotId).OnDelete(DeleteBehavior.Cascade);

        // TeamApplicant FKs
        model.Entity<TeamApplicant>()
            .HasOne(e => e.League).WithMany()
            .HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TeamApplicant>()
            .HasOne(e => e.Season).WithMany()
            .HasForeignKey(e => e.SeasonId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<TeamApplicant>()
            .HasOne(e => e.PreferredDivision).WithMany()
            .HasForeignKey(e => e.PreferredDivisionId).OnDelete(DeleteBehavior.SetNull);
        model.Entity<TeamApplicant>()
            .HasOne(e => e.PlacedTeam).WithMany()
            .HasForeignKey(e => e.PlacedTeamId).OnDelete(DeleteBehavior.SetNull);

        // TeamApplicantMember FKs
        model.Entity<TeamApplicantMember>()
            .HasOne(e => e.TeamApplicant).WithMany(e => e.Members)
            .HasForeignKey(e => e.TeamApplicantId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<TeamApplicantMember>()
            .HasOne(e => e.Player).WithMany()
            .HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<TeamApplicantMember>()
            .HasOne(e => e.CreatedPlayer).WithMany()
            .HasForeignKey(e => e.CreatedPlayerId).OnDelete(DeleteBehavior.SetNull);

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

        // Stats view
        model.Entity<Stats>().ToView("Stats").HasNoKey();
    }
}
