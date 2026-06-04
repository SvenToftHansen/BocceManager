$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$handoffPath = Join-Path $repoRoot 'docs\AI_HANDOFF.md'

if (-not (Test-Path -LiteralPath $handoffPath)) {
    throw "Missing file: $handoffPath"
}

$content = Get-Content -LiteralPath $handoffPath -Raw
$startMarker = '## Latest Handoff'
$endMarker = '## Copy/Paste Handoff Block'

$startIndex = $content.IndexOf($startMarker)
$endIndex = $content.IndexOf($endMarker)

if ($startIndex -lt 0 -or $endIndex -lt 0 -or $endIndex -le $startIndex) {
    throw 'Could not parse Latest Handoff section.'
}

$latest = $content.Substring($startIndex, $endIndex - $startIndex).Trim()

$out = @"
I am handing off from another assistant. Continue with the same approach and business rules from this handoff.

$latest
"@

Set-Clipboard -Value $out
Write-Host "Latest handoff copied to clipboard."
