using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredDaysAndTimesToLFT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredDay_DaySlots_DaySlotId",
                table: "LookingForTeamPreferredDay");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredDay_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredDay");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredTime_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredTime");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredTime_TimeSlots_TimeSlotId",
                table: "LookingForTeamPreferredTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookingForTeamPreferredTime",
                table: "LookingForTeamPreferredTime");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamPreferredTime_LookingForTeamId",
                table: "LookingForTeamPreferredTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookingForTeamPreferredDay",
                table: "LookingForTeamPreferredDay");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamPreferredDay_LookingForTeamId",
                table: "LookingForTeamPreferredDay");

            migrationBuilder.RenameTable(
                name: "LookingForTeamPreferredTime",
                newName: "LookingForTeamPreferredTimes");

            migrationBuilder.RenameTable(
                name: "LookingForTeamPreferredDay",
                newName: "LookingForTeamPreferredDays");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeamPreferredTime_TimeSlotId",
                table: "LookingForTeamPreferredTimes",
                newName: "IX_LookingForTeamPreferredTimes_TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeamPreferredDay_DaySlotId",
                table: "LookingForTeamPreferredDays",
                newName: "IX_LookingForTeamPreferredDays_DaySlotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookingForTeamPreferredTimes",
                table: "LookingForTeamPreferredTimes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookingForTeamPreferredDays",
                table: "LookingForTeamPreferredDays",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredTimes_LookingForTeamId_TimeSlotId",
                table: "LookingForTeamPreferredTimes",
                columns: new[] { "LookingForTeamId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredDays_LookingForTeamId_DaySlotId",
                table: "LookingForTeamPreferredDays",
                columns: new[] { "LookingForTeamId", "DaySlotId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredDays_DaySlots_DaySlotId",
                table: "LookingForTeamPreferredDays",
                column: "DaySlotId",
                principalTable: "DaySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredDays_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredDays",
                column: "LookingForTeamId",
                principalTable: "LookingForTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredTimes_LookingForTeams_LookingForTeam~",
                table: "LookingForTeamPreferredTimes",
                column: "LookingForTeamId",
                principalTable: "LookingForTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredTimes_TimeSlots_TimeSlotId",
                table: "LookingForTeamPreferredTimes",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredDays_DaySlots_DaySlotId",
                table: "LookingForTeamPreferredDays");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredDays_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredDays");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredTimes_LookingForTeams_LookingForTeam~",
                table: "LookingForTeamPreferredTimes");

            migrationBuilder.DropForeignKey(
                name: "FK_LookingForTeamPreferredTimes_TimeSlots_TimeSlotId",
                table: "LookingForTeamPreferredTimes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookingForTeamPreferredTimes",
                table: "LookingForTeamPreferredTimes");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamPreferredTimes_LookingForTeamId_TimeSlotId",
                table: "LookingForTeamPreferredTimes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LookingForTeamPreferredDays",
                table: "LookingForTeamPreferredDays");

            migrationBuilder.DropIndex(
                name: "IX_LookingForTeamPreferredDays_LookingForTeamId_DaySlotId",
                table: "LookingForTeamPreferredDays");

            migrationBuilder.RenameTable(
                name: "LookingForTeamPreferredTimes",
                newName: "LookingForTeamPreferredTime");

            migrationBuilder.RenameTable(
                name: "LookingForTeamPreferredDays",
                newName: "LookingForTeamPreferredDay");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeamPreferredTimes_TimeSlotId",
                table: "LookingForTeamPreferredTime",
                newName: "IX_LookingForTeamPreferredTime_TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_LookingForTeamPreferredDays_DaySlotId",
                table: "LookingForTeamPreferredDay",
                newName: "IX_LookingForTeamPreferredDay_DaySlotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookingForTeamPreferredTime",
                table: "LookingForTeamPreferredTime",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LookingForTeamPreferredDay",
                table: "LookingForTeamPreferredDay",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredTime_LookingForTeamId",
                table: "LookingForTeamPreferredTime",
                column: "LookingForTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_LookingForTeamPreferredDay_LookingForTeamId",
                table: "LookingForTeamPreferredDay",
                column: "LookingForTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredDay_DaySlots_DaySlotId",
                table: "LookingForTeamPreferredDay",
                column: "DaySlotId",
                principalTable: "DaySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredDay_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredDay",
                column: "LookingForTeamId",
                principalTable: "LookingForTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredTime_LookingForTeams_LookingForTeamId",
                table: "LookingForTeamPreferredTime",
                column: "LookingForTeamId",
                principalTable: "LookingForTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LookingForTeamPreferredTime_TimeSlots_TimeSlotId",
                table: "LookingForTeamPreferredTime",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
