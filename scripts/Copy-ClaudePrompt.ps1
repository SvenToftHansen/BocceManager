$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$promptPath = Join-Path $repoRoot 'docs\CLAUDE_HANDOFF_PROMPT.md'

if (-not (Test-Path -LiteralPath $promptPath)) {
    throw "Missing file: $promptPath"
}

$content = Get-Content -LiteralPath $promptPath -Raw
Set-Clipboard -Value $content
Write-Host "Claude handoff prompt copied to clipboard."
