using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamCount = table.Column<int>(type: "integer", nullable: false),
                    WeekCount = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplates_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplateWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplateWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplateWeeks_ScheduleTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTemplateMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateWeekId = table.Column<int>(type: "integer", nullable: false),
                    Slot1 = table.Column<string>(type: "text", nullable: false),
                    Slot2 = table.Column<string>(type: "text", nullable: false),
                    CourtId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplateMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplateMatches_ScheduleTemplateWeeks_TemplateWeekId",
                        column: x => x.TemplateWeekId,
                        principalTable: "ScheduleTemplateWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleTemplateMatches_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_SeasonId_TeamCount",
                table: "ScheduleTemplates",
                columns: new[] { "SeasonId", "TeamCount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplateWeeks_TemplateId",
                table: "ScheduleTemplateWeeks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplateMatches_TemplateWeekId",
                table: "ScheduleTemplateMatches",
                column: "TemplateWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplateMatches_CourtId",
                table: "ScheduleTemplateMatches",
                column: "CourtId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScheduleTemplateMatches");
            migrationBuilder.DropTable(name: "ScheduleTemplateWeeks");
            migrationBuilder.DropTable(name: "ScheduleTemplates");
        }
    }
}
