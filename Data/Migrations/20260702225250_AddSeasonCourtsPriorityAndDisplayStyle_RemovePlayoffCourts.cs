using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonCourtsPriorityAndDisplayStyle_RemovePlayoffCourts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoffCourts");

            migrationBuilder.AddColumn<string>(
                name: "CourtDisplayStyle",
                table: "Seasons",
                type: "text",
                nullable: false,
                defaultValue: "number");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SeasonCourts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourtDisplayStyle",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SeasonCourts");

            migrationBuilder.CreateTable(
                name: "PlayoffCourts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourtId = table.Column<int>(type: "integer", nullable: false),
                    PlayoffConfigId = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffCourts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffCourts_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffCourts_PlayoffConfigs_PlayoffConfigId",
                        column: x => x.PlayoffConfigId,
                        principalTable: "PlayoffConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffCourts_CourtId",
                table: "PlayoffCourts",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffCourts_PlayoffConfigId",
                table: "PlayoffCourts",
                column: "PlayoffConfigId");
        }
    }
}
