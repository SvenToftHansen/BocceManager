using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerNumberToTeamPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerNumber",
                table: "TeamPlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rosters: the current captain (if any) becomes player #1,
            // remaining players keep their existing relative order after that.
            migrationBuilder.Sql(
                "UPDATE \"TeamPlayers\" " +
                "SET \"PlayerNumber\" = ( " +
                "    SELECT x.\"rn\" FROM ( " +
                "        SELECT tp2.\"Id\" AS \"Id\", " +
                "               ROW_NUMBER() OVER ( " +
                "                   PARTITION BY tp2.\"TeamId\" " +
                "                   ORDER BY CASE WHEN tp2.\"PlayerId\" = t.\"CaptainPlayerId\" THEN 0 ELSE 1 END, tp2.\"Id\" " +
                "               ) AS \"rn\" " +
                "        FROM \"TeamPlayers\" tp2 " +
                "        JOIN \"Teams\" t ON t.\"Id\" = tp2.\"TeamId\" " +
                "    ) x " +
                "    WHERE x.\"Id\" = \"TeamPlayers\".\"Id\" " +
                ");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerNumber",
                table: "TeamPlayers");
        }
    }
}
