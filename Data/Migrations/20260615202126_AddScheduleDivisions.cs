using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleDivisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleDivisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DivisionId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    TemplateWeekNumber = table.Column<int>(type: "integer", nullable: false),
                    MatchDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Team1Id = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<int>(type: "integer", nullable: false),
                    CourtId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleDivisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleDivisions_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduleDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleDivisions_ScheduleTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleDivisions_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleDivisions_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDivisions_CourtId",
                table: "ScheduleDivisions",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDivisions_DivisionId",
                table: "ScheduleDivisions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDivisions_Team1Id",
                table: "ScheduleDivisions",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDivisions_Team2Id",
                table: "ScheduleDivisions",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDivisions_TemplateId",
                table: "ScheduleDivisions",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleDivisions");
        }
    }
}
