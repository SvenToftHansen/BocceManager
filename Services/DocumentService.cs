using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public static class DocumentService
{
    public static string DocumentsFolder
    {
        get
        {
            try
            {
                using var db = new BocceManager.Data.BocceDbContext();
                var path = AppParameterService.GetAppParameter(db, "DocumentsFolder");
                if (!string.IsNullOrWhiteSpace(path)) return path;
            }
            catch { }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
        }
    }

    public static void EnsureFolder() => Directory.CreateDirectory(DocumentsFolder);

    public static string GetFilePath(ClubDocument doc) =>
        Path.Combine(DocumentsFolder, doc.FileName);

    public static ClubDocument AddFile(BocceDbContext db, string sourcePath, string title, string? notes, int? leagueId)
    {
        EnsureFolder();
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var docType = ext == ".pdf" ? "pdf" : "docx";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var fileName = baseName + ext;
        var destPath = Path.Combine(DocumentsFolder, fileName);
        if (File.Exists(destPath))
        {
            int counter = 1;
            do { fileName = baseName + " (" + counter++ + ")" + ext; }
            while (File.Exists(Path.Combine(DocumentsFolder, fileName)));
            destPath = Path.Combine(DocumentsFolder, fileName);
        }
        File.Copy(sourcePath, destPath);

        var doc = new ClubDocument
        {
            Title = title,
            FileName = fileName,
            DocType = docType,
            Notes = notes,
            LeagueId = leagueId,
            UploadedAt = DateTime.UtcNow
        };
        db.ClubDocuments.Add(doc);
        db.SaveChanges();
        return doc;
    }

    public static ClubDocument AddGoogleDoc(BocceDbContext db, string url, string title, string? notes, int? leagueId)
    {
        var doc = new ClubDocument
        {
            Title = title,
            FileName = "",
            DocType = "googledocs",
            GoogleDocsUrl = url,
            Notes = notes,
            LeagueId = leagueId,
            UploadedAt = DateTime.UtcNow
        };
        db.ClubDocuments.Add(doc);
        db.SaveChanges();
        return doc;
    }

    public static void Delete(BocceDbContext db, ClubDocument doc)
    {
        if (!string.IsNullOrEmpty(doc.FileName))
        {
            var path = GetFilePath(doc);
            if (File.Exists(path)) File.Delete(path);
        }
        db.ClubDocuments.Remove(doc);
        db.SaveChanges();
    }
}
