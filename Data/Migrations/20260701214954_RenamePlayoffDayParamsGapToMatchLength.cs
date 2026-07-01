using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePlayoffDayParamsGapToMatchLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationBetweenRoundsMins",
                table: "PlayoffDayParams",
                newName: "MatchLengthMins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MatchLengthMins",
                table: "PlayoffDayParams",
                newName: "DurationBetweenRoundsMins");
        }
    }
}
