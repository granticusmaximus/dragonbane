using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Domain.ValueObjects;
using DndCompanion.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Tests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public RepositoryTests()
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
    public async Task Campaign_repository_lists_only_active_campaigns()
    {
        var repo = new EfCampaignRepository(_db);
        await repo.AddAsync(new Campaign { Name = "Active One", IsActive = true });
        await repo.AddAsync(new Campaign { Name = "Retired", IsActive = false });
        await repo.SaveChangesAsync();

        var active = await repo.ListActiveAsync();

        Assert.Single(active);
        Assert.Equal("Active One", active[0].Name);
    }

    [Fact]
    public async Task Campaign_repository_loads_characters_with_GetWithCharactersAsync()
    {
        var campaignRepo = new EfCampaignRepository(_db);
        var characterRepo = new EfCharacterRepository(_db);

        var character = new Character { Name = "Lyra", Species = "Human", Class = "Cleric" };
        await characterRepo.AddAsync(character);

        var campaign = new Campaign { Name = "Curse of Strahd" };
        await campaignRepo.AddAsync(campaign);
        await campaignRepo.SaveChangesAsync();

        campaign.Characters.Add(new CampaignCharacter { CampaignId = campaign.Id, CharacterId = character.Id });
        await campaignRepo.SaveChangesAsync();

        var loaded = await campaignRepo.GetWithCharactersAsync(campaign.Id);

        Assert.NotNull(loaded);
        var cc = Assert.Single(loaded!.Characters);
        Assert.Equal("Lyra", cc.Character.Name);
    }

    [Fact]
    public async Task Campaign_repository_loads_all_sessions_with_GetWithCharactersAsync()
    {
        // Ordering is a client-side concern (see the comment on GetWithCharactersAsync) — this
        // only asserts the sessions load at all, with the right data.
        var campaignRepo = new EfCampaignRepository(_db);
        var sessionRepo = new EfRepository<SessionLog>(_db);

        var campaign = new Campaign { Name = "Beyond the Blue Door" };
        await campaignRepo.AddAsync(campaign);
        await campaignRepo.SaveChangesAsync();

        await sessionRepo.AddAsync(new SessionLog { CampaignId = campaign.Id, SessionDate = new DateTime(2026, 2, 1), Notes = "Session two" });
        await sessionRepo.AddAsync(new SessionLog { CampaignId = campaign.Id, SessionDate = new DateTime(2026, 1, 1), Notes = "Session zero" });
        await sessionRepo.SaveChangesAsync();

        var loaded = await campaignRepo.GetWithCharactersAsync(campaign.Id);

        Assert.NotNull(loaded);
        var ordered = loaded!.Sessions.OrderBy(s => s.SessionDate).ToList();
        Assert.Equal(2, ordered.Count);
        Assert.Equal("Session zero", ordered[0].Notes);
        Assert.Equal("Session two", ordered[1].Notes);
    }

    [Fact]
    public async Task Character_repository_GetSheetAsync_includes_homebrew_item_with_source()
    {
        var characterRepo = new EfCharacterRepository(_db);
        var itemRepo = new EfRepository<Item>(_db);
        var sourceRepo = new EfRepository<ContentSource>(_db);

        var homebrew = new ContentSource { Kind = ContentKind.Homebrew, Attribution = "User-created homebrew content." };
        await sourceRepo.AddAsync(homebrew);
        await sourceRepo.SaveChangesAsync();

        var item = new Item { Source = homebrew, Name = "Blade of the Ancients", Category = "Weapon" };
        await itemRepo.AddAsync(item);
        await itemRepo.SaveChangesAsync();

        var character = new Character { Name = "Kaelen", Species = "Aasimar", Class = "Paladin" };
        await characterRepo.AddAsync(character);
        await characterRepo.SaveChangesAsync();

        var characterItemRepo = new EfRepository<CharacterItem>(_db);
        await characterItemRepo.AddAsync(new CharacterItem { CharacterId = character.Id, ItemId = item.Id, Quantity = 1 });
        await characterItemRepo.SaveChangesAsync();

        var sheet = await characterRepo.GetSheetAsync(character.Id);

        Assert.NotNull(sheet);
        var ci = Assert.Single(sheet!.Items);
        Assert.Equal("Blade of the Ancients", ci.Item.Name);
        Assert.Equal(ContentKind.Homebrew, ci.Item.Source.Kind);
    }

    [Fact]
    public async Task Generic_repository_supports_add_get_list_remove()
    {
        var repo = new EfRepository<ContentSource>(_db);
        var source = new ContentSource { Kind = ContentKind.Homebrew, Attribution = "Test" };

        await repo.AddAsync(source);
        await repo.SaveChangesAsync();

        var fetched = await repo.GetAsync(source.Id);
        Assert.NotNull(fetched);
        Assert.Single(await repo.ListAsync());

        repo.Remove(fetched!);
        await repo.SaveChangesAsync();

        Assert.Empty(await repo.ListAsync());
    }

    [Fact]
    public async Task ActionEntries_are_queryable_by_session_and_round_trip_dice_results()
    {
        var campaignRepo = new EfCampaignRepository(_db);
        var characterRepo = new EfCharacterRepository(_db);
        var sessionRepo = new EfRepository<SessionLog>(_db);
        var entryRepo = new EfRepository<ActionEntry>(_db);

        var campaign = new Campaign { Name = "Beyond the Blue Door" };
        await campaignRepo.AddAsync(campaign);
        var character = new Character { Name = "Baloney Slim", Species = "Aasimar", Class = "Druid" };
        await characterRepo.AddAsync(character);
        await campaignRepo.SaveChangesAsync();

        var session = new SessionLog { CampaignId = campaign.Id, Title = "Session Zero", SessionDate = DateTime.Today };
        await sessionRepo.AddAsync(session);
        await sessionRepo.SaveChangesAsync();

        var dice = new DiceResult("WIS check (proficient)", [new Die(20, 15)], 4);
        await entryRepo.AddAsync(new ActionEntry
        {
            SessionLogId = session.Id,
            CharacterId = character.Id,
            Description = dice.Label,
            DiceResultJson = System.Text.Json.JsonSerializer.Serialize(dice),
            Source = EntrySource.Dice
        });
        await entryRepo.AddAsync(new ActionEntry
        {
            SessionLogId = session.Id,
            CharacterId = character.Id,
            Description = "Searched the pedestal for a hidden compartment.",
            Source = EntrySource.Manual
        });
        await entryRepo.SaveChangesAsync();

        var entries = (await entryRepo.ListAsync()).Where(e => e.SessionLogId == session.Id).ToList();

        Assert.Equal(2, entries.Count);
        var diceEntry = Assert.Single(entries, e => e.Source == EntrySource.Dice);
        var roundTripped = System.Text.Json.JsonSerializer.Deserialize<DiceResult>(diceEntry.DiceResultJson!);
        Assert.Equal(19, roundTripped!.Total);
        Assert.Contains(entries, e => e.Source == EntrySource.Manual);
    }

    [Fact]
    public async Task Character_repository_round_trips_combat_stats_skill_flags_and_spell_slots()
    {
        var characterRepo = new EfCharacterRepository(_db);
        var character = new Character
        {
            Name = "Baloney Slim",
            Species = "Aasimar",
            Class = "Druid",
            CurrentHp = 18,
            MaxHp = 24,
            TempHp = 3,
            ArmorClass = 15,
            Speed = 25,
            InitiativeBonus = 1,
            Size = SizeCategory.Small,
            Alignment = "Chaotic Good",
            ProficientSkills = Skill.Athletics | Skill.Perception,
            ExpertSkills = Skill.Perception,
            ProficientSaves = Ability.Constitution | Ability.Wisdom,
            SpellSlots = SpellSlots.Empty.WithCurrentAt(1, 2).WithMaxAt(1, 4).WithCurrentAt(2, 1).WithMaxAt(2, 2)
        };
        await characterRepo.AddAsync(character);
        await characterRepo.SaveChangesAsync();

        var reloaded = await characterRepo.GetAsync(character.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(18, reloaded!.CurrentHp);
        Assert.Equal(24, reloaded.MaxHp);
        Assert.Equal(3, reloaded.TempHp);
        Assert.Equal(15, reloaded.ArmorClass);
        Assert.Equal(25, reloaded.Speed);
        Assert.Equal(1, reloaded.InitiativeBonus);
        Assert.Equal(SizeCategory.Small, reloaded.Size);
        Assert.Equal("Chaotic Good", reloaded.Alignment);
        Assert.Equal(Skill.Athletics | Skill.Perception, reloaded.ProficientSkills);
        Assert.Equal(Skill.Perception, reloaded.ExpertSkills);
        Assert.Equal(Ability.Constitution | Ability.Wisdom, reloaded.ProficientSaves);
        Assert.Equal(2, reloaded.SpellSlots.CurrentAt(1));
        Assert.Equal(4, reloaded.SpellSlots.MaxAt(1));
        Assert.Equal(1, reloaded.SpellSlots.CurrentAt(2));
        Assert.Equal(2, reloaded.SpellSlots.MaxAt(2));
    }

    [Fact]
    public async Task Character_defaults_apply_on_insert_without_explicit_values()
    {
        var characterRepo = new EfCharacterRepository(_db);
        var character = new Character { Name = "Fresh Character", Species = "Human", Class = "Fighter" };
        await characterRepo.AddAsync(character);
        await characterRepo.SaveChangesAsync();

        var reloaded = await characterRepo.GetAsync(character.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(10, reloaded!.ArmorClass);
        Assert.Equal(30, reloaded.Speed);
        Assert.Equal(SizeCategory.Medium, reloaded.Size);
        Assert.Equal(Skill.None, reloaded.ProficientSkills);
        Assert.Equal(Ability.None, reloaded.ProficientSaves);
    }
}
