using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPartnerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartnerPlayerId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PartnerPlayerId",
                table: "Players",
                column: "PartnerPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Players_PartnerPlayerId",
                table: "Players",
                column: "PartnerPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Players_PartnerPlayerId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_PartnerPlayerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PartnerPlayerId",
                table: "Players");
        }
    }
}
