using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BocceManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class NullifyDefaultScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert old default-zero rows (never played) to NULL.
            // Any real game result has at least one score of 12 or -1,
            // so rows where all four are 0 were never entered.
            migrationBuilder.Sql(@"
                UPDATE ""ScheduleDivisions""
                SET ""Team1Score1"" = NULL,
                    ""Team2Score1"" = NULL,
                    ""Team1Score2"" = NULL,
                    ""Team2Score2"" = NULL
                WHERE ""Team1Score1"" = 0
                  AND ""Team2Score1"" = 0
                  AND ""Team1Score2"" = 0
                  AND ""Team2Score2"" = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore NULLs to 0 (the old non-nullable default)
            migrationBuilder.Sql(@"
                UPDATE ""ScheduleDivisions""
                SET ""Team1Score1"" = COALESCE(""Team1Score1"", 0),
                    ""Team2Score1"" = COALESCE(""Team2Score1"", 0),
                    ""Team1Score2"" = COALESCE(""Team1Score2"", 0),
                    ""Team2Score2"" = COALESCE(""Team2Score2"", 0);
            ");
        }
    }
}
