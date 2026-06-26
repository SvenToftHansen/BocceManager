using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredDaysAndTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LookingForTeamPreferredDay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LookingForTeamId = table.Column<int>(type: "integer", nullable: false),
                    DaySlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookingForTeamPreferredDay", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookingForTeamPreferredDay_DaySlots_DaySlotId",
                        column: x => x.DaySlotId,
                        principalTable: "DaySlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LookingForTeamPreferredDay_LookingForTeams_LookingForTeamId",
                        column: x => x.LookingForTeamId,
                        principalTable: "LookingForTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LookingForTeamPreferredTime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LookingForTeamId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookingForTeamPreferredTime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookingForTeamPreferredTime_LookingForTeams_LookingForTeamId",
                        column: x => x.LookingForTeamId,
                        principalTable: "LookingForTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LookingForTeamPreferredTime_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredDay_DaySlotId",
                table: "LookingForTeamPreferredDay",
                column: "DaySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredDay_LookingForTeamId",
                table: "LookingForTeamPreferredDay",
                column: "LookingForTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredTime_LookingForTeamId",
                table: "LookingForTeamPreferredTime",
                column: "LookingForTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredTime_TimeSlotId",
                table: "LookingForTeamPreferredTime",
                column: "TimeSlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LookingForTeamPreferredDay");

            migrationBuilder.DropTable(
                name: "LookingForTeamPreferredTime");
        }
    }
}
