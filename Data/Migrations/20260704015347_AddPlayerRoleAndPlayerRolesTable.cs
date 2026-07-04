using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerRoleAndPlayerRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayerRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRoles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PlayerRoles",
                columns: new[] { "Id", "RoleName" },
                values: new object[,]
                {
                    { 0, "Player" },
                    { 1, "Fundraiser" },
                    { 2, "Treasurer" },
                    { 3, "Secretary" },
                    { 4, "Vice President" },
                    { 5, "President" },
                    { 6, "Club Captain" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRoles");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Players");
        }
    }
}
