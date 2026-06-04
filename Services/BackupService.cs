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
}
