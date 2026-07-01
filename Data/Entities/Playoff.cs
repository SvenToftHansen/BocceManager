namespace BocceManager.Data.Entities;

// ── Playoff configuration — one per season ────────────────────────────────────
public class PlayoffConfig
{
    public int      Id                       { get; set; }
    public int      SeasonId                 { get; set; }
    public int      MatchDurationMins        { get; set; } = 90;
    // ScaleToFit | Scroll
    public string   DisplayMode              { get; set; } = "ScaleToFit";
    public bool     IsGenerated              { get; set; } = false;

    public Season   Season                   { get; set; } = null!;
    public ICollection<PlayoffDayParams> DayParams { get; set; } = [];
}

// ── Per-day scheduling parameters ─────────────────────────────────────────────
public class PlayoffDayParams
{
    public int       Id                        { get; set; }
    public int       PlayoffConfigId           { get; set; }
    public int       DayNumber                 { get; set; }   // 1, 2, 3 …
    public DateOnly  GameDate                  { get; set; }
    public TimeOnly  StartTime                 { get; set; }
    public TimeOnly  EndTime                   { get; set; }
    public int       DurationBetweenRoundsMins { get; set; } = 30;

    public PlayoffConfig PlayoffConfig         { get; set; } = null!;
}

// ── Seeding — ordered list of teams in this season's playoff ─────────────────
public class PlayoffSeeding
{
    public int  Id       { get; set; }
    public int  SeasonId { get; set; }
    public int  Seed     { get; set; }   // 1 = top seed
    public int  TeamId   { get; set; }

    public Season Season { get; set; } = null!;
    public Team   Team   { get; set; } = null!;
}

// ── Playoff round — one row per round (R1, QF, SF, Final …) ──────────────────
public class PlayoffRound
{
    public int      Id             { get; set; }
    public int      SeasonId       { get; set; }
    public int      RoundNumber    { get; set; }
    public string?  RoundName      { get; set; }
    public DateOnly? MatchDate     { get; set; }
    public TimeOnly? StartTime     { get; set; }
    public TimeOnly? EndTime       { get; set; }
    public int      DurationBetweenRoundsMins { get; set; } = 30;

    public Season  Season  { get; set; } = null!;
    public ICollection<PlayoffMatch> Matches { get; set; } = [];
}

// ── Playoff match — one row per bracket slot ──────────────────────────────────
public class PlayoffMatch
{
    public int    Id             { get; set; }
    public int    SeasonId       { get; set; }
    public int?   PlayoffRoundId { get; set; }
    public int    Seed1          { get; set; }
    public int?   Seed2          { get; set; }
    public int?   Team1Id        { get; set; }
    public int?   Team2Id        { get; set; }
    public int?   CourtId        { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }
    // scheduled | completed | bye
    public string  Status        { get; set; } = "scheduled";
    public int?    WinnerId      { get; set; }
    public string? EnteredBy     { get; set; }
    public DateTime? EnteredAt   { get; set; }

    // Bracket structure
    public int    BracketSlot    { get; set; } = 0;   // 0-indexed position within round
    public int?   NextMatchId    { get; set; }         // match the winner advances to
    public bool   NextMatchIsTop { get; set; } = true; // winner fills top (Team1) slot in next match
    public bool   IsBye          { get; set; } = false;

    public Season        Season       { get; set; } = null!;
    public PlayoffRound? PlayoffRound { get; set; }
    public Team?         Team1        { get; set; }
    public Team?         Team2        { get; set; }
    public Court?        Court        { get; set; }
    public Team?         Winner       { get; set; }
    public PlayoffMatch? NextMatch    { get; set; }
    public ICollection<PlayoffGame> Games { get; set; } = [];
}

// ── Individual game scores within a match ─────────────────────────────────────
public class PlayoffGame
{
    public int  Id             { get; set; }
    public int  PlayoffMatchId { get; set; }
    public int  GameNumber     { get; set; }
    public int  Team1Score     { get; set; } = 0;
    public int  Team2Score     { get; set; } = 0;
    public bool IsForfeit      { get; set; } = false;
    public string?   EnteredBy { get; set; }
    public DateTime? EnteredAt { get; set; }

    public PlayoffMatch PlayoffMatch { get; set; } = null!;
}
