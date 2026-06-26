using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "AppParameters" ("Key", "Value", "Description", "IsActive")
                SELECT 'InitiationFeeAmount', '10.00', 'Default initiation fee charged to new players', true
                WHERE NOT EXISTS (
                    SELECT 1 FROM "AppParameters" WHERE "Key" = 'InitiationFeeAmount'
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "SeasonParameters" ("SeasonId", "Key", "Value", "Description", "IsActive")
                SELECT s."Id", 'SeasonFeeAmount', '10.00', 'Seasonal play fee for this season', true
                FROM "Seasons" s
                WHERE NOT EXISTS (
                    SELECT 1 FROM "SeasonParameters" sp
                    WHERE sp."SeasonId" = s."Id" AND sp."Key" = 'SeasonFeeAmount'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AppParameters" WHERE "Key" = 'InitiationFeeAmount';
                """);

            migrationBuilder.Sql("""
                DELETE FROM "SeasonParameters" WHERE "Key" = 'SeasonFeeAmount';
                """);
        }
    }
}
