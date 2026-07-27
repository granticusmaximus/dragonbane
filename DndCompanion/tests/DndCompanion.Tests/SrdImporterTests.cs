using DndCompanion.Domain;
using DndCompanion.Infrastructure.Persistence;
using DndCompanion.Infrastructure.Srd;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Tests;

public class SrdImporterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly string _dataDir;

    public SrdImporterTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _dataDir = Directory.CreateTempSubdirectory().FullName;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        Directory.Delete(_dataDir, recursive: true);
    }

    private void WriteFixtures()
    {
        File.WriteAllText(Path.Combine(_dataDir, "spells.json"), """
            [{"Name":"Test Bolt","Level":0,"School":"Evocation","CastingTime":"Action","Range":"120 feet","Components":"V, S","Duration":"Instantaneous","Concentration":false,"RulesText":"Test spell.","ScalingJson":null}]
            """);
        File.WriteAllText(Path.Combine(_dataDir, "items.json"), """
            [{"Name":"Test Dagger","Category":"Weapon","WeightLb":1.0,"Cost":"2 GP","PropertiesJson":null,"RulesText":null}]
            """);
        File.WriteAllText(Path.Combine(_dataDir, "actions.json"), """
            [{"Name":"Test Dash","ActionType":"Action","RulesText":"Move fast."}]
            """);
    }

    [Fact]
    public async Task Imports_spells_items_and_actions_tagged_with_srd_source()
    {
        WriteFixtures();
        var importer = new SrdImporter(_db);

        var changes = await importer.ImportAsync(_dataDir);

        Assert.True(changes > 0);

        var source = Assert.Single(await _db.ContentSources.ToListAsync());
        Assert.Equal(ContentKind.Srd, source.Kind);
        Assert.Equal(SrdImporter.SrdAttribution, source.Attribution);

        var spell = Assert.Single(await _db.Spells.ToListAsync());
        Assert.Equal("Test Bolt", spell.Name);
        Assert.Equal(source.Id, spell.SourceId);

        var item = Assert.Single(await _db.Items.ToListAsync());
        Assert.Equal("Test Dagger", item.Name);
        Assert.Equal(source.Id, item.SourceId);

        var action = Assert.Single(await _db.ActionDefs.ToListAsync());
        Assert.Equal("Test Dash", action.Name);
        Assert.Equal(source.Id, action.SourceId);
    }

    [Fact]
    public async Task Second_import_is_a_no_op()
    {
        WriteFixtures();
        var importer = new SrdImporter(_db);

        await importer.ImportAsync(_dataDir);
        var secondRunChanges = await importer.ImportAsync(_dataDir);

        Assert.Equal(0, secondRunChanges);
        Assert.Single(await _db.ContentSources.ToListAsync());
        Assert.Single(await _db.Spells.ToListAsync());
    }

    [Fact]
    public async Task Unknown_spell_school_throws()
    {
        File.WriteAllText(Path.Combine(_dataDir, "spells.json"), """
            [{"Name":"Bad Spell","Level":1,"School":"NotASchool","CastingTime":"Action","Range":"Self","Components":"V","Duration":"Instantaneous","Concentration":false,"RulesText":null,"ScalingJson":null}]
            """);
        var importer = new SrdImporter(_db);

        await Assert.ThrowsAsync<InvalidDataException>(() => importer.ImportAsync(_dataDir));
    }

    [Fact]
    public async Task Missing_data_files_import_cleanly_with_zero_rows()
    {
        var importer = new SrdImporter(_db);

        await importer.ImportAsync(_dataDir);

        Assert.Single(await _db.ContentSources.ToListAsync());
        Assert.Empty(await _db.Spells.ToListAsync());
        Assert.Empty(await _db.Items.ToListAsync());
        Assert.Empty(await _db.ActionDefs.ToListAsync());
    }
}
