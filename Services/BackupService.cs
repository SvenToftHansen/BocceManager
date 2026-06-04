using System;
using System.Diagnostics;
using System.IO;

namespace BocceManager.Services;

public static class BackupService
{
    private static string BackupFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

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
            var process = new ProcessStartInfo
            {
                FileName = "pg_dump",
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
            var process = new ProcessStartInfo
            {
                FileName = "psql",
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
        var process = new ProcessStartInfo
        {
            FileName = "psql",
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
