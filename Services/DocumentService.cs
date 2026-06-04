using BocceManager.Data;
using BocceManager.Data.Entities;

namespace BocceManager.Services;

public static class DocumentService
{
    public static string DocumentsFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");

    public static void EnsureFolder() => Directory.CreateDirectory(DocumentsFolder);

    public static string GetFilePath(ClubDocument doc) =>
        Path.Combine(DocumentsFolder, doc.FileName);

    public static ClubDocument AddFile(BocceDbContext db, string sourcePath, string title, string? notes, int? leagueId)
    {
        EnsureFolder();
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var docType = ext == ".pdf" ? "pdf" : "docx";
        var fileName = $"{Guid.NewGuid():N}{ext}";
        File.Copy(sourcePath, Path.Combine(DocumentsFolder, fileName));

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
