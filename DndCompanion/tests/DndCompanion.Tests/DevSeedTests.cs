using DndCompanion.Domain;
using DndCompanion.Infrastructure.Dev;
using DndCompanion.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Tests;

public class DevSeedTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public DevSeedTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Seeds_Baloney_Slim_with_linked_spells_and_items()
    {
        await DevSeed.BaloneySlimAsync(_db);

        var character = await _db.Characters.SingleOrDefaultAsync(c => c.Name == "Baloney Slim");
        Assert.NotNull(character);
        Assert.Equal("Aasimar", character!.Species);
        Assert.Equal("Druid", character.Class);
        Assert.Null(character.Subclass);
        Assert.Equal(1, character.Level);
        Assert.Equal(14, character.Abilities.Strength);
        Assert.Equal(15, character.Abilities.Wisdom);

        var spellLinks = await _db.Set<Domain.Entities.CharacterSpell>()
            .Where(cs => cs.CharacterId == character.Id)
            .Include(cs => cs.Spell)
            .ToListAsync();
        Assert.Equal(12, spellLinks.Count); // 6 cantrips + 4 prepared 1st-level + 2 always-prepared (Druidic, Magic Initiate)

        var speakWithAnimals = Assert.Single(spellLinks, cs => cs.Spell.Name == "Speak with Animals");
        Assert.True(speakWithAnimals.AlwaysPrepared);
        Assert.True(speakWithAnimals.IsPrepared);

        var itemLinks = await _db.Set<Domain.Entities.CharacterItem>()
            .Where(ci => ci.CharacterId == character.Id)
            .Include(ci => ci.Item)
            .ToListAsync();
        Assert.Equal(8, itemLinks.Count);
    }

    [Fact]
    public async Task Every_seeded_spell_and_item_has_a_ContentSource()
    {
        await DevSeed.BaloneySlimAsync(_db);

        var spells = await _db.Spells.Include(s => s.Source).ToListAsync();
        var items = await _db.Items.Include(i => i.Source).ToListAsync();

        Assert.All(spells, s => Assert.NotNull(s.Source));
        Assert.All(items, i => Assert.NotNull(i.Source));
    }

    [Fact]
    public async Task Light_is_tagged_Homebrew_and_distinct_from_any_SRD_Light_row()
    {
        // Simulate the SRD importer having already seeded a real "Light" cantrip before DevSeed runs.
        var srd = new Domain.Entities.ContentSource { Kind = ContentKind.Srd, Attribution = "SRD test fixture" };
        _db.Add(srd);
        await _db.SaveChangesAsync();
        _db.Add(new Domain.Entities.Spell
        {
            Source = srd, Name = "Light", Level = 0, School = SpellSchool.Evocation,
            CastingTime = "Action", Range = "Touch", Components = "V, M", Duration = "1 hour"
        });
        await _db.SaveChangesAsync();

        await DevSeed.BaloneySlimAsync(_db);

        var lightRows = await _db.Spells.Include(s => s.Source).Where(s => s.Name == "Light").ToListAsync();
        Assert.Equal(2, lightRows.Count);
        Assert.Contains(lightRows, s => s.Source.Kind == ContentKind.Srd);
        Assert.Contains(lightRows, s => s.Source.Kind == ContentKind.Homebrew);
    }

    [Fact]
    public async Task Running_twice_does_not_duplicate_the_character_or_its_links()
    {
        await DevSeed.BaloneySlimAsync(_db);
        await DevSeed.BaloneySlimAsync(_db);

        var characters = await _db.Characters.Where(c => c.Name == "Baloney Slim").ToListAsync();
        Assert.Single(characters);

        var spellLinks = await _db.Set<Domain.Entities.CharacterSpell>()
            .Where(cs => cs.CharacterId == characters[0].Id)
            .ToListAsync();
        Assert.Equal(12, spellLinks.Count);
    }
}
