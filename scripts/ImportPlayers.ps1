# Import players from PostgreSQL backup into BocceManager SQLite database
# Usage: .\ImportPlayers.ps1

param(
    [string]$BackupFile = "C:\Users\svenh\Documents\BocceDocs\dump_inserts.sql",
    [string]$DbPath = "C:\Users\svenh\Documents\BocceDocs\BocceManager\bin\Debug\net10.0-windows\bocce.db"
)

if (-not (Test-Path $DbPath)) {
    Write-Error "Database not found at: $DbPath"
    exit 1
}

if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup file not found at: $BackupFile"
    exit 1
}

Write-Host "Importing players from PostgreSQL backup..."
Write-Host "Backup: $BackupFile"
Write-Host "Database: $DbPath"
Write-Host ""

# Read the SQL file
$sqlContent = Get-Content $BackupFile -Raw

# Extract the players INSERT block
$playerPattern = @'
INSERT INTO public\.players \(id.*?\) VALUES\s+(.+?)(?=;|\n--|$)
'@

if ($sqlContent -match $playerPattern) {
    $playerData = $matches[1]
    Write-Host "Found player data in backup file"

    # Parse the player records
    $players = @()
    $records = $playerData -split '\),\s*\('

    foreach ($record in $records) {
        # Clean up the record
        $record = $record -replace '^\(', '' -replace '\)$', '' -replace "^'", '' -replace "'$", ''

        # Parse the values
        $parts = @()
        $inString = $false
        $currentPart = ""

        foreach ($char in $record.ToCharArray()) {
            if ($char -eq "'" -and $inString) {
                $inString = $false
                $parts += $currentPart
                $currentPart = ""
            } elseif ($char -eq "'" -and -not $inString) {
                $inString = $true
            } elseif ($char -eq ',' -and -not $inString) {
                if ($currentPart -ne "") {
                    $parts += $currentPart
                    $currentPart = ""
                }
            } else {
                $currentPart += $char
            }
        }
        if ($currentPart -ne "") {
            $parts += $currentPart
        }

        # Extract player fields: id, parkId, firstName, lastName, email, phone, lotNumber, isActive, createdAt
        if ($parts.Count -ge 5) {
            $id = [int]::Parse($parts[0].Trim())
            $firstName = $parts[2].Trim()
            $lastName = $parts[3].Trim()
            $email = if ([string]::IsNullOrWhiteSpace($parts[4]) -or $parts[4] -eq "NULL") { $null } else { $parts[4].Trim() }
            $phone = if ([string]::IsNullOrWhiteSpace($parts[5]) -or $parts[5] -eq "NULL") { $null } else { $parts[5].Trim() }
            $lotNumber = if ([string]::IsNullOrWhiteSpace($parts[6]) -or $parts[6] -eq "NULL") { $null } else { $parts[6].Trim() }

            $players += @{
                Id = $id
                FirstName = $firstName
                LastName = $lastName
                Email = $email
                Phone = $phone
                LotNumber = $lotNumber
            }
        }
    }

    Write-Host "Extracted $($players.Count) players from backup"
    Write-Host ""
    Write-Host "First few players:"
    $players | Select-Object -First 5 | ForEach-Object { Write-Host "  - $($_.FirstName) $($_.LastName)" }
    Write-Host ""
    Write-Host "Note: Full import would need to be done via the application or via a C# migration script"
    Write-Host "For now, you can manually view the backup file at: $BackupFile"
}
else {
    Write-Host "Could not find player data in backup file"
    Write-Host "You can manually import data by:"
    Write-Host "1. Restoring the PostgreSQL database locally"
    Write-Host "2. Exporting to CSV"
    Write-Host "3. Importing into BocceManager via the UI"
}
