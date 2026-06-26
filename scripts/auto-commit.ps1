# Daily auto-commit for BocceManager
# Runs via Task Scheduler at 9:00 PM — commits tracked changes, skips if nothing to commit.

$repoPath = "C:\Users\svenh\Documents\BocceDocs\BocceManager"
$logFile  = "$env:APPDATA\BocceManager\logs\auto-commit.log"

function Write-Log($msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $msg"
    Write-Output $line
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

# Ensure log directory exists
$null = New-Item -ItemType Directory -Force -Path (Split-Path $logFile)

Set-Location $repoPath

# Check for any changes to tracked files (modifications, deletions, renames)
$status = git status --porcelain 2>&1
$tracked = $status | Where-Object { $_ -match '^( M|M |MM|D | D|R |RM)' }

if (-not $tracked) {
    Write-Log "Nothing to commit — skipping."
    exit 0
}

Write-Log "Changes detected — staging tracked files."
git add -u 2>&1 | ForEach-Object { Write-Log "  add: $_" }

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
$message   = "Auto-commit: $timestamp"

$result = git commit -m $message 2>&1
Write-Log "Committed: $($result -join ' ')"
