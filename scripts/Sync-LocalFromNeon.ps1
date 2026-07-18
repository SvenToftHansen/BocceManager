# Pulls the Neon (production/website) database down into the local dev Postgres instance.
# Neon is treated as source of truth: local bocce_league is backed up, then fully replaced
# with a dump of Neon's public schema + data.
#
# Usage:  powershell -File Scripts/Sync-LocalFromNeon.ps1

$ErrorActionPreference = "Stop"

$LocalConn = "postgresql://postgres:7720@localhost:5432/bocce_league"
$NeonConn  = "postgresql://neondb_owner:npg_soItNj8C9UeJ@ep-cool-block-afx881rz-pooler.c-2.us-west-2.aws.neon.tech/GVBocce?sslmode=require&channel_binding=require"

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $PSScriptRoot "..\db-backups"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

$localBackupFile = Join-Path $backupDir "local_bocce_league_backup_$stamp.sql"
$neonDumpFile     = Join-Path $backupDir "neon_gvbocce_dump_$stamp.sql"

Write-Host "== 1. Backing up local bocce_league to $localBackupFile =="
pg_dump $LocalConn --no-owner --no-privileges -f $localBackupFile
if ($LASTEXITCODE -ne 0) { throw "Local backup failed - aborting before touching local DB." }

Write-Host "== 2. Dumping Neon (GVBocce) to $neonDumpFile =="
pg_dump $NeonConn --no-owner --no-privileges --clean --if-exists -f $neonDumpFile
if ($LASTEXITCODE -ne 0) { throw "Neon dump failed - aborting before touching local DB." }

Write-Host "== 3. Restoring Neon dump into local bocce_league =="
psql $LocalConn -v ON_ERROR_STOP=1 -f $neonDumpFile
if ($LASTEXITCODE -ne 0) { throw "Restore into local failed. Local DB may be in a partial state - restore from $localBackupFile if needed." }

Write-Host "== Done. Local bocce_league now matches Neon (GVBocce) as of $stamp =="
Write-Host "Local pre-sync backup kept at: $localBackupFile"
