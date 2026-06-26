using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DivisionParameters");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "LeagueOfficials");

            migrationBuilder.DropTable(
                name: "PendingPlayers");

            migrationBuilder.DropTable(
                name: "PlayerParameters");

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
                name: "GlAccounts");

            migrationBuilder.DropTable(
                name: "TeamApplicants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DivisionParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
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
                name: "GlAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeagueOfficials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivesRegistrations = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivesSpareRequests = table.Column<bool>(type: "boolean", nullable: false),
                    RoleTitle = table.Column<string>(type: "text", nullable: false)
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
                name: "PendingPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    LotNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Preference = table.Column<string>(type: "text", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPlayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
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
                name: "SpareRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: true),
                    RequestingPlayerId = table.Column<int>(type: "integer", nullable: false),
                    SpareListId = table.Column<int>(type: "integer", nullable: false),
                    SparePlayerId = table.Column<int>(type: "integer", nullable: false),
                    GameDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
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
                name: "TeamApplicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignedDivisionId = table.Column<int>(type: "integer", nullable: true),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
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
                name: "TeamParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
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
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreditAccountId = table.Column<int>(type: "integer", nullable: false),
                    DebitAccountId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EnteredBy = table.Column<string>(type: "text", nullable: true),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true)
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
                name: "TeamApplicantDaySlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DaySlotId = table.Column<int>(type: "integer", nullable: false),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false)
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
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TeamApplicantId = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_DivisionParameters_DivisionId_Key",
                table: "DivisionParameters",
                columns: new[] { "DivisionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlAccounts_Code",
                table: "GlAccounts",
                column: "Code",
                unique: true);

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
                name: "IX_PlayerParameters_PlayerId_Key",
                table: "PlayerParameters",
                columns: new[] { "PlayerId", "Key" },
                unique: true);

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
        }
    }
}
