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

    public static void RestoreBackup(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new Exception($"Backup file not found: {backupFilePath}");

        // PostgreSQL connection details (must match BocceDbContext)
        var pgHost = "localhost";
        var pgPort = "5432";
        var pgDatabase = "bocce_league";
        var pgUsername = "postgres";
        var pgPassword = "7720";

        try
        {
            // Drop and recreate the database
            DropAndRecreateDatabase(pgHost, pgPort, pgUsername, pgPassword, pgDatabase);

            // Restore from backup file using psql
            var psqlPath = GetPsqlPath();
            var process = new ProcessStartInfo
            {
                FileName = psqlPath,
                Arguments = $"--host={pgHost} --port={pgPort} --username={pgUsername} --dbname={pgDatabase} --file=\"{backupFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Environment["PGPASSWORD"] = pgPassword;

            using (var proc = Process.Start(process))
            {
                if (proc == null)
                    throw new Exception("Failed to start psql process");

                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    var error = proc.StandardError.ReadToEnd();
                    throw new Exception($"psql restore failed: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Restore failed: {ex.Message}", ex);
        }
    }

    private static void DropAndRecreateDatabase(string pgHost, string pgPort, string pgUsername, string pgPassword, string pgDatabase)
    {
        var psqlPath = GetPsqlPath();
        var process = new ProcessStartInfo
        {
            FileName = psqlPath,
            Arguments = $"--host={pgHost} --port={pgPort} --username={pgUsername} --dbname=postgres --command=\"DROP DATABASE IF EXISTS {pgDatabase} WITH (FORCE); CREATE DATABASE {pgDatabase};\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Environment["PGPASSWORD"] = pgPassword;

        using (var proc = Process.Start(process))
        {
            if (proc == null)
                throw new Exception("Failed to start psql process");

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                var error = proc.StandardError.ReadToEnd();
                throw new Exception($"Failed to recreate database: {error}");
            }
        }
    }
}
