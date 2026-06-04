using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClubDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    DocType = table.Column<string>(type: "text", nullable: false),
                    GoogleDocsUrl = table.Column<string>(type: "text", nullable: true),
                    LeagueId = table.Column<int>(type: "integer", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourtName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DaySlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DayName = table.Column<string>(type: "text", nullable: false),
                    DayAbbr = table.Column<string>(type: "text", nullable: false),
                    DayNbr = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaySlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RulesText = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayersPerTeamMinimum = table.Column<int>(type: "integer", nullable: true),
                    PlayersPerTeamMaximum = table.Column<int>(type: "integer", nullable: true),
                    MaxTeamsInDivision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    LotNumber = table.Column<string>(type: "text", nullable: true),
                    Preference = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPlayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    LotNumber = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LookingForTeam = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timeslot12h = table.Column<string>(type: "text", nullable: false),
                    Timeslot24h = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DebitAccountId = table.Column<int>(type: "integer", nullable: false),
                    CreditAccountId = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_GlAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntries_GlAccounts_DebitAccountId",
                        column: x => x.DebitAccountId,
                        principalTable: "GlAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Announcements_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLists_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SentBy = table.Column<string>(type: "text", nullable: true),
                    LeagueId = table.Column<int>(type: "integer", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    RecipientCount = table.Column<int>(type: "integer", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogs_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeagueParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueParameters_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    GamesPerSeason = table.Column<int>(type: "integer", nullable: false),
                    GameInterval = table.Column<string>(type: "text", nullable: false),
                    TimeslotDriven = table.Column<bool>(type: "boolean", nullable: false),
                    PlayersPerTeamMinimum = table.Column<int>(type: "integer", nullable: true),
                    PlayersPerTeamMaximum = table.Column<int>(type: "integer", nullable: true),
                    PointsForWin = table.Column<int>(type: "integer", nullable: false),
                    PointsForTie = table.Column<int>(type: "integer", nullable: false),
                    PointsForLoss = table.Column<int>(type: "integer", nullable: false),
                    PointsForNoShow = table.Column<int>(type: "integer", nullable: false),
                    PointsToWinGame = table.Column<int>(type: "integer", nullable: false),
                    GamesPerMatch = table.Column<int>(type: "integer", nullable: false),
                    ScoringMode = table.Column<string>(type: "text", nullable: false),
                    TeamsInPlayoffs = table.Column<int>(type: "integer", nullable: false),
                    FirstPlaceGuaranteed = table.Column<bool>(type: "boolean", nullable: false),
                    PlayoffType = table.Column<string>(type: "text", nullable: false),
                    PlayoffGamesPerMatch = table.Column<int>(type: "integer", nullable: false),
                    PlayoffScoringMode = table.Column<string>(type: "text", nullable: false),
                    PlayoffTiebreakerEnd = table.Column<bool>(type: "boolean", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WeeksInSeason = table.Column<int>(type: "integer", nullable: false),
                    MaxTeamsInDivision = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PlayoffStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PlayoffEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seasons_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InitiationFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    AmountOwing = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiationFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiationFees_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeagueOfficials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    RoleTitle = table.Column<string>(type: "text", nullable: false),
                    ReceivesSpareRequests = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivesRegistrations = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueOfficials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeagueOfficials_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueOfficials_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerParameters_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpareLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpareLists_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpareLists_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailListMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailListId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailListMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailListMembers_EmailLists_EmailListId",
                        column: x => x.EmailListId,
                        principalTable: "EmailLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailListMembers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShortName = table.Column<string>(type: "text", nullable: false),
                    SortName = table.Column<string>(type: "text", nullable: false),
                    PlayersPerTeamMinimum = table.Column<int>(type: "integer", nullable: true),
                    PlayersPerTeamMaximum = table.Column<int>(type: "integer", nullable: true),
                    TeamsInDivision = table.Column<int>(type: "integer", nullable: false),
                    DaySlotId = table.Column<int>(type: "integer", nullable: true),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_DaySlots_DaySlotId",
                        column: x => x.DaySlotId,
                        principalTable: "DaySlots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Divisions_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Divisions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayoffRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    RoundName = table.Column<string>(type: "text", nullable: true),
                    MatchDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffRounds_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonCourts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    CourtId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonCourts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonCourts_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonCourts_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonDaySlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    DaySlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonDaySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonDaySlots_DaySlots_DaySlotId",
                        column: x => x.DaySlotId,
                        principalTable: "DaySlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonDaySlots_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    AmountOwing = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonFees_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonFees_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonParameters_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonTimeSlots_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonTimeSlots_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DivisionParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivisionParameters_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    MatchDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleWeeks_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedDivisionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Divisions_AssignedDivisionId",
                        column: x => x.AssignedDivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamApplicants_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    TeamLetter = table.Column<string>(type: "text", nullable: false),
                    SystemName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    CaptainPlayerId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsByeTeam = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Players_CaptainPlayerId",
                        column: x => x.CaptainPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamApplicantDaySlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false),
                    DaySlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicantDaySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicantDaySlots_DaySlots_DaySlotId",
                        column: x => x.DaySlotId,
                        principalTable: "DaySlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamApplicantDaySlots_TeamApplicants_TeamApplicantId",
                        column: x => x.TeamApplicantId,
                        principalTable: "TeamApplicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamApplicantPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicantPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicantPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamApplicantPlayers_TeamApplicants_TeamApplicantId",
                        column: x => x.TeamApplicantId,
                        principalTable: "TeamApplicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamApplicantTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamApplicantTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamApplicantTimeSlots_TeamApplicants_TeamApplicantId",
                        column: x => x.TeamApplicantId,
                        principalTable: "TeamApplicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamApplicantTimeSlots_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LookingForTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookingForTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookingForTeams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LookingForTeams_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LookingForTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleWeekId = table.Column<int>(type: "integer", nullable: false),
                    Team1Id = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<int>(type: "integer", nullable: false),
                    CourtId = table.Column<int>(type: "integer", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScheduledTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_ScheduleWeeks_ScheduleWeekId",
                        column: x => x.ScheduleWeekId,
                        principalTable: "ScheduleWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    PlayoffRoundId = table.Column<int>(type: "integer", nullable: true),
                    Seed1 = table.Column<int>(type: "integer", nullable: false),
                    Seed2 = table.Column<int>(type: "integer", nullable: true),
                    Team1Id = table.Column<int>(type: "integer", nullable: true),
                    Team2Id = table.Column<int>(type: "integer", nullable: true),
                    CourtId = table.Column<int>(type: "integer", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScheduledTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    WinnerId = table.Column<int>(type: "integer", nullable: true),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_PlayoffRounds_PlayoffRoundId",
                        column: x => x.PlayoffRoundId,
                        principalTable: "PlayoffRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatches_Teams_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamParameters_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    Ties = table.Column<int>(type: "integer", nullable: false),
                    NoShows = table.Column<int>(type: "integer", nullable: false),
                    StandingsPoints = table.Column<int>(type: "integer", nullable: false),
                    PointsFor = table.Column<int>(type: "integer", nullable: false),
                    PointsAgainst = table.Column<int>(type: "integer", nullable: false),
                    PlusMinus = table.Column<int>(type: "integer", nullable: false),
                    DivisionRank = table.Column<int>(type: "integer", nullable: true),
                    PlayoffSeed = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamStandings_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamStandings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    Team1Score = table.Column<int>(type: "integer", nullable: false),
                    Team2Score = table.Column<int>(type: "integer", nullable: false),
                    IsForfeit = table.Column<bool>(type: "boolean", nullable: false),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchTeamResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    Ties = table.Column<int>(type: "integer", nullable: false),
                    NoShows = table.Column<int>(type: "integer", nullable: false),
                    StandingsPoints = table.Column<int>(type: "integer", nullable: false),
                    PointsFor = table.Column<int>(type: "integer", nullable: false),
                    PointsAgainst = table.Column<int>(type: "integer", nullable: false),
                    PlusMinus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchTeamResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchTeamResults_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchTeamResults_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpareRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    SpareListId = table.Column<int>(type: "integer", nullable: false),
                    RequestingPlayerId = table.Column<int>(type: "integer", nullable: false),
                    SparePlayerId = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: true),
                    GameDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpareRequests_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpareRequests_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpareRequests_Players_RequestingPlayerId",
                        column: x => x.RequestingPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpareRequests_Players_SparePlayerId",
                        column: x => x.SparePlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpareRequests_SpareLists_SpareListId",
                        column: x => x.SpareListId,
                        principalTable: "SpareLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayoffMatchId = table.Column<int>(type: "integer", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    Team1Score = table.Column<int>(type: "integer", nullable: false),
                    Team2Score = table.Column<int>(type: "integer", nullable: false),
                    IsForfeit = table.Column<bool>(type: "boolean", nullable: false),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffGames_PlayoffMatches_PlayoffMatchId",
                        column: x => x.PlayoffMatchId,
                        principalTable: "PlayoffMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_LeagueId",
                table: "Announcements",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_AppParameters_Key",
                table: "AppParameters",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DivisionParameters_DivisionId_Key",
                table: "DivisionParameters",
                columns: new[] { "DivisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_DaySlotId",
                table: "Divisions",
                column: "DaySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_SeasonId",
                table: "Divisions",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_TimeSlotId",
                table: "Divisions",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailListMembers_EmailListId_PlayerId",
                table: "EmailListMembers",
                columns: new[] { "EmailListId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailListMembers_PlayerId",
                table: "EmailListMembers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLists_LeagueId",
                table: "EmailLists",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_LeagueId",
                table: "EmailLogs",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_MatchId",
                table: "Games",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_Code",
                table: "GlAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiationFees_PlayerId",
                table: "InitiationFees",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_CreditAccountId",
                table: "JournalEntries",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_DebitAccountId",
                table: "JournalEntries",
                column: "DebitAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueOfficials_LeagueId_PlayerId",
                table: "LeagueOfficials",
                columns: new[] { "LeagueId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeagueOfficials_PlayerId",
                table: "LeagueOfficials",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeagueParameters_LeagueId_Key",
                table: "LeagueParameters",
                columns: new[] { "LeagueId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_LeagueId_PlayerId",
                table: "LookingForTeams",
                columns: new[] { "LeagueId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_PlayerId",
                table: "LookingForTeams",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeams_TeamId",
                table: "LookingForTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CourtId",
                table: "Matches",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ScheduleWeekId",
                table: "Matches",
                column: "ScheduleWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchTeamResults_MatchId",
                table: "MatchTeamResults",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchTeamResults_TeamId",
                table: "MatchTeamResults",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerParameters_PlayerId_Key",
                table: "PlayerParameters",
                columns: new[] { "PlayerId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffGames_PlayoffMatchId",
                table: "PlayoffGames",
                column: "PlayoffMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_CourtId",
                table: "PlayoffMatches",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_PlayoffRoundId",
                table: "PlayoffMatches",
                column: "PlayoffRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_SeasonId",
                table: "PlayoffMatches",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_Team1Id",
                table: "PlayoffMatches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_Team2Id",
                table: "PlayoffMatches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatches_WinnerId",
                table: "PlayoffMatches",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffRounds_SeasonId",
                table: "PlayoffRounds",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleWeeks_DivisionId",
                table: "ScheduleWeeks",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonCourts_CourtId",
                table: "SeasonCourts",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonCourts_SeasonId_CourtId",
                table: "SeasonCourts",
                columns: new[] { "SeasonId", "CourtId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonDaySlots_DaySlotId",
                table: "SeasonDaySlots",
                column: "DaySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonDaySlots_SeasonId_DaySlotId",
                table: "SeasonDaySlots",
                columns: new[] { "SeasonId", "DaySlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonFees_PlayerId_SeasonId",
                table: "SeasonFees",
                columns: new[] { "PlayerId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonFees_SeasonId",
                table: "SeasonFees",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonParameters_SeasonId_Key",
                table: "SeasonParameters",
                columns: new[] { "SeasonId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_LeagueId",
                table: "Seasons",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTimeSlots_SeasonId_TimeSlotId",
                table: "SeasonTimeSlots",
                columns: new[] { "SeasonId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTimeSlots_TimeSlotId",
                table: "SeasonTimeSlots",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareLists_LeagueId_PlayerId",
                table: "SpareLists",
                columns: new[] { "LeagueId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpareLists_PlayerId",
                table: "SpareLists",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareRequests_LeagueId",
                table: "SpareRequests",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareRequests_MatchId",
                table: "SpareRequests",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareRequests_RequestingPlayerId",
                table: "SpareRequests",
                column: "RequestingPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareRequests_SpareListId",
                table: "SpareRequests",
                column: "SpareListId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareRequests_SparePlayerId",
                table: "SpareRequests",
                column: "SparePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantDaySlots_DaySlotId",
                table: "TeamApplicantDaySlots",
                column: "DaySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantDaySlots_TeamApplicantId_DaySlotId",
                table: "TeamApplicantDaySlots",
                columns: new[] { "TeamApplicantId", "DaySlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantPlayers_PlayerId",
                table: "TeamApplicantPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantPlayers_TeamApplicantId_PlayerId",
                table: "TeamApplicantPlayers",
                columns: new[] { "TeamApplicantId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_AssignedDivisionId",
                table: "TeamApplicants",
                column: "AssignedDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicants_SeasonId",
                table: "TeamApplicants",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantTimeSlots_TeamApplicantId_TimeSlotId",
                table: "TeamApplicantTimeSlots",
                columns: new[] { "TeamApplicantId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamApplicantTimeSlots_TimeSlotId",
                table: "TeamApplicantTimeSlots",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamParameters_TeamId_Key",
                table: "TeamParameters",
                columns: new[] { "TeamId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_PlayerId",
                table: "TeamPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayers_TeamId_PlayerId",
                table: "TeamPlayers",
                columns: new[] { "TeamId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainPlayerId",
                table: "Teams",
                column: "CaptainPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId_TeamLetter",
                table: "Teams",
                columns: new[] { "DivisionId", "TeamLetter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamStandings_DivisionId",
                table: "TeamStandings",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStandings_TeamId_DivisionId",
                table: "TeamStandings",
                columns: new[] { "TeamId", "DivisionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "AppParameters");

            migrationBuilder.DropTable(
                name: "ClubDocuments");

            migrationBuilder.DropTable(
                name: "DivisionParameters");

            migrationBuilder.DropTable(
                name: "EmailListMembers");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "InitiationFees");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "LeagueOfficials");

            migrationBuilder.DropTable(
                name: "LeagueParameters");

            migrationBuilder.DropTable(
                name: "LookingForTeams");

            migrationBuilder.DropTable(
                name: "MatchTeamResults");

            migrationBuilder.DropTable(
                name: "PendingPlayers");

            migrationBuilder.DropTable(
                name: "PlayerParameters");

            migrationBuilder.DropTable(
                name: "PlayoffGames");

            migrationBuilder.DropTable(
                name: "SeasonCourts");

            migrationBuilder.DropTable(
                name: "SeasonDaySlots");

            migrationBuilder.DropTable(
                name: "SeasonFees");

            migrationBuilder.DropTable(
                name: "SeasonParameters");

            migrationBuilder.DropTable(
                name: "SeasonTimeSlots");

            migrationBuilder.DropTable(
                name: "SpareRequests");

            migrationBuilder.DropTable(
                name: "TeamApplicantDaySlots");

            migrationBuilder.DropTable(
                name: "TeamApplicantPlayers");

            migrationBuilder.DropTable(
                name: "TeamApplicantTimeSlots");

            migrationBuilder.DropTable(
                name: "TeamParameters");

            migrationBuilder.DropTable(
                name: "TeamPlayers");

            migrationBuilder.DropTable(
                name: "TeamStandings");

            migrationBuilder.DropTable(
                name: "EmailLists");

            migrationBuilder.DropTable(
                name: "GlAccounts");

            migrationBuilder.DropTable(
                name: "PlayoffMatches");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "SpareLists");

            migrationBuilder.DropTable(
                name: "TeamApplicants");

            migrationBuilder.DropTable(
                name: "PlayoffRounds");

            migrationBuilder.DropTable(
                name: "Courts");

            migrationBuilder.DropTable(
                name: "ScheduleWeeks");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "DaySlots");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}
