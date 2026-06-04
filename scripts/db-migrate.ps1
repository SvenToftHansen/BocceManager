# BocceManager database migration commands
# Run these from the solution root: c:\Users\svenh\Documents\BocceDocs\BocceManager
#
# Apply all pending schema changes to bocce.db (idempotent — safe to run any time):
#   .\scripts\db-migrate.ps1
#
# Or call with a specific action:
#   .\scripts\db-migrate.ps1 -Action apply
#   .\scripts\db-migrate.ps1 -Action tables
#   .\scripts\db-migrate.ps1 -Action apply -DbPath "C:\path\to\bocce.db"

param(
    [string]$Action = "apply",   # apply | tables
    [string]$DbPath = ""
)

$project = "BocceManager.DbMigrate/BocceManager.DbMigrate.csproj"

switch ($Action) {
    "apply" {
        Write-Host "Applying migrations..." -ForegroundColor Cyan
        if ($DbPath) {
            dotnet run --project $project -- $DbPath
        } else {
            dotnet run --project $project
        }
    }
    "tables" {
        Write-Host "Listing tables in bocce.db..." -ForegroundColor Cyan
        if ($DbPath) {
            dotnet run --project $project -- $DbPath --tables
        } else {
            dotnet run --project $project -- --tables
        }
    }
    default {
        Write-Host "Unknown action: $Action" -ForegroundColor Red
        Write-Host "Usage: .\scripts\db-migrate.ps1 [-Action apply|tables] [-DbPath path]"
    }
}
