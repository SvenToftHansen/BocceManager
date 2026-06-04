using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BocceManager.Services;

public static class BackupService
{
    private static string BackupFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

    private static string FindPostgresqlBin()
    {
        // Try common PostgreSQL installation paths on Windows
        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL",
            @"C:\Program Files (x86)\PostgreSQL",
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\PostgreSQL"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\PostgreSQL")
        };

        foreach (var basePath in commonPaths)
        {
            if (!Directory.Exists(basePath)) continue;

            // Find latest version folder
            var versionFolders = Directory.GetDirectories(basePath)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            if (versionFolders != null)
            {
                var binPath = Path.Combine(versionFolders, "bin");
                if (Directory.Exists(binPath))
                    return binPath;
            }
        }

        throw new Exception("PostgreSQL installation not found. Please ensure PostgreSQL is installed.");
    }

    private static string GetPgDumpPath()
    {
        var pgBin = FindPostgresqlBin();
        var pgDumpPath = Path.Combine(pgBin, "pg_dump.exe");
        if (!File.Exists(pgDumpPath))
            throw new Exception($"pg_dump not found at {pgDumpPath}");
        return pgDumpPath;
    }

    private static string GetPsqlPath()
    {
        var pgBin = FindPostgresqlBin();
        var psqlPath = Path.Combine(pgBin, "psql.exe");
        if (!File.Exists(psqlPath))
            throw new Exception($"psql not found at {psqlPath}");
        return psqlPath;
    }

    public static string CreateBackup()
    {
        // Ensure backup folder exists
        Directory.CreateDirectory(BackupFolder);

        // Generate backup filename with timestamp
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupFile = Path.Combine(BackupFolder, $"bocce_league_backup_{timestamp}.sql");

        // PostgreSQL connection details (must match BocceDbContext)
        var pgHost = "localhost";
        var pgPort = "5432";
        var pgDatabase = "bocce_league";
        var pgUsername = "postgres";
        var pgPassword = "7720";

        try
        {
            // Use pg_dump to export the database
            var pgDumpPath = GetPgDumpPath();
            var process = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"--host={pgHost} --port={pgPort} --username={pgUsername} --no-password --format=plain {pgDatabase}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set password via environment variable (safer than command line)
            process.Environment["PGPASSWORD"] = pgPassword;

            using (var proc = Process.Start(process))
            {
                if (proc == null)
                    throw new Exception("Failed to start pg_dump process");

                // Capture output to file
                using (var writer = new StreamWriter(backupFile))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    writer.Write(output);
                }

                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    var error = proc.StandardError.ReadToEnd();
                    throw new Exception($"pg_dump failed: {error}");
                }
            }

            if (!File.Exists(backupFile) || new FileInfo(backupFile).Length == 0)
                throw new Exception("Backup file was not created or is empty");

            return backupFile;
        }
        catch (Exception ex)
        {
            // Clean up failed backup
            if (File.Exists(backupFile))
                File.Delete(backupFile);

            throw new Exception($"Backup failed: {ex.Message}", ex);
        }
    }

    public static string GetBackupFolderPath() => BackupFolder;

    public static void RestoreBackup(string backupFilePath, Action<string> progress = null)
    {
        if (!File.Exists(backupFilePath))
            throw new Exception($"Backup file not found: {backupFilePath}");

        var pgHost     = "localhost";
        var pgPort     = "5432";
        var pgDatabase = "bocce_league";
        var pgUsername = "postgres";
        var pgPassword = "7720";

        try
        {
            progress?.Invoke("Step 1 of 4 — Closing active database connections...");
            RunPsqlCommand(pgHost, pgPort, pgUsername, pgPassword, "postgres",
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{pgDatabase}' AND pid <> pg_backend_pid();");

            progress?.Invoke("Step 2 of 4 — Dropping existing database...");
            RunPsqlCommand(pgHost, pgPort, pgUsername, pgPassword, "postgres",
                $"DROP DATABASE IF EXISTS \"{pgDatabase}\";");

            progress?.Invoke("Step 3 of 4 — Creating fresh database...");
            RunPsqlCommand(pgHost, pgPort, pgUsername, pgPassword, "postgres",
                $"CREATE DATABASE \"{pgDatabase}\";");

            progress?.Invoke("Step 4 of 4 — Restoring data (this may take a moment)...");
            var psqlPath = GetPsqlPath();
            var psi = new ProcessStartInfo
            {
                FileName = psqlPath,
                // --single-transaction wraps the entire restore in one commit instead of
                // committing every INSERT separately — makes restore ~10-50x faster.
                Arguments = $"--host={pgHost} --port={pgPort} --username={pgUsername} --dbname={pgDatabase} --single-transaction --file=\"{backupFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = pgPassword;

            using var proc = Process.Start(psi)
                ?? throw new Exception("Failed to start psql process");
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                var error = proc.StandardError.ReadToEnd().Trim();
                throw new Exception($"psql restore failed: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Restore failed: {ex.Message}", ex);
        }
    }

    private static void RunPsqlCommand(string pgHost, string pgPort, string pgUsername, string pgPassword, string dbName, string sql)
    {
        var psqlPath = GetPsqlPath();
        var psi = new ProcessStartInfo
        {
            FileName = psqlPath,
            Arguments = $"--host={pgHost} --port={pgPort} --username={pgUsername} --dbname={dbName} --command={Quote(sql)}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = pgPassword;

        using var proc = Process.Start(psi)
            ?? throw new Exception("Failed to start psql process");

        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            var error = proc.StandardError.ReadToEnd().Trim();
            throw new Exception($"Failed to recreate database: {error}");
        }
    }

    private static string Quote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";

    public static BackupSummary PreviewBackup(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new Exception($"Backup file not found: {backupFilePath}");

        var fileInfo = new FileInfo(backupFilePath);
        var summary = new BackupSummary
        {
            FileName   = fileInfo.Name,
            FileSizeKb = fileInfo.Length / 1024,
            FileDate   = fileInfo.LastWriteTime,
            TableCounts = new System.Collections.Generic.Dictionary<string, int>()
        };

        // Scan the SQL file, counting COPY or INSERT rows per table
        string currentTable = null;
        bool inCopyBlock = false;

        foreach (var line in File.ReadLines(backupFilePath))
        {
            if (line.StartsWith("-- Dumped on"))
            {
                // e.g. "-- Dumped on: 2026-06-04 14:32:45"
                var dateStr = line.Substring("-- Dumped on".Length).Trim().TrimStart(':').Trim();
                if (DateTime.TryParse(dateStr, out var dt))
                    summary.DumpedOn = dt;
            }
            else if (line.StartsWith("-- PostgreSQL database version"))
            {
                summary.PostgresVersion = line.Replace("--", "").Trim();
            }
            else if (line.StartsWith("COPY "))
            {
                // COPY public."Players" (col1, col2, ...) FROM stdin;
                var tableName = ExtractTableName(line);
                if (tableName != null)
                {
                    currentTable = tableName;
                    inCopyBlock = true;
                    if (!summary.TableCounts.ContainsKey(currentTable))
                        summary.TableCounts[currentTable] = 0;
                }
            }
            else if (inCopyBlock && line == "\\.")
            {
                inCopyBlock = false;
                currentTable = null;
            }
            else if (inCopyBlock && currentTable != null && line.Length > 0)
            {
                summary.TableCounts[currentTable]++;
            }
        }

        return summary;
    }

    private static string ExtractTableName(string copyLine)
    {
        // COPY public."TableName" (...) FROM stdin;
        var start = copyLine.IndexOf('"');
        var end   = copyLine.IndexOf('"', start + 1);
        if (start >= 0 && end > start)
            return copyLine.Substring(start + 1, end - start - 1);
        // Fallback: COPY public.TableName (...) FROM stdin;
        var parts = copyLine.Split(' ');
        if (parts.Length >= 2)
        {
            var tbl = parts[1];
            if (tbl.Contains('.')) tbl = tbl.Split('.')[1];
            return tbl.Trim('"');
        }
        return null;
    }
}

public class BackupSummary
{
    public string   FileName    { get; set; }
    public long     FileSizeKb  { get; set; }
    public DateTime FileDate    { get; set; }
    public DateTime? DumpedOn   { get; set; }
    public string   PostgresVersion { get; set; }
    public System.Collections.Generic.Dictionary<string, int> TableCounts { get; set; }
}
