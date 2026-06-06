using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using Microsoft.Data.Sqlite;

namespace BocceManager.Tests;

public class AppParameterServiceTests
{
    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Load_EmptyDb_ReturnsEmpty()
    {
        using var tdb = new TestDb();

        var result = AppParameterService.Load(tdb.Db);

        Assert.Empty(result);
    }

    [Fact]
    public void Load_MultipleParams_ReturnsSortedByKey()
    {
        using var tdb = new TestDb();
        tdb.Db.AppParameters.AddRange(
            new AppParameter { Key = "zoo",   Value = "z" },
            new AppParameter { Key = "alpha", Value = "a" },
            new AppParameter { Key = "mid",   Value = "m" });
        tdb.Db.SaveChanges();

        var result = AppParameterService.Load(tdb.Db);

        Assert.Equal(["alpha", "mid", "zoo"], result.Select(p => p.Key));
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Save_PersistsAllRows()
    {
        using var tdb = new TestDb();
        (string, string, string)[] rows =
        [
            ("fee",     "50",             "Annual fee"),
            ("max",     "16",             "Max teams"),
            ("contact", "admin@bocce.ca", "")
        ];

        AppParameterService.Save(tdb.Db, rows);

        var saved = tdb.Db.AppParameters.OrderBy(p => p.Key).ToList();
        Assert.Equal(3, saved.Count);
        Assert.Equal("contact", saved[0].Key);
        Assert.Equal("admin@bocce.ca", saved[0].Value);
        Assert.Equal("fee",       saved[1].Key);
        Assert.Equal("50",        saved[1].Value);
        Assert.Equal("Annual fee", saved[1].Description);
    }

    [Fact]
    public void Save_ReplacesExistingParams()
    {
        using var tdb = new TestDb();
        tdb.Db.AppParameters.Add(new AppParameter { Key = "old", Value = "gone" });
        tdb.Db.SaveChanges();

        AppParameterService.Save(tdb.Db, [("new", "kept", "")]);

        var saved = tdb.Db.AppParameters.ToList();
        Assert.Single(saved);
        Assert.Equal("new", saved[0].Key);
    }

    [Fact]
    public void Save_EmptyDescription_StoredAsNull()
    {
        using var tdb = new TestDb();

        AppParameterService.Save(tdb.Db, [("k", "v", "")]);

        Assert.Null(tdb.Db.AppParameters.Single().Description);
    }

    [Fact]
    public void Save_WhitespaceDescription_StoredAsNull()
    {
        using var tdb = new TestDb();

        AppParameterService.Save(tdb.Db, [("k", "v", "   ")]);

        Assert.Null(tdb.Db.AppParameters.Single().Description);
    }

    [Fact]
    public void Save_NonEmptyDescription_Persisted()
    {
        using var tdb = new TestDb();

        AppParameterService.Save(tdb.Db, [("k", "v", "some desc")]);

        Assert.Equal("some desc", tdb.Db.AppParameters.Single().Description);
    }

    // ── NullIfEmpty ───────────────────────────────────────────────────────────

    [Fact]
    public void NullIfEmpty_Null_ReturnsNull()
        => Assert.Null(AppParameterService.NullIfEmpty(null));

    [Fact]
    public void NullIfEmpty_EmptyString_ReturnsNull()
        => Assert.Null(AppParameterService.NullIfEmpty(""));

    [Fact]
    public void NullIfEmpty_WhitespaceOnly_ReturnsNull()
        => Assert.Null(AppParameterService.NullIfEmpty("   "));

    [Fact]
    public void NullIfEmpty_NonEmpty_ReturnsValue()
        => Assert.Equal("hello", AppParameterService.NullIfEmpty("hello"));

    // ── Fmt ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Fmt_Null_ReturnsEmDash()
        => Assert.Equal("—", AppParameterService.Fmt(null));

    [Fact]
    public void Fmt_Zero_ReturnsZeroString()
        => Assert.Equal("0", AppParameterService.Fmt(0));

    [Fact]
    public void Fmt_PositiveValue_ReturnsString()
        => Assert.Equal("42", AppParameterService.Fmt(42));

    // ── Test helper ───────────────────────────────────────────────────────────

    private sealed class TestDb : IDisposable
    {
        private readonly string _path;
        public BocceDbContext Db { get; }

        public TestDb()
        {
            _path = Path.Combine(Path.GetTempPath(), $"bocce_test_{Guid.NewGuid():N}.db");
            BocceDbContext.DbPath = _path;
            Db = new BocceDbContext();
            Db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Db.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_path); } catch { }
        }
    }
}
